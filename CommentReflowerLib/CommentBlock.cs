// Comment Reflower Comment Block class
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
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml;

namespace CommentReflowerLib
{
    /** The different options for the start and end block strings  */
    public enum StartEndBlockType
    {
        Empty=0,
        AlwaysOnOwnLine=1,
        OnOwnLineIfBlockIsMoreThanOne=2,
        NeverOnOwnLine=3
    }

    /** Summary description for CommentBlock. */
    public class CommentBlock
    {
        /** Constructor taking values */
        public CommentBlock(
            string mName,
            ArrayList mFileAssociations,
            StartEndBlockType mBlockStartType,
            string mBlockStart,
            bool mIsBlockStartRegEx,
            StartEndBlockType mBlockEndType,
            string mBlockEnd,
            bool mIsBlockEndRegEx,
            string mLineStart,
            bool mOnlyEmptyLineBeforeStartOfBlock)
        {
            this.mName = mName;
            this.mFileAssociations = mFileAssociations;
            this.mBlockStartType = mBlockStartType;
            this.mBlockStart = mBlockStart;
            this.mIsBlockStartRegEx = mIsBlockStartRegEx;
            this.mBlockEndType = mBlockEndType;
            this.mBlockEnd = mBlockEnd;
            this.mIsBlockEndRegEx = mIsBlockEndRegEx;
            this.mLineStart = mLineStart;
            this.mOnlyEmptyLineBeforeStartOfBlock = mOnlyEmptyLineBeforeStartOfBlock;
        }

        /** Copy constructor does deep copy */
        public CommentBlock(CommentBlock other)
        {
            this.mName = other.mName;
            this.mFileAssociations = (ArrayList)other.mFileAssociations.Clone();
            this.mBlockStartType = other.mBlockStartType;
            this.mBlockStart = other.mBlockStart;
            this.mIsBlockStartRegEx = other.mIsBlockStartRegEx;
            this.mBlockEndType = other.mBlockEndType;
            this.mBlockEnd = other.mBlockEnd;
            this.mIsBlockEndRegEx = other.mIsBlockEndRegEx;
            this.mLineStart = other.mLineStart;
            this.mOnlyEmptyLineBeforeStartOfBlock = other.mOnlyEmptyLineBeforeStartOfBlock;
        }

        /** Constructor from xml file */
        public CommentBlock(XmlReader r)
        {
            r.ReadStartElement("CommentBlock");
            mName = r.ReadElementString("Name");
            mFileAssociations = createFileAssocFromString(r.ReadElementString("FileAssociations"));
            mBlockStartType = (StartEndBlockType)Enum.Parse(typeof(StartEndBlockType), r.ReadElementString("BlockStartType"));
            mBlockStart = r.ReadElementString("BlockStart");
            mIsBlockStartRegEx = XmlConvert.ToBoolean(r.ReadElementString("IsBlockStartRegEx"));
            mBlockEndType = (StartEndBlockType)Enum.Parse(typeof(StartEndBlockType), r.ReadElementString("BlockEndType"));
            mBlockEnd = r.ReadElementString("BlockEnd");
            mIsBlockEndRegEx = XmlConvert.ToBoolean(r.ReadElementString("IsBlockEndRegEx"));
            mLineStart = r.ReadElementString("LineStart");
            mOnlyEmptyLineBeforeStartOfBlock = XmlConvert.ToBoolean(r.ReadElementString("OnlyEmptyLineBeforeStartOfBlock"));
            r.ReadEndElement();
        }
    
        /** Dumps the object to xml file */
        public void dumpToXml(XmlWriter w)
        {
            w.WriteStartElement("CommentBlock");
            w.WriteElementString("Name", mName);
            w.WriteElementString("FileAssociations",getAssociationsAsString());
            w.WriteElementString("BlockStartType",mBlockStartType.ToString());
            w.WriteElementString("BlockStart",mBlockStart);
            w.WriteElementString("IsBlockStartRegEx",XmlConvert.ToString(mIsBlockStartRegEx));
            w.WriteElementString("BlockEndType",mBlockEndType.ToString());
            w.WriteElementString("BlockEnd",mBlockEnd);
            w.WriteElementString("IsBlockEndRegEx",XmlConvert.ToString(mIsBlockEndRegEx));
            w.WriteElementString("LineStart",mLineStart);
            w.WriteElementString("OnlyEmptyLineBeforeStartOfBlock",XmlConvert.ToString(mOnlyEmptyLineBeforeStartOfBlock));
            w.WriteEndElement();
        }

