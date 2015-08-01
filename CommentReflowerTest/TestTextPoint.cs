// Comment Reflower TextPoint class
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

namespace CommentReflowerTest
{
    /// <summary>
    /// This class implements enough of the EnvDTE.TextPoint and
    /// EnvDTE.EditPoint intefaces to allow regression testing
    /// </summary>
    public class TestTextPoint: EnvDTE.TextPoint, EnvDTE.EditPoint
    {
        public TestTextPoint(
            TestFileWrapper parent,
            int lineNum,
            int lineCharOffset)
        {
            mParent = parent;
            mLineNum = lineNum;
            if (lineCharOffset < 1)
            {
                throw new System.ArgumentException("lineCharOffset must be greater than 1"); 
            }
            mCharPosition = lineCharOffset - 1;
        }

        // TextPoint interface functions

        public bool EqualTo(EnvDTE.TextPoint other)
        {
            return (this.Line == other.Line) &&
                (this.LineCharOffset == other.LineCharOffset);
        }

        public bool LessThan(EnvDTE.TextPoint other)
        {
            return (this.Line < other.Line) ||
                   ((this.Line == other.Line) && (this.LineCharOffset < other.LineCharOffset));
        }

        public bool GreaterThan(EnvDTE.TextPoint other)
        {
            return (this.Line > other.Line) ||
                ((this.Line == other.Line) && (this.LineCharOffset > other.LineCharOffset));
        }

        public EnvDTE.CodeElement get_CodeElement(EnvDTE.vsCMElement cal)
        {
            return null;
        }

        public EnvDTE.DTE DTE
        {
            get
            {
                return null;
            }
        }

        public EnvDTE.TextDocument Parent
        {
            get
            {
                return mParent;
            }
        }

        public EnvDTE.EditPoint  CreateEditPoint()
        {
            return mParent.CreateEditPoint(this);
        }
 
        public int AbsoluteCharOffset
        {
            get
            {
                int count = 0;
                for (int i=1; i < mLineNum;i++)
                {
                    count += mParent.GetLine(i).Length + 1;//+1 for newline
                }
                return count + mCharPosition ;
            }
        }

        public int LineCharOffset
        {
            get
            {
                return mCharPosition+1;
            }
        }

        public int DisplayColumn
        {
            get
            {
                int ret = 0;
                string curLine = mParent.GetLine(mLineNum);
                for (int i=0; i < mCharPosition; i++)
                {
                    if (curLine[i] == '\t')
                    {
                        ret += Parent.TabSize - (ret % Parent.TabSize);
                    }
                    else
                    {
                        ret++;
                    }
                }
                return ret+1;
            }
        }

        public bool AtEndOfDocument
        {
            get
            {
                return AtEndOfLine && (mLineNum == mParent.GetNumberLines());
            }
        }

        public bool AtStartOfDocument
        {
            get
            {
                return (mLineNum == 1) && AtStartOfLine;
            }
        }
        public bool AtEndOfLine
        {
            get
            {
                return (mCharPosition == mParent.GetLine(mLineNum).Length);
            }
        }
        public bool AtStartOfLine
        {
            get
            {
                return (mCharPosition == 0);
            }
        }
        public int LineLength
        {
            get
            {
                return mParent.GetLine(mLineNum).Length;
            }
        }
        public int Line
        {
            get
            {
                return mLineNum;
            }
        }

        public bool TryToShow(
            EnvDTE.vsPaneShowHow How,
            object PointOrCount
            )
        {
            return false;
        }

        // EditPoint interface
        public void CharLeft(int i)
        {
            if (i < 0)
            {
                CharRight(-i);
                return;
            }
            while (i>0)
            {
                if ( i <= mCharPosition )
                {
                    mCharPosition -= i;
                    i=0;
                }
                else
                {
                    i -= mCharPosition;
                    if (mLineNum == 1)
                    {
                        mCharPosition = 0;
                        return;
                    }
                    else
                    {
                        mLineNum--;
                        mCharPosition = this.LineLength;
                        i--;// the carriage return moves to the end of the previous line
                    }
                }
            }
        }
        public void CharRight(int i)
        {
            if (i < 0)
            {
                CharLeft(-i);
                return;
            }
            while (i>0)
            {
                int shiftsToGo = this.LineLength - mCharPosition;
                if ( i <= shiftsToGo )
                {
                    mCharPosition += i;
                    i=0;
                }
                else
                {
                    i -= shiftsToGo;
                    if (mLineNum == mParent.GetNumberLines())
                    {
                        mCharPosition = this.LineLength;
                        return;
                    }
                    else
                    {
                        mLineNum++;
                        mCharPosition=0;
                        i--;// the carriage return moves to the start of the next line
                    }
                }
            }
        }
        public void EndOfLine()
        {
            mCharPosition = mParent.GetLine(mLineNum).Length;
        }
        public void StartOfLine()
        {
            mCharPosition = 0;
        }

