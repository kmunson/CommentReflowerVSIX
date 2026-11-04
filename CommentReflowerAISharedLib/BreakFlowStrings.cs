// Copyright (C) 2004  Ian Nowland
//
// This program is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation; either version 2 of the License, or (at your option) any later
// version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
// PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software Foundation, Inc., 59 Temple
// Place - Suite 330, Boston, MA 02111-1307, USA.

using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace CommentReflowerLib;

/// <summary>
/// Summary description for BreakFlowString.
/// </summary>
public class BreakFlowString
{
	/// <summary>
	/// Constructor taking values
	/// </summary>
	public BreakFlowString(
		string Name,
		string MatchString,
		bool IsRegEx,
		bool NeverReflowLine,
		bool NeverReflowIntoNextLine
	)
	{
		this.Name = Name;
		this.MatchString = MatchString;
		this.IsRegEx = IsRegEx;
		this.NeverReflowLine = NeverReflowLine;
		this.NeverReflowIntoNextLine = NeverReflowIntoNextLine;
	}

	/// <summary>
	/// Copy constructor does deep copy
	/// </summary>
	public BreakFlowString(BreakFlowString other)
	{
		this.Name = other.Name;
		this.MatchString = other.MatchString;
		this.IsRegEx = other.IsRegEx;
		this.NeverReflowLine = other.NeverReflowLine;
		this.NeverReflowIntoNextLine = other.NeverReflowIntoNextLine;
	}

	/// <summary>
	/// Returns true if this object occurs in the given string
	/// </summary>
	public bool Matches(string st, bool ontoNextLineAsWell)
	{
		if (NeverReflowLine ||
			(ontoNextLineAsWell && NeverReflowIntoNextLine))
		{
			string regstr;
			if (IsRegEx)
			{
				regstr = MatchString;
			}
			else
			{
				regstr = Regex.Escape(MatchString);
			}
			Regex regex = new(regstr);
			return regex.Match(st).Success;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// The name of the block. For display
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The string to match
	/// </summary>
	public string MatchString { get; set; }

	/// <summary>
	/// Whether the match string is a reg ex
	/// </summary>
	public bool IsRegEx { get; set; }

	/// <summary>
	/// Whether a line with this string should ever be reflowed at all
	/// </summary>
	public bool NeverReflowLine { get; set; }

	/// <summary>
	/// Whether this line is allowed to flow into the next
	/// </summary>
	public bool NeverReflowIntoNextLine { get; set; }
}