        /** Creates a File Assoication List from a semicolon separated string */
        public static ArrayList createFileAssocFromString(string st)
        {
            ArrayList ret = new ArrayList();
            if (st.Trim().Length == 0)
            {
                throw new System.ArgumentException("File association string must not be empty");
            }
            foreach (string next in st.Split(';'))
            {
                ret.Add(next.Trim());
            }
            return ret;
        }

        /** Returns this objects file associations as a string */
        public string getAssociationsAsString()
        {
            string ret = "";
            foreach(string st in mFileAssociations)
            {
                if (ret.Length > 0)
                {
                    ret += "; ";
                }
                ret += st;
            }
            return ret;
        }

        /**
         * Helper function converts the given string to a reg ex, taking a
         * boolean indicating whether or not it is already a reg ex
         */
        private string getRegEx(
            string str, 
            bool isRegEx)
        {
            if (isRegEx)
            {
                return str;
            }
            else
            {
                return Regex.Escape(str);
            }
        }

        /**
         * Returns true if the given line matches the block start or end or line
         * continuation for this block
         */
        public bool lineHasBlockPattern(
            string currentLine, 
            out int indentation)
        {
            string stCh = "."; // character before first block
            if (mOnlyEmptyLineBeforeStartOfBlock)
            {
                stCh = " ";
            }

            //start block
            if (mBlockStartType != StartEndBlockType.Empty)
            {
                string startString = @"^(" + stCh + @"*?)" + getRegEx(mBlockStart, mIsBlockStartRegEx);
                if (mBlockStartType == StartEndBlockType.AlwaysOnOwnLine)
                {
                    startString +=  @"[ ]*$" ; //only spaces then end of line
                }
                Regex regex = new Regex(startString);
                Match match = regex.Match(currentLine);
                if (match.Success)
                {
                    indentation = match.Groups[1].Length;
                    return true;
                }
                // If the block can be on the same line as a comment, then it
                // may have trailing spaces that don't happen when it is on a
                // line by itself. so trim those spaces and scan again.
                if (mBlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne)
                {
                    startString = @"^("+ stCh + @"*?)" + 
                                  getRegEx(mBlockStart.TrimEnd(), mIsBlockStartRegEx) + 
                                  @"$";
                    regex = new Regex(startString);
                    match = regex.Match(currentLine);
                    if (match.Success)
                    {
                        indentation = match.Groups[1].Length;
                        return true;
                    }
                }
            }

            // If the block start is not empty then the line start ch must
            // always be space.
            if (mBlockStartType != StartEndBlockType.Empty)
            {
                stCh = " ";
            }
            
            // start line
            string continString = @"^(" + stCh + @"*?)" + Regex.Escape(mLineStart);
            Regex regex2 = new Regex(continString);
            Match match2 = regex2.Match(currentLine);
            if (match2.Success)
            {
                indentation = match2.Groups[1].Length;
                return true;
            }

            // also match the trimmed start line followed by line break
            continString = @"^(" + stCh + @"*?)" + Regex.Escape(mLineStart.TrimEnd()) + @"$";
            regex2 = new Regex(continString);
            match2 = regex2.Match(currentLine);
            if (match2.Success)
            {
                indentation = match2.Groups[1].Length;
                return true;
            }

            if ((mBlockEndType == StartEndBlockType.AlwaysOnOwnLine) ||
                (mBlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne))
            {
                string endString = @"^( *?)" + getRegEx(mBlockEnd, mIsBlockEndRegEx) + @" *$";
                Regex regex = new Regex(endString);
                Match match = regex.Match(currentLine);
                if (match.Success)
                {
                    indentation = match.Groups[1].Length;
                    return true;
                }
            }
            indentation = 0;
            return false;
        }

