// Comment Reflower Parameters class
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


    /** Summary description for ParameterSet. */
    public class ParameterSet
    {
        /** Default constuctor creates a default ParameterSet */
        public ParameterSet()
        {
            ArrayList cPlusPlusCba = new ArrayList();
            cPlusPlusCba.Add("*.c");
            cPlusPlusCba.Add("*.cpp");
            cPlusPlusCba.Add("*.cs");
            cPlusPlusCba.Add("*.cc");
            cPlusPlusCba.Add("*.h");
            mCommentBlocks.Add(new CommentBlock(
                "C style function block",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.AlwaysOnOwnLine,
                @"/\*\*\*+",
                true,
                StartEndBlockType.AlwaysOnOwnLine,
                @" \*\*\*+/",
                true,
                " * ",
                true));
            mCommentBlocks.Add(new CommentBlock(
                "Doxygen C style (/**)",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
                "/** ",
                false,
                StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
                " */",
                false,
                " * ",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "Doxygen C style 2 (/*!), which may have text on first line",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.NeverOnOwnLine,
                "/*! ",
                false,
                StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
                " */",
                false,
                " * ",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "C style",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.NeverOnOwnLine,
                "/* ",
                false,
                StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
                " */",
                false,
                " * ",
                false));

            cPlusPlusCba.Add("*.js"); // ADD JAVASCRIPT AFTER C BLOCK COMMENTS
                                      // DONE!
            mCommentBlocks.Add(new CommentBlock(
                "C++ style function block",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.AlwaysOnOwnLine,
                @"////+",
                true,
                StartEndBlockType.AlwaysOnOwnLine,
                @"////+",
                true,
                "// ",
                true));
            mCommentBlocks.Add(new CommentBlock(
                "Doxygen C++ style (///)",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "/// ",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "Doxygen C++ style (///) with trailing tab",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "///\t",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "C++ style",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "// ",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "Doxygen C++ style 2 (//!)",
                (ArrayList)cPlusPlusCba.Clone(),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "//! ",
                false));

            mCommentBlocks.Add(new CommentBlock(
                "Visual Basic Single Quote (') Function Block",
                CommentBlock.createFileAssocFromString("*.vb;*.vbs"),
                StartEndBlockType.AlwaysOnOwnLine,
                @"'''''+",
                true,
                StartEndBlockType.AlwaysOnOwnLine,
                @"'''''+",
                true,
                "' ",
                true));
            mCommentBlocks.Add(new CommentBlock(
                "Visual Basic Triple Quote (''')",
                CommentBlock.createFileAssocFromString("*.vb;*.vbs"),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "''' ",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "Visual Basic Double Quote ('')",
                CommentBlock.createFileAssocFromString("*.vb;*.vbs"),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "'' ",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "Visual Basic Single Quote (')",
                CommentBlock.createFileAssocFromString("*.vb;*.vbs"),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "' ",
                false));

            mCommentBlocks.Add(new CommentBlock(
                "# Block",
                CommentBlock.createFileAssocFromString("Jamfile;Jamrules;*.jam"),
                StartEndBlockType.AlwaysOnOwnLine,
                @"\#\#\#+",
                true,
                StartEndBlockType.AlwaysOnOwnLine,
                @"\#\#\#+",
                true,
                "# ",
                true));
            mCommentBlocks.Add(new CommentBlock(
                "# Comment",
                CommentBlock.createFileAssocFromString("Jamfile;Jamrules;*.jam"),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "# ",
                false));
            mCommentBlocks.Add(new CommentBlock(
                "Empty Block for Text Files",
                CommentBlock.createFileAssocFromString("*.txt"),
                StartEndBlockType.Empty,
                "",
                false,
                StartEndBlockType.Empty,
                "",
                false,
                "",
                false));


            mBulletPoints.Add(new BulletPoint(
                "Numbered comment followed by a tag and a hyphen like '1) tag - '",
                @"[0-9]+\) \w+ - ", 
                true,                           
                true));
            mBulletPoints.Add(new BulletPoint(
                "Numbered comment like '1) '",
                @"[0-9]+\) ",
                true,
                true));
            mBulletPoints.Add(new BulletPoint(
                "Hyphen at the start of a line '- '",
                @"- ", 
                true,
                true));
            mBulletPoints.Add(new BulletPoint(
                "Doxygen style tag followed by hyphen, like '@tag - '",
                @"@\w+ - ", 
                true,
                true));
            mBulletPoints.Add(new BulletPoint(
                "Doxygen style tag followed by space, like '@tag '",
                @"@\w+ ", 
                true,
                true));
            mBulletPoints.Add(new BulletPoint(
                "Doxygen style tag followed by space, like '\\tag '",
                @"\\\w+ ", 
                true,
                true));
            mBulletPoints.Add(new BulletPoint(
                "Single character followed by hyphen, like '0 - '",
                @". - ", 
                true,
                true));

            mBreakFlowStrings.Add(new BreakFlowString(
                "HTML Line break tag",
                @"<BR>", 
                false,
                false,
                true));
            mBreakFlowStrings.Add(new BreakFlowString(
                "XML comment on a line by itself",
                @"^\s*<.+?>\s*$", 
                true,
                true,
                true));
            mBreakFlowStrings.Add(new BreakFlowString(
                "Consecutive spaces anywhere on the line between non-space elements",
                @"[^\s]+?\s\s[^\s]+?", 
                true,
                true,
                true));
            mBreakFlowStrings.Add(new BreakFlowString(
                "(Underline) consecutive -'s on a line by themselves",
                @"^\s*--+\s*$", 
                true,
                true,
                true));
            mBreakFlowStrings.Add(new BreakFlowString(
                "(Double Underline) consecutive ='s on a line by themselves",
                @"^\s*==+\s*$", 
                true,
                true,
                true));
            mBreakFlowStrings.Add(new BreakFlowString(
                "$Source rcs tag",
                @"$Source", 
                false,
                true,
                true));
            mBreakFlowStrings.Add(new BreakFlowString(
                "$Id rcs tag",
                @"$Id", 
                false,
                true,
                true));
            
            mUseTabsToIndent = false;
            mWrapWidth = 80;
            mMinimumBlockWidth =30;
        }

        /** Copy constructor does deep copy */
        public ParameterSet(ParameterSet other)
        {
            mUseTabsToIndent = other.mUseTabsToIndent;
            mWrapWidth = other.mWrapWidth;
            mMinimumBlockWidth = other.mMinimumBlockWidth;
            foreach (CommentBlock cb in other.mCommentBlocks)
            {
                mCommentBlocks.Add(new CommentBlock(cb));
            }
            foreach (BulletPoint bp in other.mBulletPoints)
            {
                mBulletPoints.Add(new BulletPoint(bp));
            }
            foreach (BreakFlowString lb in other.mBreakFlowStrings)
            {
                mBreakFlowStrings.Add(new BreakFlowString(lb));
            }
        }

        /** Constructor from xml file. Throws exception on invalid file */
        public ParameterSet(string xmlFileName)
        {
            XmlTextReader r = new XmlTextReader(xmlFileName);

            try
            {
                r.Read();
                r.ReadStartElement("CommentReflowerParameters");
                string version = r.ReadElementString("Version");
                if (version.CompareTo("1") != 0)
                {
                    throw new System.ArgumentException("Unsupported file version");
                }
                mUseTabsToIndent = XmlConvert.ToBoolean(r.ReadElementString("UseTabsToIndent"));
                mWrapWidth = XmlConvert.ToInt32(r.ReadElementString("WrapWidth"));
                mMinimumBlockWidth = XmlConvert.ToInt32(r.ReadElementString("MinimumBlockWidth"));
                validateGeneralSettings();

                r.Read();
                while (r.Name != "CommentReflowerParameters")
                {
                    if (r.LocalName == "CommentBlock")
                    {
                        mCommentBlocks.Add(new CommentBlock(r));
                        validateCommentBlock(mCommentBlocks.Count-1);
                    }
                    else if (r.LocalName == "BulletPoint")
                    {
                        mBulletPoints.Add(new BulletPoint(r));
                        validateBullet(mBulletPoints.Count-1);
                    }
                    else if (r.LocalName == "BreakFlowString")
                    {
                        mBreakFlowStrings.Add(new BreakFlowString(r));
                        validateBreakFlowString(mBreakFlowStrings.Count-1);
                    }
                    else
                    {
                        throw new System.ArgumentException("Unknown element in Xml");
                    }
                    r.Read();
                }
            }
            finally 
            {
                r.Close();
            }
        }

        /** Writes object to xml file */
        public void writeToXmlFile(string xmlFileName)
        {
            XmlTextWriter w = new XmlTextWriter(xmlFileName,new System.Text.ASCIIEncoding());
            w.Formatting = Formatting.Indented;
            w.WriteStartDocument();
            w.WriteStartElement("CommentReflowerParameters");
            w.WriteElementString("Version", "1");
            w.WriteElementString("UseTabsToIndent",XmlConvert.ToString(mUseTabsToIndent));
            w.WriteElementString("WrapWidth",XmlConvert.ToString(mWrapWidth));
            w.WriteElementString("MinimumBlockWidth",XmlConvert.ToString(mMinimumBlockWidth));

            foreach (CommentBlock cb in mCommentBlocks)
            {
                cb.dumpToXml(w);
            }
            foreach (BulletPoint bp in mBulletPoints)
            {
                bp.dumpToXml(w);
            }
            foreach (BreakFlowString lb in mBreakFlowStrings)
            {
                lb.dumpToXml(w);
            }
            w.WriteEndElement();

            w.WriteEndDocument();
            // Flush the xml document to the underlying stream and close the
            // underlying stream. The data will not be written out to the stream
            // until either the Flush() method is called or the Close() method
            // is called.
            w.Close();
        }

        /**
         * Returns true if the given string matches any bullet point string, and
         * if so sets the indentation of this line and the indentation of any
         * following text for the bullet.
         */
        public bool matchesBulletPoint(string st, out int thisIndent, out int nextLineIndent)
        {
            thisIndent = 0;
            nextLineIndent = 0;
            foreach (BulletPoint bp in mBulletPoints)
            {
                if (bp.matches(st, out thisIndent, out nextLineIndent))
                {
                    return true;
                }
            }
            return false;
        }

        /** Returns true if the given string matches any bullet point string. */
        public bool matchesBulletPoint(string st)
        {
            int temp1;
            int temp2;
            return this.matchesBulletPoint(st, out temp1, out temp2);
        }

        /**
         * Returns true if the given string matches any force line break string.
         */
        public bool matchesBreakFlowString(string st, bool ontoNextLineAsWell)
        {
            foreach (BreakFlowString flb in mBreakFlowStrings)
            {
                if (flb.matches(st,ontoNextLineAsWell))
                {
                    return true;
                }
            }
            return false;
        }


        public void validateGeneralSettings()
        {
            if (mWrapWidth < 10)
            {
                throw new System.ArgumentException("Comment wrap width must be greater than 10");
            }
            if (mMinimumBlockWidth < 10)
            {
                throw new System.ArgumentException("Minimum Block width must be greater than 10");
            }
        }

        /**
         * Validates the comment block at the given index, throws an exception
         * if it is invalis
         */
        public void validateCommentBlock(int index)
        {
            CommentBlock cb = (CommentBlock)mCommentBlocks[index];

            if (cb.mName.Trim().Length == 0)
            {
                throw new System.ArgumentException("Comment block name must not be empty");
            }

            int i=0;
            foreach (CommentBlock bpother in mCommentBlocks)
            {
                if ((i != index) && (bpother.mName.CompareTo(cb.mName) == 0))
                {
                    throw new System.ArgumentException("Comment Block must be unique");
                }
                i++;
            }

            if (cb.mBlockStartType == StartEndBlockType.Empty)
            {
                cb.mBlockStart = "";
            }
            else
            {
                if (cb.mBlockStart.Trim().Length == 0)
                {
                    throw new System.ArgumentException("Block start type is not empty but actual string only contains whitespace");
                }

                if (cb.mIsBlockStartRegEx)
                {
                    try
                    {
                        Regex regex = new Regex(cb.mBlockStart);
                    }
                    catch(Exception)
                    {
                        throw new System.ArgumentException("Block start regular expression is invalid");
                    }
                }
            }


            if (cb.mBlockEndType == StartEndBlockType.Empty)
            {
                cb.mBlockEnd = "";
            }
            else
            {
                if (cb.mBlockEnd.Trim().Length == 0)
                {
                    throw new System.ArgumentException("Block End type is not empty but actual string only contains whitespace");
                }

                if (cb.mIsBlockEndRegEx)
                {
                    try
                    {
                        Regex regex = new Regex(cb.mBlockEnd);
                    }
                    catch(Exception)
                    {
                        throw new System.ArgumentException("Block End regular expression is invalid");
                    }
                }
            }
        }

        /**
         * Validates the bullet at the given index, throws an exception if it is
         * invalid.
         */
        public void validateBullet(int index)
        {
            BulletPoint bp = (BulletPoint) mBulletPoints[index];

            if (bp.mName.Trim().Length == 0)
            {
                throw new System.ArgumentException("Bullet name must not be empty");
            }

            int i=0;
            foreach (BulletPoint bpother in mBulletPoints)
            {
                if ((i != index) && (bpother.mName.CompareTo(bp.mName) == 0))
                {
                    throw new System.ArgumentException("Bullet name must be unique");
                }
                i++;
            }

            if (bp.mIsRegEx)
            {
                try
                {
                    Regex regex = new Regex(bp.mString);
                }
                catch(Exception)
                {
                    throw new System.ArgumentException("Bullet regular expression is invalid");
                }
            }
        }

        /**
         * Validates the force line break at the given index, throws an
         * exception if it is invalid
         */
        public void validateBreakFlowString(int index)
        {
            BreakFlowString bp = (BreakFlowString) mBreakFlowStrings[index];

            if (bp.mName.Trim().Length == 0)
            {
                throw new System.ArgumentException("Force line break name must not be empty");
            }

            int i=0;
            foreach (BreakFlowString bpother in mBreakFlowStrings)
            {
                if ((i != index) && (bpother.mName.CompareTo(bp.mName) == 0))
                {
                    throw new System.ArgumentException("Force line break name must be unique");
                }
                i++;
            }

            if (bp.mIsRegEx)
            {
                try
                {
                    Regex regex = new Regex(bp.mString);
                }
                catch(Exception)
                { 
                    throw new System.ArgumentException("Force line break regular expression is invalid");
                }
            }
        }

        public ArrayList/*<CommentBlock>*/ getBlocksForFileName(string fileName)
        {
            ArrayList ret = new ArrayList();
            foreach (CommentBlock cb in mCommentBlocks)
            {
                foreach (string asoc in cb.mFileAssociations)
                {
                    string regexString = asoc.ToLower();
                    regexString = regexString.Replace(".","\\.");
                    regexString = regexString.Replace("*",".*?");
                    Regex re = new Regex(regexString);
                    if (re.IsMatch(fileName.ToLower()))
                    {
                        ret.Add(cb);
                        break; //just break inner loop
                    }
                }
            }
            return ret;
        }

        /** The comment blocks */
        public ArrayList/*<CommentBlock*/ mCommentBlocks = new ArrayList();
        
        /** The Bullets */
        public ArrayList/*<Bullets>*/ mBulletPoints = new ArrayList();

        /** The force line breaks */
        public ArrayList/*<BreakFlowStrings>*/ mBreakFlowStrings = new ArrayList();

        /** Whether to use tabs to indent blocks */
        public bool mUseTabsToIndent;

        /** The column width to wrap the block at */
        public int mWrapWidth;

        /**
         * The minimum width of any block of text. Blocks will extend past
         * mWrapWidth to meet this
         */
        public int mMinimumBlockWidth;
    }
}
