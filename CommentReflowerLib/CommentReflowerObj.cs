// Comment Reflower main entry class
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
using System.Text.RegularExpressions;

namespace CommentReflowerLib
{

    public class MatchedBlockData
    {
        /** The line of the file containg the start of the block */
        public int mStartLine;
        /** The line of the file containing the end of the block */
        public int mEndLine;
        /** The indentation of the block within the file */
        public int mIndentation;
        /** The actual string matches as the start of block. */
        public string mMatchedBlockStart;
        /**
         * The number of spaces that should be added to the start of block if it
         * is put on the same line as comment. Only valid if the comment block
         * type was OnOwnLineIfBlockIsMoreThanOne
         */
        public int mSpacesTrimmedOffBlockStart;
        /** The actual string matches as the start of block. */
        public string mMatchedBlockEnd;
    }

    /** Summary description for CommentReflowerObj. */
    public class CommentReflowerObj
    {
        public CommentReflowerObj()
        {
        }

        /**
         * Wraps all blocks that have any part between the two given text points 
         * Returns if any blocks found.
         */
        static public bool WrapAllBlocksInSelection(
            ParameterSet pset,
            string fileName,
            EnvDTE.TextPoint selectionStart, 
            EnvDTE.TextPoint selectionEnd)
        {
            bool blockFound = false;
            EnvDTE.EditPoint selStart = selectionStart.CreateEditPoint();
            selStart.StartOfLine();
            while (selStart.LessThan(selectionEnd))
            {
                CommentBlock block;
                MatchedBlockData bdata;
                if (GetBlockContainingPoint(pset,
                                            fileName,
                                            selStart,
                                            out block,
                                            out bdata))
                {
                    blockFound = true;
                    selStart.LineDown((bdata.mEndLine-bdata.mStartLine)+1);
                    WrapBlock(pset,block,bdata,selectionStart);
                }
                else
                {
                    selStart.LineDown(1);
                }
            }
            return blockFound;
        }
        
        /**
         * Wraps the block that contains the given text point. Returns if a
         * block was found.
         */
        static public bool WrapBlockContainingPoint(
            ParameterSet pset,
            string fileName, 
            EnvDTE.TextPoint pt)
        {
            CommentBlock block;
            MatchedBlockData bdata;
            if (!GetBlockContainingPoint(pset,
                                         fileName,
                                         pt,
                                         out block,
                                         out bdata))
            {
                return false;
            }
            WrapBlock(pset,block,bdata,pt);
            return true;
        }

        /**
         * Determines if a block contains the given point, and is fo returns the
         * CommentBlock and MatchedBlockData for the match.
         */
        static public bool GetBlockContainingPoint(
            ParameterSet pset,
            string fileName,
            EnvDTE.TextPoint pt,
            out CommentBlock retblock,
            out MatchedBlockData bdata)
        {
            retblock = null;
            bdata = new MatchedBlockData();
            bdata.mEndLine = 0;
            bdata.mStartLine = 0;
            bdata.mIndentation = 0;

            EnvDTE.EditPoint ep = pt.CreateEditPoint();
            EnvDTE.EditPoint enddoc = pt.CreateEditPoint();
            enddoc.EndOfDocument();
            string line = GetUntabbedLine(enddoc,ep.Line);

            foreach (CommentBlock block in pset.getBlocksForFileName(fileName))
            {
                if (block.lineHasBlockPattern(line,out bdata.mIndentation))
                {
                    int currentLineNumber = ep.Line;
                    // scan up for block start
                    bool foundStart = false;
                    for ( ;currentLineNumber >= 1; currentLineNumber--)
                    {
                        string currentLine = GetUntabbedLine(enddoc, currentLineNumber);
                        string previousLine = GetUntabbedLine(enddoc, currentLineNumber-1);
                        string nextLine = GetUntabbedLine(enddoc, currentLineNumber+1);
                        if (block.lineIsStartOfBlock(currentLine,
                                                     previousLine,
                                                     bdata.mIndentation,
                                                     out bdata.mMatchedBlockStart,
                                                     out bdata.mSpacesTrimmedOffBlockStart))
                        {
                            bdata.mStartLine = currentLineNumber;
                            foundStart = true;
                            break;
                        }
                        else if (!block.lineIsBlockContinuation(currentLine,bdata.mIndentation) &&
                                 !block.lineIsEndOfBlock(currentLine,nextLine,bdata.mIndentation,out bdata.mMatchedBlockEnd))
                        {
                            break;
                        }
                    }
                    if (foundStart)
                    {
                        bool foundEnd = false;
                        for ( ; currentLineNumber <= enddoc.Line ; currentLineNumber++)
                        {
                            string currentLine = GetUntabbedLine(enddoc, currentLineNumber);
                            string nextLine = GetUntabbedLine(enddoc, currentLineNumber+1);
                            if (block.lineIsEndOfBlock(currentLine, nextLine,bdata.mIndentation,out bdata.mMatchedBlockEnd))
                            {
                                bdata.mEndLine = currentLineNumber;
                                foundEnd = true;
                                break;
                            }
                            else if ((currentLineNumber != bdata.mStartLine) &&
                                (!block.lineIsBlockContinuation(currentLine,bdata.mIndentation)))
                            {
                                break;
                            }
                        }
                        if (foundEnd)
                        {
                            retblock = block;
                            return true;
                        }
                        // else try next block
                    }
                    // else try next block
                }
            }
            return false;
        }