        /**
         * Returns true if the current line matches the block end pattern.
         * Assert mBlockEnd.Length > 0.
         */
        private bool lineMatchesBlockEnd(
            string line, 
            int indent,
            out string matchedBlockEnd)
        {
            string endString = @"(" + getRegEx(mBlockEnd, mIsBlockEndRegEx) + @") *$";
            if (mBlockEndType == StartEndBlockType.AlwaysOnOwnLine)
            {
                endString = @"^ *?" + endString;
            }
            Regex regex = new Regex(endString);
            Match match = regex.Match(line);
            if (match.Success)
            {
                matchedBlockEnd = match.Groups[1].ToString();
                return true; //line is a block end
            }
            matchedBlockEnd = "";
            return false;
        }

        /**
         * Returns true if the current line matches the block start pattern.
         * @pre mBlockEnd.Length > 0
         */
        private bool lineMatchesBlockStart(
            string line, 
            int indent,
            out string matchedBlockStart,
            out int numSpacesTrimmedOffStart)
        {
            numSpacesTrimmedOffStart = 0;
            matchedBlockStart = "";
            string stCh = "."; // character before first block
            if (mOnlyEmptyLineBeforeStartOfBlock)
            {
                stCh = " ";
            }

            //start of line than anything then block start
            string startString = @"^" + stCh + @"{" + indent.ToString() + @"}(" + 
                                 getRegEx(mBlockStart, mIsBlockStartRegEx) +
                                 @")";
            if (mBlockStartType == StartEndBlockType.AlwaysOnOwnLine)
            {
                startString +=  @"[ ]*$" ; //only spaces then end of line
            }
            Regex startRegex = new Regex(startString);
            Match match = startRegex.Match(line);
            if (match.Success)
            {
                matchedBlockStart = match.Groups[1].ToString();
                return true;
            }
            // if the block start can be on the same line as comment text, then
            // it may have trailing spaces that don't happen when it is on a
            // line by itself. so trim those spaces and scan again
            if (mBlockStartType != StartEndBlockType.AlwaysOnOwnLine)
            {
                startString = @"^" + stCh + @"{" + indent.ToString() + @"}(" + 
                              getRegEx(mBlockStart.TrimEnd(), mIsBlockStartRegEx) + 
                              @")$";
                startRegex = new Regex(startString);
                match = startRegex.Match(line);
                if (match.Success)
                {
                    matchedBlockStart = match.Groups[1].ToString();
                    numSpacesTrimmedOffStart = mBlockStart.Length - mBlockStart.TrimEnd().Length;
                    return true;
                }
            }
            return false;
        }

        /**
         * Returns true if the current line is the the continuation of a block
         * (ie not block start but possible block end)
         */
        public bool lineIsBlockContinuation(
            string currentLine, 
            int indent)
        {
            // just normal line start
            string continString = @"^ {" + indent.ToString() + @"}" + Regex.Escape(mLineStart);
            Regex continRegEx = new Regex(continString);
            if (continRegEx.IsMatch(currentLine))
            {
                return true; // white space before and matches continuation
            }
            // right trimmed line start followed by end of line
            continString = @"^ {" + indent.ToString() + @"}" +  Regex.Escape(mLineStart.TrimEnd()) + @"$";
            continRegEx = new Regex(continString);
            if (continRegEx.IsMatch(currentLine))
            {
                return true; // white space before and matches continuation
            }
            return false;
        }

        /**
         * Returns true if the line is a possible start or continuation of the
         * block. 
         * @pre mBlockStartType == Empty
         */
        private bool lineIsPossibleBlockContinuationStart(
            string currentLine, 
            int indent)
        {
            if (mOnlyEmptyLineBeforeStartOfBlock)
            {
                return lineIsBlockContinuation(currentLine,indent);
            }

            // just normal line start
            string continString = @"^.{" + indent.ToString() + @"}" + Regex.Escape(mLineStart);
            Regex continRegEx = new Regex(continString);
            if (continRegEx.IsMatch(currentLine))
            {
                return true; // white space before and matches continuation
            }
            // right trimmed line start followed by end of line
            continString = @"^.{" + indent.ToString() + @"}" + Regex.Escape(mLineStart.TrimEnd()) + @"$";
            continRegEx = new Regex(continString);
            if (continRegEx.IsMatch(currentLine))
            {
                return true; // white space before and matches continuation
            }
            return false;
        }

