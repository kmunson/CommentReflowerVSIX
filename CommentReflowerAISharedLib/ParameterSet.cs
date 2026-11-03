// Comment Reflower Parameters class
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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CommentReflowerLib;

/// <summary>
/// Summary description for ParameterSet.
/// </summary>
public class ParameterSet
{
	/// <summary>
	/// Default constuctor creates a default ParameterSet
	/// </summary>
	public ParameterSet()
	{
		List<string> cPlusPlusCba = new()
		{
			"*.c",
			"*.cpp",
			"*.cs",
			"*.cc",
			"*.h"
		};
		mCommentBlocks.Add(new CommentBlock(
			"C style function block",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.AlwaysOnOwnLine,
			@"/\*\*\*+",
			true,
			StartEndBlockType.AlwaysOnOwnLine,
			@" \*\*\*+/",
			true,
			" * ",
			true
		));
		mCommentBlocks.Add(new CommentBlock(
			"Doxygen C style (/**)",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
			"/** ",
			false,
			StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
			" */",
			false,
			" * ",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"Doxygen C style 2 (/*!), which may have text on first line",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.NeverOnOwnLine,
			"/*! ",
			false,
			StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
			" */",
			false,
			" * ",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"C style",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.NeverOnOwnLine,
			"/* ",
			false,
			StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne,
			" */",
			false,
			" * ",
			false
		));

		// ADD JAVASCRIPT AFTER C BLOCK COMMENTS
		// DONE!
		cPlusPlusCba.Add("*.js");
		mCommentBlocks.Add(new CommentBlock(
			"C++ style function block",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.AlwaysOnOwnLine,
			@"////+",
			true,
			StartEndBlockType.AlwaysOnOwnLine,
			@"////+",
			true,
			"// ",
			true
		));
		mCommentBlocks.Add(new CommentBlock(
			"Doxygen C++ style (///)",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"/// ",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"Doxygen C++ style (///) with trailing tab",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"///\t",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"C++ style",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"// ",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"Doxygen C++ style 2 (//!)",
			cPlusPlusCba.ToArray(),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"//! ",
			false
		));

		mCommentBlocks.Add(new CommentBlock(
			"Visual Basic Single Quote (') Function Block",
			CommentBlock.CreateFileAssocFromString("*.vb;*.vbs"),
			StartEndBlockType.AlwaysOnOwnLine,
			@"'''''+",
			true,
			StartEndBlockType.AlwaysOnOwnLine,
			@"'''''+",
			true,
			"' ",
			true
		));
		mCommentBlocks.Add(new CommentBlock(
			"Visual Basic Triple Quote (''')",
			CommentBlock.CreateFileAssocFromString("*.vb;*.vbs"),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"''' ",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"Visual Basic Double Quote ('')",
			CommentBlock.CreateFileAssocFromString("*.vb;*.vbs"),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"'' ",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"Visual Basic Single Quote (')",
			CommentBlock.CreateFileAssocFromString("*.vb;*.vbs"),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"' ",
			false
		));

		mCommentBlocks.Add(new CommentBlock(
			"# Block",
			CommentBlock.CreateFileAssocFromString("Jamfile;Jamrules;*.jam"),
			StartEndBlockType.AlwaysOnOwnLine,
			@"\#\#\#+",
			true,
			StartEndBlockType.AlwaysOnOwnLine,
			@"\#\#\#+",
			true,
			"# ",
			true
		));
		mCommentBlocks.Add(new CommentBlock(
			"# Comment",
			CommentBlock.CreateFileAssocFromString("Jamfile;Jamrules;*.jam"),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"# ",
			false
		));
		mCommentBlocks.Add(new CommentBlock(
			"Empty Block for Text Files",
			CommentBlock.CreateFileAssocFromString("*.txt"),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"",
			false
		));

		mBulletPoints.Add(new BulletPoint(
			"Numbered comment followed by a tag and a hyphen like '1) tag - '",
			@"[0-9]+\) \w+ - ",
			true,
			true
		));
		mBulletPoints.Add(new BulletPoint(
			"Numbered comment like '1) '",
			@"[0-9]+\) ",
			true,
			true
		));
		mBulletPoints.Add(new BulletPoint(
			"Hyphen at the start of a line '- '",
			@"- ",
			true,
			true
		));
		mBulletPoints.Add(new BulletPoint(
			"Doxygen style tag followed by hyphen, like '@tag - '",
			@"@\w+ - ",
			true,
			true
		));
		mBulletPoints.Add(new BulletPoint(
			"Doxygen style tag followed by space, like '@tag '",
			@"@\w+ ",
			true,
			true
		));
		mBulletPoints.Add(new BulletPoint(
			"Doxygen style tag followed by space, like '\\tag '",
			@"\\\w+ ",
			true,
			true
		));
		mBulletPoints.Add(new BulletPoint(
			"Single character followed by hyphen, like '0 - '",
			@". - ",
			true,
			true
		));

		mBreakFlowStrings.Add(new BreakFlowString(
			"HTML Line break tag",
			@"<BR>",
			false,
			false,
			true
		));
		mBreakFlowStrings.Add(new BreakFlowString(
			"XML comment on a line by itself",
			@"^\s*<.+?>\s*$",
			true,
			true,
			true
		));
		mBreakFlowStrings.Add(new BreakFlowString(
			"Consecutive spaces anywhere on the line between non-space elements",
			@"[^\s]+?\s\s[^\s]+?",
			true,
			true,
			true
		));
		mBreakFlowStrings.Add(new BreakFlowString(
			"(Underline) consecutive -'s on a line by themselves",
			@"^\s*--+\s*$",
			true,
			true,
			true
		));
		mBreakFlowStrings.Add(new BreakFlowString(
			"(Double Underline) consecutive ='s on a line by themselves",
			@"^\s*==+\s*$",
			true,
			true,
			true
		));
		mBreakFlowStrings.Add(new BreakFlowString(
			"$Source rcs tag",
			@"$Source",
			false,
			true,
			true
		));
		mBreakFlowStrings.Add(new BreakFlowString(
			"$Id rcs tag",
			@"$Id",
			false,
			true,
			true
		));

		mUseTabsToIndent = false;
		mWrapWidth = 80;
		mMinimumBlockWidth = 30;
	}

