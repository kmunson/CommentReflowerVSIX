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
/// Summary description for BulletPoint.
/// </summary>
public class BulletPoint
{
	/// <summary>
	/// Constructor taking values
	/// </summary>
	public BulletPoint(
		string mName,
		string mString,
		bool mIsRegEx,
		bool mWrapIsAtRight
	)
	{
		this.mName = mName;
		this.mString = mString;
		this.mIsRegEx = mIsRegEx;
		this.mWrapIsAtRight = mWrapIsAtRight;
	}

	/// <summary>
	/// Copy constructor does deep copy
	/// </summary>
	public BulletPoint(BulletPoint other)
	{
		this.mName = other.mName;
		this.mString = other.mString;
		this.mIsRegEx = other.mIsRegEx;
		this.mWrapIsAtRight = other.mWrapIsAtRight;
	}

	/// <summary>
	/// Returns true if this object occurs in the given string, anf if true sets the
	/// indentation of this line and the indentation of any following text for the
	/// bullet.
	/// </summary>
	public bool Matches(string st, out int thisIndent, out int nextLineIndent)
	{
		string regstr = @"^(\s*?)";

		if (mIsRegEx && (mString[0] == '('))
		{
			regstr += mString;
		}
		else if (mIsRegEx)
		{
			regstr += "(" + mString + ")";
		}
		else
		{
			regstr += "(" + Regex.Escape(mString) + ")";
		}
		Regex regex = new(regstr);
		Match match = regex.Match(st);
		if (match.Success)
		{
			thisIndent = match.Groups[1].Length + match.Groups[2].Length;
			if (!mWrapIsAtRight)
			{
				nextLineIndent = match.Groups[1].Length;
			}
			else
			{
				nextLineIndent = thisIndent;
			}
			return true;
		}
		else
		{
			thisIndent = 0;
			nextLineIndent = 0;
			return false;
		}
	}

	/// <summary>
	/// The name of the block. For display
	/// </summary>
	public string mName;

	/// <summary>
	/// The string to match
	/// </summary>
	public string mString;

	/// <summary>
	/// Whether the match string is a reg ex
	/// </summary>
	public bool mIsRegEx;

	/// <summary>
	/// Whether the second and subsequent lines of the bullet should wrap at the left or
	/// the right of the match string.
	/// </summary>
	public bool mWrapIsAtRight;
}