        /** Returns true if the current line is the first line in a block */
        public bool lineIsStartOfBlock(
            string currentLine, 
            string previousLine, 
            int indent,
            out string matchedBlockStart,
            out int numSpacesTrimmedOffStart)
        {
            matchedBlockStart = "";
            numSpacesTrimmedOffStart = 0;

            if (mBlockStartType != StartEndBlockType.Empty)
            {
                if (lineMatchesBlockStart(currentLine,indent, out matchedBlockStart, out numSpacesTrimmedOffStart))
                {
                    // Have to worry about the case where the block start is 
                    // exactly the same as the block end, in which case we may
                    // have found a block end not a block start. So if the
                    // previous line is a continuation assume we are an end, and
                    // return false, else return true
                    return (mBlockStart.CompareTo(mBlockEnd) != 0) ||
                            (previousLine == null) || 
                            !lineIsBlockContinuation(previousLine, indent);
                }
            }
            // <pre>
            // if this line matches the line continuation pattern AND
            //     this is first line of document OR
            //     Non white space before this line OR
            //     Previous line does not match indentation OR
            //     Previous line is a block end THEN
            //      This is a block start
            // </pre>
            else if (lineIsPossibleBlockContinuationStart(currentLine,indent) &&
                     ((previousLine == null)  || 
                     !lineIsBlockContinuation(currentLine,indent) || 
                     !lineIsPossibleBlockContinuationStart(previousLine,indent) ||
                     ((mBlockEndType != StartEndBlockType.Empty) && 
                      (lineMatchesBlockEnd(previousLine, indent, out mDummyS)))))
            {
                return true;
            }
            return false;
        }

        /** Returns true if the current line is the last line in a block */
        public bool lineIsEndOfBlock(
            string currentLine, 
            string nextLine, 
            int indent,
            out string matchedBlockEnd)
        {
            matchedBlockEnd = "";
            if (mBlockEndType != StartEndBlockType.Empty)
            {
                if (lineMatchesBlockEnd(currentLine,indent, out matchedBlockEnd))
                {
                    // have to worry about the case where the block start is 
                    // exactly the same as the block end, in which case we
                    // may have found a block start not a block end! So if
                    // the next line is a continuation assume we are an
                    // start and so return false
                    return (mBlockStart.CompareTo(mBlockEnd) != 0) ||
                           (nextLine == null) ||
                           !lineIsBlockContinuation(nextLine,indent);
                }
            }
            // <pre>
            // if This line matches the line start pattern AND
            //      current line is the end of document OR
            //      next line is not have a continuation OR
            //      next line is a block start THEN
            //          this is a block end
            // </pre>
            else if (lineIsPossibleBlockContinuationStart(currentLine,indent) &&
                    ((nextLine == null) ||
                     !lineIsBlockContinuation(nextLine,indent) ||
                     ((mBlockStartType != StartEndBlockType.Empty) &&
                         lineMatchesBlockStart(nextLine,indent, out mDummyS, out mDummyI))))
            {
                return true;
            }
            return false;
        }

        /** The name of the block. For display */
        public string mName;

        /** A list of file assoiations of the type '*.ext' (ie not regexes)*/
        public ArrayList/*<string>*/ mFileAssociations;
        
        /** The type of the start block */
        public StartEndBlockType mBlockStartType;

        /** The start block string */
        public string mBlockStart;

        /** Whether the start block string is a reg ex */
        public bool mIsBlockStartRegEx;

        /** The type of the end block */
        public StartEndBlockType mBlockEndType;

        /** The end block string */
        public string mBlockEnd;

        /** Whether the end block string is a reg ex */
        public bool mIsBlockEndRegEx;

        /** The string for a line continuation. Never a regex */
        public string mLineStart;

        /**
         * Whether there can only be empty space before the start of block on
         * the first line of the block
         */
        public bool mOnlyEmptyLineBeforeStartOfBlock;

        /** Dummy variable used in processing */
        private string mDummyS;

        /** Dummy variable used in processing */
        private int mDummyI;
    }
}
