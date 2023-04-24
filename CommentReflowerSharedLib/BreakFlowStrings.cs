// Comment Reflower Force Line Break class
// Copyright (C) 2004  Ian Nowland
// 
// This program is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free
// Softwared fadf Foundation; either version 2 of the License, or (at your
// option) any later version.
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
    /** Summary description for BreakFlowString. */
    public class BreakFlowString
    {
        /** Constructor taking values */
        public BreakFlowString(
            string mName,
            string mString,
            bool mIsRegEx,
            bool mNeverReflowLine,
            bool mNeverReflowIntoNextLine
            )
        {
            this.mName = mName;
            this.mString = mString;
            this.mIsRegEx = mIsRegEx;
            this.mNeverReflowLine = mNeverReflowLine;
            this.mNeverReflowIntoNextLine = mNeverReflowIntoNextLine;
        }

        /** Copy constructor does deep copy */
        public BreakFlowString(BreakFlowString other)
        {
            this.mName = other.mName;
            this.mString = other.mString;
            this.mIsRegEx = other.mIsRegEx;
            this.mNeverReflowLine = other.mNeverReflowLine;
            this.mNeverReflowIntoNextLine = other.mNeverReflowIntoNextLine;
        }

        /** Constructor from xml file */
        public BreakFlowString(XmlReader r)
        {
            r.ReadStartElement("BreakFlowString");
            mName = r.ReadElementString("Name");
            mString = r.ReadElementString("String");
            mIsRegEx = XmlConvert.ToBoolean(r.ReadElementString("IsRegEx"));
            mNeverReflowLine = XmlConvert.ToBoolean(r.ReadElementString("NeverReflowLine"));
            mNeverReflowIntoNextLine = XmlConvert.ToBoolean(r.ReadElementString("NeverReflowIntoNextLine"));
            r.ReadEndElement();
        }

        /** Dumps the object to xml file */
        public void dumpToXml(XmlWriter w)
        {
            w.WriteStartElement("BreakFlowString");
            w.WriteElementString("Name", mName);
            w.WriteElementString("String", mString);
            w.WriteElementString("IsRegEx", XmlConvert.ToString(mIsRegEx));
            w.WriteElementString("NeverReflowLine", XmlConvert.ToString(mNeverReflowLine));
            w.WriteElementString("NeverReflowIntoNextLine", XmlConvert.ToString(mNeverReflowIntoNextLine));
            w.WriteEndElement();
        }

        /** Returns true if this object occurs in the given string */
        public bool matches(string st, bool ontoNextLineAsWell)
        {
            if (mNeverReflowLine || 
                (ontoNextLineAsWell && mNeverReflowIntoNextLine))
            {
                string regstr;
                if (mIsRegEx)
                {
                    regstr = mString;
                }
                else
                {
                    regstr = Regex.Escape(mString);
                }
                Regex regex = new Regex(regstr);
                return regex.Match(st).Success;
            }
            else
            {
                return false;
            }
        }
        
        /** The name of the block. For display */
        public string mName;

        /** The string to match */
        public string mString;

        /** Whether the match string is a reg ex */
        public bool mIsRegEx;

        /** Whether a line with this string should ever be reflowed at all */
        public bool mNeverReflowLine;

        /** Whether this line is allowed to flow into the next */
        public bool mNeverReflowIntoNextLine;
    }
}
