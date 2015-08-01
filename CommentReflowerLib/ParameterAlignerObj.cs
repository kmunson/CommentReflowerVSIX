using System;
using System.Collections;
using System.Diagnostics;

namespace CommentReflowerLib
{
	/// <summary>
	/// Summary description for ParameterAlignerObj.
	/// </summary>
	public class ParameterAlignerObj
	{
		public ParameterAlignerObj()
		{
			//
			// TODO: Add constructor logic here
			//
		}

        static private bool AtLineEndIgnoringWhiteSpace(
            EnvDTE.EditPoint pt)
        {
            EnvDTE.EditPoint temp = pt.CreateEditPoint();
            while ((!temp.AtEndOfLine) && 
                ((temp.GetText(1) == " ") ||
                (temp.GetText(1) == "\t")))
            {
                temp.CharRight(1);
            }
            return temp.AtEndOfLine;
        }


        public static bool go(
            EnvDTE.TextPoint searchPt,
            out EnvDTE.EditPoint finishPt
            /*,
            bool pullupIfLineShortEnough,
            int lengthToPutOnOneLine,
            bool forceOneParamPerLine,
            bool recurseOnSubCalls*/)
        {
            finishPt = null;

            EnvDTE.EditPoint curPt = searchPt.CreateEditPoint();
            // search right first for bracket
            // FIXME: "(" can't be in a string
            if ((curPt.GetText(1) != " ") && (curPt.GetText(1) != "\t"))
            {
                while ((!curPt.AtEndOfLine) && (curPt.GetText(1) != "("))
                {
                    curPt.CharRight(1);
                }
            }

            // now try back left if right did not succeed
            // FIXME: ";" and "(" can't be in a string
            if ((curPt.GetText(1) != "("))
            {
                curPt = searchPt.CreateEditPoint();
                while ((!curPt.AtStartOfDocument) && 
                    (curPt.GetText(1) != ";") && 
                    (curPt.GetText(1) != "("))
                {
                    curPt.CharLeft(1);
                }
            }

            // if no bracket then fail
            if (curPt.GetText(1) != "(")
            {
                return false;
            }

            curPt.CharRight(1);

            // Fixed 23 Mar 2010: skip whitespace between "(" and first char
            while ((curPt.GetText(1) == " ") ||
                (curPt.GetText(1) == "\t")) {
                curPt.CharRight(1);
            }

            int indentPoint = curPt.LineCharOffset;
            bool isFirstLineOfParameter = false;// note this is processed at the
                                                // start of the next line, so
                                                // unless the first line finishes
                                                // with a comma then the second
                                                // will not be the first line of
                                                // a parameter.
            int lineShiftRight = 0;
            bool inCBlockComment = false;
            bool inCppBlockComment = false;
            bool done = false;
            while (!done)
            {
                // skip any whitespace on the current line
                while ((curPt.GetText(1) == " ") ||
                    (curPt.GetText(1) == "\t"))
                {
                    curPt.CharRight(1);
                }

                if (curPt.AtEndOfDocument)
                {
                    throw new System.ArgumentException("Unexpected eod processing parameters");
                }
                else if (curPt.AtEndOfLine)
                {
                    curPt.LineDown(1);
                    curPt.StartOfLine();
                    while ((curPt.GetText(1) == " ") ||
                        (curPt.GetText(1) == "\t"))
                    {
                        curPt.CharRight(1);
                    }
                    if (curPt.AtEndOfLine)
                    {
                        continue;
                    }
                    if ((inCppBlockComment) && (curPt.GetText(2) != "//"))
                    {
                        inCppBlockComment = false;
                        isFirstLineOfParameter = true;
                    }
                    if (isFirstLineOfParameter)
                    {
                        lineShiftRight = indentPoint - curPt.LineCharOffset;
                    }
                    isFirstLineOfParameter = false;

                    // delete or insert spaces to meet indentation
                    int finalPosition = curPt.LineCharOffset + lineShiftRight;
                    while (curPt.LineCharOffset > finalPosition)
                    {
                        curPt.Delete(-1);
                    }
                    while (curPt.LineCharOffset < finalPosition)
                    {
                        //FIXME: tabs as well
                        curPt.Insert(" ");
                    }
                }
                else if ((inCBlockComment) && (curPt.GetText(2) == "*/"))
                {
                    inCBlockComment = false;
                    isFirstLineOfParameter = true;
                    curPt.CharRight(2);
                }
                else if (inCBlockComment)
                {
                    curPt.CharRight(1);
                }
                else if (inCppBlockComment)
                {
                    curPt.EndOfLine();
                }
                else if (curPt.GetText(1) == ";")
                {
                    throw new System.ArgumentException("Unexpected error parsing funtion call");
                }
                else if (curPt.GetText(1) == "(")
                {
                    // recurse
                    ParameterAlignerObj.go(curPt,out curPt);
                }
                else if (curPt.GetText(1) == ",")
                {
                    curPt.CharRight(1);
                    if (AtLineEndIgnoringWhiteSpace(curPt))
                    {
                        isFirstLineOfParameter = true;
                    }
                }
                else if (curPt.GetText(1) == ")")
                {
                    done = true;
                    curPt.CharRight(1);
                    finishPt = curPt;
                }
                else if (curPt.GetText(2) == "//")
                {
                    curPt.EndOfLine();
                    inCppBlockComment = true;
                }
                else if (curPt.GetText(2) == "/*")
                {
                    curPt.CharRight(2);
                    inCBlockComment =  true;
                }
                else if (curPt.GetText(2) == "@\"")
                {
                    curPt.CharRight(2);
                    //scan ahead until close 
                    while (curPt.GetText(1) != "\"")
                    {
                        curPt.CharRight(1);
                        if (curPt.AtEndOfLine)
                        {
                            throw new System.ArgumentException("Unexpected eol processing string");
                        }
                    }
                    curPt.CharRight(1);
                }
                else if ((curPt.GetText(1) == "\"") || 
                    (curPt.GetText(1) == "'"))
                {
                    string quote = curPt.GetText(1);
                    string lastChar = quote;
                    curPt.CharRight(1);

                    //scan ahead until close 
                    while (!((curPt.GetText(1) == quote) && 
                        (lastChar != "\\")))
                    {
                        lastChar = curPt.GetText(1);
                        curPt.CharRight(1);
                        if (curPt.AtEndOfLine)
                        {
                            throw new System.ArgumentException("Unexpected eol processing string");
                        }
                    }
                    curPt.CharRight(1);
                }
                else
                {
                    curPt.CharRight(1);
                }
            }
            return true;
        }
	}
}