        public void EndOfDocument()
        {
            mLineNum = mParent.GetNumberLines();
            mCharPosition = mParent.GetLine(mLineNum).Length;
        }
        public void StartOfDocument()
        {
            mCharPosition = 0;
            mLineNum = 1;
        }
        public void WordLeft(int i)
        {
            if (i < 0)
            {
                WordRight(-i);
            }
        }
        public void WordRight(int i)
        {
            if (i < 0)
            {
                WordLeft(-i);
            }
        }
        public void LineUp(int i)
        {
            if (i < 0)
            {
                LineDown(-i);
            }
            mLineNum -= i;
            if (mLineNum < 1)
            {
                mLineNum = 1;
            }
        }
        public void LineDown(int i)
        {
            if (i < 0)
            {
                LineUp(-i);
            }
            mLineNum += i; 
            if (mLineNum > mParent.GetNumberLines())
            {
                mLineNum = mParent.GetNumberLines();
                EndOfLine();
            }
        }
        public void MoveToPoint(EnvDTE.TextPoint pt)
        {
            mLineNum = pt.Line;
            mCharPosition = pt.LineCharOffset;
        }
        public void MoveToLineAndOffset(int i, int j)
        {
            mLineNum = i;
            mCharPosition = j-1;
        }
        public void MoveToAbsoluteOffset(int i)
        {
        }
        public void SetBookmark()
        {
        }
        public void ClearBookmark()
        {
        }
        public bool NextBookmark()
        {
            return false;
        }
        public bool PreviousBookmark()
        {
            return false;
        }
        public void PadToColumn(int i)
        {
        }
        public void Insert(string st)
        {
            mParent.OnInsert(this,st);
        }
        public void InsertFromFile(string st)
        {
        }
        public string GetText(object obj)
        {
            // FIXME: this function is not implemented to handle multiple lines!!!!

            if (obj is int)
            {
                int val = (int) obj;

                if ((val == -1) && (mCharPosition == 0))
                {
                    // ugly hack!
                    return "\r\n";
                }

                return mParent.GetLine(mLineNum).Substring(mCharPosition,val);

            } 
            else if (obj is TestTextPoint)
            {
                TestTextPoint end = (TestTextPoint) obj;
                return mParent.GetLine(mLineNum).Substring(mCharPosition,end.mCharPosition - mCharPosition);
            }

            return "";
        }
        public string GetLines(int start, int end)
        {
            string ret = "";
            for (int i=start; i < end; i++)
            {
                ret += mParent.GetLine(i) + "\n";
            }
            return ret;
        }
        public void Copy(object obj, bool b)
        {
        }
        public void Cut(object obj, bool b)
        {
        }
        public void Delete(object obj)
        {
            mParent.OnDelete(this,obj);
        }
        public void Paste()
        {
        }
        public bool ReadOnly(object obj)
        {
            return false;
        }
        public bool FindPattern(string st, int i, ref EnvDTE.EditPoint j, ref EnvDTE.TextRanges f)
        {
            return false;
        }
        public bool ReplacePattern(EnvDTE.TextPoint a, string st1, string st2, int i, ref EnvDTE.TextRanges r)
        {
            return false;
        }
        public void Indent(EnvDTE.TextPoint pt, int i)
        {
        }
        public void Unindent(EnvDTE.TextPoint pt, int j)
        {
        }
        public void SmartFormat(EnvDTE.TextPoint pt)
        {
        }
        public void OutlineSection(object i)
        {
        }
        public void ReplaceText(object i, string st, int j)
        {
        }
        public void ChangeCase(object i, EnvDTE.vsCaseOptions st)
        {
        }
        public void DeleteWhitespace(EnvDTE.vsWhitespaceOptions opt)
        {
        }

        private TestFileWrapper mParent;
        public int mLineNum;
        public int mCharPosition;
    
    }
}
