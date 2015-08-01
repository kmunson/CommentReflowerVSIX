// Comment Reflower FileWrapper class
// Copyright (C) 2004  Ian Nowland
// 
// This program is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation; either version 2 of the License, or (at your option) any later
// version.
// 
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.IO;
using System.Collections;

namespace CommentReflowerTest
{
    /// <summary>
    /// This class implements enough of EnvDTE.TextDocument for regression
    /// testing.
    /// </summary>
    public class TestFileWrapper: EnvDTE.TextDocument
    {
        public TestFileWrapper(string fileName)
        {
            mCurrentPoints = new ArrayList();
            using (StreamReader sr = new StreamReader(fileName)) 
            {
                String line;
                // Read and display lines from the file until the end of the
                // file is reached.
                mLines = new ArrayList();
                while ((line = sr.ReadLine()) != null) 
                {
                    mLines.Add(line);
                }
            }
        }
        public void WriteToFile(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName)) 
            {
                for (int i=0; i < mLines.Count; i++)
                {
                    sw.WriteLine(mLines[i]);
                }
            }
        }
        public void ClearBookmarks()
        {
        }
        public bool MarkText(string st, int i)
        {
            return true;
        }
        public bool ReplacePattern(string a, string b, int c, ref EnvDTE.TextRanges d)
        {
            return false;
        }
        public EnvDTE.EditPoint CreateEditPoint(EnvDTE.TextPoint tp)
        {
            EnvDTE.EditPoint ret;
            if (tp == null)
            {
                ret = new TestTextPoint(this,1,1);
            }
            else
            {
                ret = new TestTextPoint(this,tp.Line, tp.LineCharOffset);
            }
            mCurrentPoints.Add(new WeakReference(ret));
            return ret;
        }

        public bool ReplaceText(string a, string b, int i)
        {
            return true;
        }
        public void PrintOut()
        {
        }
        public EnvDTE.DTE DTE
        {
            get
            {
                return null;
            }
        }
        public EnvDTE.Document Parent
        {
            get
            {
                return null;
            }
        }
        public EnvDTE.TextSelection Selection
        {
            get
            {
                return null;
            }
        }
        public EnvDTE.TextPoint StartPoint
        {
            get
            {
                return null;
            }
        }
        public EnvDTE.TextPoint EndPoint
        {
            get
            {
                return null;
            }
        }
        public string Language
        {
            get
            {
                return null;
            }
            set
            {
            }
        }
        public string Type
        {
            get
            {
                return null;
            }
        }
        public int IndentSize
        {
            get
            {
                return 0;
            }
        }
        public int TabSize
        {
            get
            {
                return 4;
            }
        }


        public void OnDelete(
            TestTextPoint startPoint,
            object obj)
        {
            TestTextPoint endPoint;
            if (obj is int)
            {
                endPoint = (TestTextPoint) startPoint.CreateEditPoint();
                endPoint.CharRight((int)obj);
            }
            else if (obj is TestTextPoint)
            {
                endPoint = (TestTextPoint) obj;
            }
            else
            {
                throw new System.ArgumentException("Invalid argument");
            }
            if (!endPoint.GreaterThan(startPoint))
            {
                if (endPoint.EqualTo(startPoint))
                {
                    return;
                }
                else
                {
                    throw new System.ArgumentException("Invalid argument");
                }
            }

            if (startPoint.mLineNum == endPoint.mLineNum)
            {
                SetLine(startPoint.mLineNum, GetLine(startPoint.mLineNum).Remove(startPoint.mCharPosition, 
                        endPoint.mCharPosition - startPoint.mCharPosition));
            }
            else
            {
                SetLine(startPoint.mLineNum, GetLine(startPoint.mLineNum).Remove(startPoint.mCharPosition,
                                                                           startPoint.LineLength - startPoint.mCharPosition) + 
                                           GetLine(endPoint.mLineNum).Remove(0,endPoint.mCharPosition));
                mLines.RemoveRange((startPoint.mLineNum-1)+1,endPoint.mLineNum - startPoint.mLineNum);
            }

            int startLine = startPoint.mLineNum;
            int startChar = startPoint.mCharPosition;
            int endLine = endPoint.mLineNum;
            int endChar = endPoint.mCharPosition;

            for (int i=0; i < mCurrentPoints.Count; )
            {
                WeakReference wr = (WeakReference) mCurrentPoints[i];
                if (wr.IsAlive)
                {
                    TestTextPoint curPoint = (TestTextPoint) wr.Target;
                    if (((curPoint.mLineNum > startLine) ||
                         ((curPoint.mLineNum == startLine) && (curPoint.mCharPosition >= startChar) )) &&
                        ((curPoint.mLineNum < endLine) ||
                         ((curPoint.mLineNum == endLine) && (curPoint.mCharPosition <= endChar) )))
                    {
                        // point in range of deletion
                        curPoint.mLineNum = startLine;
                        curPoint.mCharPosition = startChar;
                    }
                    else if ((curPoint.mLineNum == endLine) &&
                        (curPoint.mCharPosition >= endChar))
                    {
                        // point on line after deletion
                        curPoint.mLineNum = startLine;
                        curPoint.mCharPosition = startChar + (curPoint.mCharPosition - endChar);
                    }
                    else if (curPoint.mLineNum > endLine)
                    {
                        // line after deletion
                        curPoint.mLineNum -= endLine - startLine;
                    }
                    i++;
                }
                else
                {
                    mCurrentPoints.RemoveAt(i);
                }
            }
        }

        public void OnInsert(
            TestTextPoint startPoint,
            string st)
        {
            int startLine = startPoint.mLineNum;
            int startChar = startPoint.mCharPosition;
            int charsPushedOnNextLine=0;// chars after last carriage return
            int linesPushedDown=0;// number carriage returns

            if (st[0] == '\n')
            {
                string temp = GetLine(startLine).Substring(startChar);
                SetLine(startLine,GetLine(startLine).Substring(0,startChar));
                mLines.Insert(startLine,st.Substring(1) + temp);
                
                linesPushedDown = 1;
                charsPushedOnNextLine = st.Length-1;
            }
            else if (st[0] == '\r')
            {
                string temp = GetLine(startLine).Substring(startChar);
                SetLine(startLine,GetLine(startLine).Substring(0,startChar));
                mLines.Insert(startLine,st.Substring(2) + temp);
                
                linesPushedDown = 1;
                charsPushedOnNextLine = st.Length-2;
            }
            else
            {
                SetLine(startLine, GetLine(startPoint.mLineNum).Insert(startChar,st));
                charsPushedOnNextLine = st.Length+startChar;
            }

            for (int i=0; i < mCurrentPoints.Count; )
            {
                WeakReference wr = (WeakReference) mCurrentPoints[i];
                if (wr.IsAlive)
                {
                    TestTextPoint curPoint = (TestTextPoint) wr.Target;
                    if ((curPoint.mLineNum == startLine) &&
                        (curPoint.mCharPosition >= startChar))
                    {
                        curPoint.mLineNum += linesPushedDown;
                        curPoint.mCharPosition = (curPoint.mCharPosition - startChar) +
                                                 charsPushedOnNextLine;
                    } 
                    else if (curPoint.mLineNum > startLine)
                    {
                        curPoint.mLineNum += linesPushedDown;
                    }
                    i++;
                }
                else
                {
                    mCurrentPoints.RemoveAt(i);
                }
            }

        
        
        }

// my functions:
        
        public string GetLine(int lineNum)
        {
            return (string)mLines[lineNum-1];
        }

        public void SetLine(int lineNum, string val)
        {
            mLines[lineNum-1] = val;
        }

        public int GetNumberLines()
        {
            return mLines.Count;
        }

        // no line break characters here!!
        private ArrayList/*<string>*/ mLines;

        private ArrayList/*<WeakRefernce<TestTextPoint>>*/ mCurrentPoints;
    }
}