	/// <summary>
	/// Copy constructor does deep copy
	/// </summary>
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

	/// <summary>
	/// Returns true if the given string matches any bullet point string, and
	/// if so sets the indentation of this line and the indentation of any
	/// following text for the bullet.
	/// </summary>
	public bool MatchesBulletPoint(string st, out int thisIndent, out int nextLineIndent)
	{
		thisIndent = 0;
		nextLineIndent = 0;
		foreach (BulletPoint bp in mBulletPoints)
		{
			if (bp.Matches(st, out thisIndent, out nextLineIndent))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns true if the given string matches any bullet point string.
	/// </summary>
	public bool MatchesBulletPoint(string st)
	{
		return this.MatchesBulletPoint(st, out _, out _);
	}

	/// <summary>
	/// Returns true if the given string matches any force line break string.
	/// </summary>
	public bool MatchesBreakFlowString(string st, bool ontoNextLineAsWell)
	{
		foreach (BreakFlowString flb in mBreakFlowStrings)
		{
			if (flb.Matches(st, ontoNextLineAsWell))
			{
				return true;
			}
		}
		return false;
	}

	public void ValidateGeneralSettings()
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

	/// <summary>
	/// Validates the comment block at the given index, throws an exception
	/// if it is invalis
	/// </summary>
	public void ValidateCommentBlock(int index)
	{
		CommentBlock cb = (CommentBlock)mCommentBlocks[index];

		if (cb.mName.Trim().Length == 0)
		{
			throw new System.ArgumentException("Comment block name must not be empty");
		}

		int i = 0;
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
					Regex regex = new(cb.mBlockStart);
				}
				catch (Exception)
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
					Regex regex = new(cb.mBlockEnd);
				}
				catch (Exception)
				{
					throw new System.ArgumentException("Block End regular expression is invalid");
				}
			}
		}
	}

	/// <summary>
	/// Validates the bullet at the given index, throws an exception if it is
	/// invalid.
	/// </summary>
	public void ValidateBullet(int index)
	{
		BulletPoint bp = (BulletPoint)mBulletPoints[index];

		if (bp.mName.Trim().Length == 0)
		{
			throw new System.ArgumentException("Bullet name must not be empty");
		}

		int i = 0;
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
				Regex regex = new(bp.mString);
			}
			catch (Exception)
			{
				throw new System.ArgumentException("Bullet regular expression is invalid");
			}
		}
	}

	/// <summary>
	/// Validates the force line break at the given index, throws an
	/// exception if it is invalid
	/// </summary>
	public void ValidateBreakFlowString(int index)
	{
		BreakFlowString bp = (BreakFlowString)mBreakFlowStrings[index];

		if (bp.mName.Trim().Length == 0)
		{
			throw new System.ArgumentException("Force line break name must not be empty");
		}

		int i = 0;
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
				Regex regex = new(bp.mString);
			}
			catch (Exception)
			{
				throw new System.ArgumentException("Force line break regular expression is invalid");
			}
		}
	}

	public List<CommentBlock> GetBlocksForFileName(string fileName)
	{
		var ret = new List<CommentBlock>();
		foreach (CommentBlock cb in mCommentBlocks)
		{
			foreach (string asoc in cb.mFileAssociations)
			{
				string regexString = asoc.ToLower();
				regexString = regexString.Replace(".", "\\.");
				regexString = regexString.Replace("*", ".*?");
				Regex re = new(regexString);
				if (re.IsMatch(fileName.ToLower()))
				{
					ret.Add(cb);
					break; //just break inner loop
				}
			}
		}
		return ret;
	}

	/// <summary>
	/// The comment blocks
	/// </summary>
	public List<CommentBlock> mCommentBlocks = new();

	/// <summary>
	/// The Bullets
	/// </summary>
	public List<BulletPoint> mBulletPoints = new();

	/// <summary>
	/// The force line breaks
	/// </summary>
	public List<BreakFlowString> mBreakFlowStrings = new();

	/// <summary>
	/// Whether to use tabs to indent blocks
	/// </summary>
	public bool mUseTabsToIndent;

	/// <summary>
	/// The column width to wrap the block at
	/// </summary>
	public int mWrapWidth;

	/// <summary>
	/// The minimum width of any block of text. Blocks will extend past
	/// mWrapWidth to meet this
	/// </summary>
	public int mMinimumBlockWidth;
}