        /**
         * Returns the line at the given number with any tabs before any
         * non-space character converted to spaces.
         * @param enddoc is just used to get the line. 
         * @ret null if the given line is outside the bounds of the document.
         */
        static private string GetUntabbedLine(EnvDTE.EditPoint enddoc, int lineNum)
        {
            if ((lineNum < 1) || (lineNum > enddoc.Line))
            {
                return null;
            }
            else
            {
                string temp = enddoc.GetLines(lineNum,lineNum+1);
                string ret = "";
                for (int i=0; i < temp.Length; i++)
                {
                    if (temp[i] == '\t')
                    {
                        int spaces = enddoc.Parent.TabSize - (ret.Length % enddoc.Parent.TabSize);
                        ret += new System.String(' ',spaces);
                    }
                    else if (temp[i] == ' ')
                    {
                        ret += temp[i];
                    }
                    else
                    {
                        return ret + temp.Substring(i);
                    }
                }
                return ret;
            }
        }

        static private void WrapBlock(
            ParameterSet pset,
            CommentBlock block,
            MatchedBlockData bdata,
            EnvDTE.TextPoint pt)
        {
            int blockTabSize = pt.Parent.TabSize;
            if (!pset.mUseTabsToIndent)
            {
                blockTabSize = 0;
            }

            bool isInPreformmatedBlock = false;
            bool isLastLine = false;
            bool isStartOnSeparateLine = false;

            EnvDTE.EditPoint curPoint = pt.CreateEditPoint();
            curPoint.MoveToLineAndOffset(bdata.mStartLine,1);

            // seems we have to pick the reight line ending ourselves
            string eol = "\r\n";
            if (curPoint.GetText(-1).Length == 1)
            {
                eol = "\n";
            }

            EnvDTE.EditPoint endPoint = pt.CreateEditPoint();
            endPoint.MoveToLineAndOffset(bdata.mEndLine,1);

            if ((block.mBlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) ||
                (block.mBlockEndType == StartEndBlockType.NeverOnOwnLine))
            {
                // delete the end block string
                endPoint.StartOfLine();
                SkipColumns(endPoint, bdata.mIndentation);
                if (GetTextOnLine(endPoint, bdata.mMatchedBlockEnd.Length) == bdata.mMatchedBlockEnd)
                {
                    // delete the whole line
                    endPoint.EndOfLine();
                    EnvDTE.EditPoint secondLastLinePoint = endPoint.CreateEditPoint();
                    secondLastLinePoint.LineUp(1);
                    secondLastLinePoint.EndOfLine();
                    secondLastLinePoint.Delete(endPoint);
                }
                else
                {
                    // delete just the string
                    EnvDTE.EditPoint eolPoint = endPoint.CreateEditPoint();
                    eolPoint.EndOfLine();
                    int endOffset = endPoint.GetText(eolPoint).LastIndexOf(bdata.mMatchedBlockEnd);
                    endPoint.CharRight(endOffset);
                    endPoint.Delete(eolPoint);
                }
            }
            else if (block.mBlockEndType == StartEndBlockType.AlwaysOnOwnLine)
            {
                // just move up al line as there is nothing interesting on it
                endPoint.LineUp(1);
            }
            endPoint.EndOfLine();

            // now loop down the lines
            while (!isLastLine)
            {
                if (curPoint.Line >= endPoint.Line)
                {
                    isLastLine = true;
                }

                curPoint.StartOfLine();
                if ((curPoint.Line == bdata.mStartLine) && 
                    (block.mBlockStartType == StartEndBlockType.AlwaysOnOwnLine))
                {
                    curPoint.LineDown(1);// simply go to the next line
                    continue;
                }
                else if ((curPoint.Line == bdata.mStartLine) && 
                    (block.mBlockStartType == StartEndBlockType.NeverOnOwnLine))
                {
                    SkipColumns(curPoint, bdata.mIndentation);
                    SkipString(curPoint, bdata.mMatchedBlockStart);
                }
                else if ((curPoint.Line == bdata.mStartLine) && 
                    (block.mBlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne))
                {
                    // we will automatically pull up the next line if we can. We
                    // also haqndle the case where traling spaces from the block
                   // start have been removed
                    SkipColumns(curPoint, bdata.mIndentation);
                    SkipString(curPoint, bdata.mMatchedBlockStart);

                    if ((!AtLineEndIgnoringWhiteSpace(curPoint)) || isLastLine)
                    {
                        isStartOnSeparateLine = false;
                    }
                    else if (LineJustContainsContinuation(curPoint.Line+1,bdata.mIndentation,block.mLineStart, pset, pt))
                    {
                        isStartOnSeparateLine = true;
                    }
                    else
                    {
                        isStartOnSeparateLine = false;
                        if (bdata.mSpacesTrimmedOffBlockStart > 0)
                        {
                            curPoint.Insert(new String(' ', bdata.mSpacesTrimmedOffBlockStart));
                            // change these as we reprocess line!
                            bdata.mMatchedBlockStart += new String(' ', bdata.mSpacesTrimmedOffBlockStart);
                            bdata.mSpacesTrimmedOffBlockStart = 0;
                        }
                        EnvDTE.EditPoint nextLineStartPoint = curPoint.CreateEditPoint();
                        nextLineStartPoint.LineDown(1);
                        nextLineStartPoint.StartOfLine();
                        SkipColumns(nextLineStartPoint, bdata.mIndentation);
                        SkipString(nextLineStartPoint, block.mLineStart);
                        curPoint.Delete(nextLineStartPoint);
                        continue;// reprocess the line as it now may be last
                    }
                }
                else // just a regular line start
                {
                    // pass over line start
                    SkipColumns(curPoint, bdata.mIndentation);
                    if (GetTextOnLine(curPoint, block.mLineStart.Length) != block.mLineStart)
                    {
                        if ((block.mBlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) && 
                            (GetTextOnLine(curPoint, bdata.mMatchedBlockEnd.Length) == bdata.mMatchedBlockEnd))
                        {
                            break;// we are done!
                        }
                        else if (GetTextOnLine(curPoint, block.mLineStart.TrimEnd().Length) == block.mLineStart.TrimEnd())
                        {
                            curPoint.LineDown(1);
                            continue;// empty line with just trimmed block line
                                     // start
                        }
                        else
                        {
                            throw new System.ArgumentException("Error parsing block line start");
                        }
                    }
                    curPoint.CharRight(block.mLineStart.Length);
                }

                // ASSERT: past the comment block start or comment line start
                // for the line

                int lastStartPre = GetRestOfLine(curPoint).ToLower().LastIndexOf("<pre>");
                int lastEndPre = GetRestOfLine(curPoint).ToLower().LastIndexOf("</pre>");

                // work out if we are in a preformatted block
                if ((lastStartPre != -1) && (lastStartPre > lastEndPre))
                {
                    isInPreformmatedBlock =  true;
                }

                // check all cases that stop wrapping of this line to the next
                if (isInPreformmatedBlock || 
                    AtLineEndIgnoringWhiteSpace(curPoint) ||
                    pset.matchesBreakFlowString(GetRestOfLine(curPoint),false))
                {
                    if ((lastEndPre != -1) && (lastEndPre > lastStartPre))
                    {
                        isInPreformmatedBlock =  false;
                    }
                    curPoint.LineDown(1);
                    continue;
                }
                bool breakLine = pset.matchesBreakFlowString(GetRestOfLine(curPoint),true);

                // work out if we are in a preformatted block
                if ((lastEndPre != -1) && (lastEndPre > lastStartPre))
                {
                    isInPreformmatedBlock =  false;
                }

                // work out indent for current line
                int currentIndent = -1;
                int thisIndent;
                if (pset.matchesBulletPoint(GetRestOfLine(curPoint),out thisIndent, out currentIndent))
                {
                    // We need to convert current indent to number of columns 
                    // as at the moment it is number of characters.
                    EnvDTE.EditPoint tempPoint = curPoint.CreateEditPoint();
                    tempPoint.CharRight(currentIndent);
                    currentIndent = tempPoint.DisplayColumn - curPoint.DisplayColumn;
                    // Now advance on this line by the size of the bullet point.
                    curPoint.CharRight(thisIndent);
                }
                else 
                {
                    currentIndent = SkipWhitespace(curPoint);
                }
                if ((block.mBlockStartType == StartEndBlockType.NeverOnOwnLine) &&
                    (curPoint.Line == bdata.mStartLine) )
                {
                    // Need to account that on the first line the block start
                    // may have a different length to the line start.
                    //
                    // Note the reason we do not do the multi/ single option here
                    // is that if we are pulling up text onto the first line we 
                    // actually break the reflowing indentation rule. This all
                    // needs to be thought through a bit better.
                    currentIndent += bdata.mMatchedBlockStart.Length - block.mLineStart.Length;
                }

                int blockWrapWidth = pset.mWrapWidth;
                if ((bdata.mIndentation + block.mLineStart.Length + currentIndent + pset.mMinimumBlockWidth) > blockWrapWidth)
                {
                    blockWrapWidth = bdata.mIndentation + block.mLineStart.Length + currentIndent + pset.mMinimumBlockWidth;
                }



                // Now see if we can wrap into the next line, or if we wrap by
                // inserting a new line before it.
                EnvDTE.EditPoint nextLinePoint = null;
                bool wrapIntoNextLine = false;
                if (!isLastLine && !breakLine)
                {
                    nextLinePoint = curPoint.CreateEditPoint();
                    nextLinePoint.LineDown(1);
                    nextLinePoint.StartOfLine();
                    SkipColumns(nextLinePoint, bdata.mIndentation);
                    if ((block.mLineStart.TrimEnd().Length != block.mLineStart.Length) &&
                        (GetRestOfLine(nextLinePoint).CompareTo(block.mLineStart.TrimEnd()) == 0))
                    {
                        // handle the next line that is completely empty with
                        // rimmed line end
                        nextLinePoint = null;
                    }
                    else
                    {
                        SkipString(nextLinePoint, block.mLineStart);
                        if (!pset.matchesBulletPoint(GetRestOfLine(nextLinePoint)) &&
                            !pset.matchesBreakFlowString(GetRestOfLine(nextLinePoint),false) &&
                            (SkipWhitespace(nextLinePoint) == currentIndent))
                        {
                            wrapIntoNextLine = true;
                        }
                        else
                        {
                            nextLinePoint = null;
                        }
                    }
                }
                
                // if on the first line and we can'd wrap the text into the next
                // line, then push any text on the first line down to a separate
                // line
                if ((block.mBlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
                    (curPoint.Line == bdata.mStartLine) &&
                    !wrapIntoNextLine &&
                    !isLastLine &&
                    !isStartOnSeparateLine)
                {
                    isStartOnSeparateLine = true;
                    curPoint.StartOfLine();
                    SkipColumns(curPoint, bdata.mIndentation);
                    SkipString(curPoint, bdata.mMatchedBlockStart.TrimEnd());
                    curPoint.Delete(bdata.mMatchedBlockStart.Length - bdata.mMatchedBlockStart.TrimEnd().Length);
                    curPoint.Insert(eol + GetIndentationString(1,bdata.mIndentation,blockTabSize) + block.mLineStart);
                    continue;
                }

                // ASSERT: if we have got here, there is at least one word on
                // this line, and if wrapIntoNextLine then there is at least one
                // word on the next line

                // now go to the start of the first word that passes the wrap
                // point, or the end of the line if it is sooner
                int wordCount = 0;
                while (((curPoint.DisplayColumn-1) <= blockWrapWidth) &&
                       !AtLineEndIgnoringWhiteSpace(curPoint))
                {
                    wordCount++;
                    GoToEndOfNextWord(curPoint);
                }

                if ((curPoint.DisplayColumn-1) <= blockWrapWidth)
                {
                    // the end of line occurs at or before the wrap point. Try
                    // to fill in the gap from the next line if we can
                    if (!wrapIntoNextLine)
                    {
                        curPoint.LineDown(1);
                    }
                    else
                    {
                        int charsToFill = blockWrapWidth - ((curPoint.DisplayColumn-1)+1);//+1 for extra space

                        EnvDTE.EditPoint nextLineEndPoint = nextLinePoint.CreateEditPoint();
                        int numChars = 0;
                        int numWords =0;
                        while ((numChars <  charsToFill) && 
                            !AtLineEndIgnoringWhiteSpace(nextLineEndPoint))
                        {
                            numWords++;
                            GoToEndOfNextWord(nextLineEndPoint);
                            numChars = nextLineEndPoint.DisplayColumn - nextLinePoint.DisplayColumn;
                        }
                        if ((numChars > charsToFill) && (numWords > 1))
                        {
                            GoToEndOfPreviousWord(nextLineEndPoint);
                            numChars = nextLineEndPoint.DisplayColumn - nextLinePoint.DisplayColumn;
                        }
                        if ((numChars <= 0) || (numChars > charsToFill))
                        {
                            // the first word on the next line is too long to
                            // pull up

                            // push comment block start onto separate line and
                            // retry if we can 
                            if ((curPoint.Line == bdata.mStartLine) &&
                                (block.mBlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
                                !isStartOnSeparateLine)
                            {
                                isStartOnSeparateLine = true;
                                curPoint.StartOfLine();
                                SkipColumns(curPoint, bdata.mIndentation);
                                SkipString(curPoint, bdata.mMatchedBlockStart.TrimEnd());
                                curPoint.Delete(bdata.mMatchedBlockStart.Length - bdata.mMatchedBlockStart.TrimEnd().Length);
                                curPoint.Insert(eol + GetIndentationString(1,bdata.mIndentation,blockTabSize) + block.mLineStart);
                            }
                            else
                            {
                                // just skip onto the next line
                                curPoint.LineDown(1);
                            }
                        }
                        else
                        {
                            // delete trailing whitespace from this line
                            EnvDTE.EditPoint lineEndPoint = curPoint.CreateEditPoint();
                            lineEndPoint.EndOfLine();
                            curPoint.Delete(lineEndPoint);

                            // get new string from next line and insert it
                            string st = nextLinePoint.GetText(nextLineEndPoint);
                            if (AtLineEndIgnoringWhiteSpace(nextLineEndPoint))
                            {
                                // no text left on next line, so delete the
                                // whole thing
                                nextLineEndPoint.EndOfLine();
                                curPoint.Delete(nextLineEndPoint);
                                curPoint.Insert(" " + st);
                                // don't move curPoint down as we want to
                                // reprocess this line in case there is more
                                // room left
                            }
                            else
                            {
                                // there is still text left on the next line

                                // push comment block start onto separate line
                                // if neccesary
                                if ((curPoint.Line == bdata.mStartLine) &&
                                    (block.mBlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
                                    !isStartOnSeparateLine)
                                {
                                    isStartOnSeparateLine = true;
                                    curPoint.StartOfLine();
                                    SkipColumns(curPoint, bdata.mIndentation);
                                    SkipString(curPoint, bdata.mMatchedBlockStart.TrimEnd());
                                    curPoint.Delete(bdata.mMatchedBlockStart.Length - bdata.mMatchedBlockStart.TrimEnd().Length);
                                    curPoint.Insert(eol + GetIndentationString(1,bdata.mIndentation,blockTabSize) + block.mLineStart);
                                    // reprocess this moved down line!
                                }
                                else
                                {
                                    // remove trailing string as well as
                                    // trailing spaces
                                    while ((nextLineEndPoint.GetText(1) == " ") ||
                                        (nextLineEndPoint.GetText(1) == "\t"))
                                    {
                                        nextLineEndPoint.CharRight(1);
                                    }
                                    nextLinePoint.Delete(nextLineEndPoint);
                                    curPoint.Insert(" " + st);
                                    curPoint.LineDown(1);//move to next line
                                }
                            }
                        }
                    }
                }
                else // ((curPoint.DisplayColumn-1) > blockWrapWidth)
                {   
                    // push start onto separate line if neccesary
                    if ((curPoint.Line == bdata.mStartLine) && 
                        (block.mBlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
                        !isStartOnSeparateLine)
                    {
                        isStartOnSeparateLine = true;
                        curPoint.StartOfLine();
                        SkipColumns(curPoint, bdata.mIndentation);
                        SkipString(curPoint, bdata.mMatchedBlockStart.TrimEnd());
                        curPoint.Delete(bdata.mMatchedBlockStart.Length - bdata.mMatchedBlockStart.TrimEnd().Length);
                        curPoint.Insert(eol + GetIndentationString(1,bdata.mIndentation,blockTabSize) + block.mLineStart);
                        isLastLine = false;// forces reprocess even if isLatLine
                                           // was just true
                        continue;// reprocess the just moved line
                    }

                    // this line overflows the word wrap. Move back to the end
                    // of the last word that doesn't overflow if possible
                    if (wordCount > 1)
                    {
                        GoToEndOfPreviousWord(curPoint);
                    }
                    if (AtLineEndIgnoringWhiteSpace(curPoint))
                    {
                        curPoint.LineDown(1);// the single line is too long, so
                                             // forget about it 
                        // and go to the next line
                    }
                    else
                    {
                        // copy the remainder of the line after a space but
                        // delete the space as well
                        if (!wrapIntoNextLine)
                        {
                            EnvDTE.EditPoint dataStartPoint = curPoint.CreateEditPoint();
                            SkipWhitespace(dataStartPoint);
                            curPoint.Delete(dataStartPoint);
                            // insert a whole new line
                            curPoint.Insert(eol + GetIndentationString(1,bdata.mIndentation,blockTabSize) + block.mLineStart);
                            if (currentIndent > 0)
                            {
                                curPoint.Insert(GetIndentationString(curPoint.DisplayColumn,currentIndent,blockTabSize));
                            }
                        }
                        else
                        {
                            // insert at the start of the next line
                            EnvDTE.EditPoint dataStartPoint = curPoint.CreateEditPoint();
                            SkipWhitespace(dataStartPoint);
                            EnvDTE.EditPoint lineEndPoint = curPoint.CreateEditPoint();
                            lineEndPoint.EndOfLine();
                            string st = dataStartPoint.GetText(lineEndPoint).TrimEnd();
                            curPoint.Delete(lineEndPoint);
                            nextLinePoint.Insert(st + " ");
                            curPoint.LineDown(1);
                        }
                        // if there is enough there to create one or multiple
                        // lines, then create them
                        while (true)
                        {
                            curPoint.StartOfLine();
                            SkipColumns(curPoint, bdata.mIndentation);
                            SkipString(curPoint, block.mLineStart);
                            SkipColumns(curPoint, currentIndent);
                            int myWordCount = 0;
                            while ((curPoint.DisplayColumn-1) <= blockWrapWidth &&
                                !AtLineEndIgnoringWhiteSpace(curPoint))
                            {
                                myWordCount++;
                                GoToEndOfNextWord(curPoint);
                            }
                            if ((curPoint.DisplayColumn-1) <= blockWrapWidth)
                            {
                                break; // don't move down a line as we want to
                                    // reprocess this line
                            }
                            if (myWordCount > 1)
                            {
                                GoToEndOfPreviousWord(curPoint);
                            }
                            if (AtLineEndIgnoringWhiteSpace(curPoint))
                            {
                                break;
                            }
                            EnvDTE.EditPoint dataStartPoint = curPoint.CreateEditPoint();
                            SkipWhitespace(dataStartPoint);
                            curPoint.Delete(dataStartPoint);
                            curPoint.Insert(eol + GetIndentationString(1,bdata.mIndentation,blockTabSize) + block.mLineStart);
                            if (currentIndent > 0)
                            {
                                curPoint.Insert(GetIndentationString(curPoint.DisplayColumn,currentIndent,blockTabSize));
                            }
                            // process the just created line in the next
                            // iteration
                        }
                    }
                }
            }

            if ((block.mBlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
                (endPoint.Line != bdata.mStartLine))
            {
                endPoint.EndOfLine();
                endPoint.Insert(eol + GetIndentationString(1,bdata.mIndentation,blockTabSize) + bdata.mMatchedBlockEnd);
            }
            else if ((block.mBlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) ||
                (block.mBlockEndType == StartEndBlockType.NeverOnOwnLine))
            {
                endPoint.EndOfLine();
                endPoint.Insert(bdata.mMatchedBlockEnd);
            }
        }
        /**
         * Helper function returns -1 if the line is empty else returns the
         * amount of whitespace pt is indented by
         */
        static private int SkipWhitespace(
            EnvDTE.EditPoint pt)
        {
            int start = pt.DisplayColumn;
            while (!pt.AtEndOfLine &&
                ((pt.GetText(1) == " ") ||
                (pt.GetText(1) == "\t")))
            {
                pt.CharRight(1);
            }
            if (pt.AtEndOfLine)
            {
                return -1;
            }
            else
            {
                return pt.DisplayColumn-start;
            }
        }

        static private void SkipColumns(
            EnvDTE.EditPoint pt,
            int blockIndentation)
        {
            int start = pt.DisplayColumn;
            int startLine = pt.Line;
            while ((pt.DisplayColumn-start) < blockIndentation)
            {
                pt.CharRight(1);
                if (pt.Line != startLine)
                {
                    throw new System.ArgumentException("Error parsing columns");
                }
            }
            if ((pt.DisplayColumn-start) != blockIndentation)
            {
                throw new System.ArgumentException("Error parsing columns");
            }
        }

        static private string GetIndentationString(
            int startColumn, 
            int numberColumns, 
            int tabSize)
        {
            if (tabSize > 0)
            {
                string ret = "";
                int endColumn = startColumn + numberColumns;
                while ((startColumn + tabSize) <= endColumn)
                {
                    ret += "\t";
                    startColumn += tabSize - ((startColumn-1) % tabSize);
                }
                if (startColumn < endColumn)
                {

                    ret += new System.String(' ',endColumn-startColumn);
                }
                return ret;
            }
            else
            {
                return new System.String(' ',numberColumns);
            }
        }

        static private void SkipString(EnvDTE.EditPoint pt, string wanted)
        {
            string actual = GetTextOnLine(pt, wanted.Length);
            if (actual != wanted)
            {
                throw new System.ArgumentException("Error parsing expected string", wanted);
            }
            pt.CharRight(wanted.Length);
        }

        static private string GetTextOnLine(EnvDTE.EditPoint pt, int length)
        {
            if (length > (pt.LineLength - (pt.LineCharOffset-1)))
            {
                length = pt.LineLength - (pt.LineCharOffset-1);
            }
            string st = pt.GetText(length);
            return st;
        }

        static private string GetRestOfLine(EnvDTE.EditPoint pt)
        {
            return pt.GetText(pt.LineLength - (pt.LineCharOffset-1));
        }

        static private bool LineJustContainsContinuation(
            int lineNum,
            int indentation,
            string lineStart,
            ParameterSet pset,
            EnvDTE.TextPoint pt)
        {
            EnvDTE.EditPoint temp = pt.CreateEditPoint();
            temp.MoveToLineAndOffset(lineNum,1);
            SkipColumns(temp, indentation);
            SkipString(temp, lineStart.TrimEnd());// note the trim here, which
                                                  // allows the match of blank
                                                  // lines
            return AtLineEndIgnoringWhiteSpace(temp) ||
                pset.matchesBreakFlowString(GetRestOfLine(temp),false) ||
                pset.matchesBulletPoint(GetRestOfLine(temp));
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

        // pre: must be a word before the end of the line moves to the end of
        // the word
        static private void GoToEndOfNextWord(
            EnvDTE.EditPoint pt)
        {
            while ((pt.GetText(1) == " ") ||
                (pt.GetText(1) == "\t"))
            {
                pt.CharRight(1);
            }
            while ((!pt.AtEndOfLine) && 
                (pt.GetText(1) != " ") &&
                (pt.GetText(1) != "\t"))
            {
                pt.CharRight(1);
            }
        }

        static private void GoToEndOfPreviousWord(
            EnvDTE.EditPoint pt)
        {
            pt.CharLeft(1);
            while ((pt.GetText(1) != " ") &&
                (pt.GetText(1) != "\t"))
            {
                pt.CharLeft(1);
            }
            while ((pt.GetText(1) == " ") ||
                (pt.GetText(1) == "\t"))
            {
                pt.CharLeft(1);
            }
            pt.CharRight(1);
        }
    }
}
