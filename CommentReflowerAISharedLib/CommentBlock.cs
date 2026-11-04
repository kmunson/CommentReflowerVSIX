// Comment Reflower Comment Block class
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
/// The different options for the start and end block strings
/// </summary>
public enum StartEndBlockType
{
	Empty = 0,
	AlwaysOnOwnLine = 1,
	OnOwnLineIfBlockIsMoreThanOne = 2,
	NeverOnOwnLine = 3
}

/// <summary>
/// Summary description for CommentBlock.
/// </summary>
public class CommentBlock
{
	/// <summary>
	/// Constructor taking values
	/// </summary>
	public CommentBlock(
		string Name,
		string[] FileAssociations,
		StartEndBlockType BlockStartType,
		string BlockStart,
		bool IsBlockStartRegEx,
		StartEndBlockType BlockEndType,
		string BlockEnd,
		bool IsBlockEndRegEx,
		string LineStart,
		bool OnlyEmptyLineBeforeStartOfBlock
	)
	{
		this.Name = Name;
		this.FileAssociations = FileAssociations;
		this.BlockStartType = BlockStartType;
		this.BlockStart = BlockStart;
		this.IsBlockStartRegEx = IsBlockStartRegEx;
		this.BlockEndType = BlockEndType;
		this.BlockEnd = BlockEnd;
		this.IsBlockEndRegEx = IsBlockEndRegEx;
		this.LineStart = LineStart;
		this.OnlyEmptyLineBeforeStartOfBlock = OnlyEmptyLineBeforeStartOfBlock;
	}

	/// <summary>
	/// Copy constructor does deep copy
	/// </summary>
	public CommentBlock(CommentBlock other)
	{
		this.Name = other.Name;
		this.FileAssociations = (string[])other.FileAssociations.Clone();
		this.BlockStartType = other.BlockStartType;
		this.BlockStart = other.BlockStart;
		this.IsBlockStartRegEx = other.IsBlockStartRegEx;
		this.BlockEndType = other.BlockEndType;
		this.BlockEnd = other.BlockEnd;
		this.IsBlockEndRegEx = other.IsBlockEndRegEx;
		this.LineStart = other.LineStart;
		this.OnlyEmptyLineBeforeStartOfBlock = other.OnlyEmptyLineBeforeStartOfBlock;
	}

	/// <summary>
	/// Creates a File Association List from a semicolon separated string
	/// </summary>
	public static string[] CreateFileAssocFromString(string st)
	{
		if (st.Trim().Length == 0)
		{
			throw new System.ArgumentException("File association string must not be empty");
		}
		string[] ret = st.Split(';');
		return Array.ConvertAll(ret, s => s.Trim());
	}

	/// <summary>
	/// Helper function converts the given string to a reg ex, taking a boolean
	/// indicating whether or not it is already a reg ex
	/// </summary>
	private string GetRegEx(
		string str,
		bool isRegEx
	)
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

	/// <summary>
	/// Returns true if the given line matches the block start or end or line
	/// continuation for this block
	/// </summary>
	public bool LineHasBlockPattern(
		string currentLine,
		out int indentation
	)
	{
		string stCh = "."; // character before first block
		if (OnlyEmptyLineBeforeStartOfBlock)
		{
			stCh = " ";
		}

		//start block
		if (BlockStartType != StartEndBlockType.Empty)
		{
			string startString = @"^(" + stCh + @"*?)" + GetRegEx(BlockStart, IsBlockStartRegEx);
			if (BlockStartType == StartEndBlockType.AlwaysOnOwnLine)
			{
				startString += @"[ ]*$"; //only spaces then end of line
			}
			Regex regex = new(startString);
			Match match = regex.Match(currentLine);
			if (match.Success)
			{
				indentation = match.Groups[1].Length;
				return true;
			}
			// If the block can be on the same line as a comment, then it may have trailing
			// spaces that don't happen when it is on a line by itself. so trim those spaces
			// and scan again.
			if (BlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne)
			{
				startString = @"^(" + stCh + @"*?)" +
					GetRegEx(BlockStart.TrimEnd(), IsBlockStartRegEx) +
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

		// If the block start is not empty then the line start ch must always be space.
		if (BlockStartType != StartEndBlockType.Empty)
		{
			stCh = " ";
		}

		// start line
		string continString = @"^(" + stCh + @"*?)" + Regex.Escape(LineStart);
		Regex regex2 = new(continString);
		Match match2 = regex2.Match(currentLine);
		if (match2.Success)
		{
			indentation = match2.Groups[1].Length;
			return true;
		}

		// also match the trimmed start line followed by line break
		continString = @"^(" + stCh + @"*?)" + Regex.Escape(LineStart.TrimEnd()) + @"$";
		regex2 = new Regex(continString);
		match2 = regex2.Match(currentLine);
		if (match2.Success)
		{
			indentation = match2.Groups[1].Length;
			return true;
		}

		if ((BlockEndType == StartEndBlockType.AlwaysOnOwnLine) ||
			(BlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne))
		{
			string endString = @"^( *?)" + GetRegEx(BlockEnd, IsBlockEndRegEx) + @" *$";
			Regex regex = new(endString);
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

	/// <summary>
	/// Whether the lines of the block after the first can leave out the line start. This
	/// is so if the line start is blank, because nothing in it shows that a line is in
	/// the block, and the block has a start that does.
	/// </summary>
	public bool LineStartIsOptional
	{
		get
		{
			return (BlockStartType != StartEndBlockType.Empty) && (LineStart.Trim(' ').Length == 0);
		}
	}

	/// <summary>
	/// Returns the indentations that the block containing the given line could have, to
	/// be tried in order. If the line has the block pattern this is the indentation it
	/// has. A block whose line start is optional, as in XML, has nothing on the lines
	/// after the first to show where the block is indented, so it could be indented by
	/// any amount up to that of the line, and the nearest block start is wanted.
	/// </summary>
	public IEnumerable<int> GetPossibleIndentations(string currentLine)
	{
		if (LineStartIsOptional)
		{
			for (int i = currentLine.Length - currentLine.TrimStart(' ').Length; i >= 0; i--)
			{
				yield return i;
			}
		}
		else if (LineHasBlockPattern(currentLine, out int indentation))
		{
			yield return indentation;
		}
	}

	/// <summary>
	/// Returns true if the current line matches the block end pattern. Assert
	/// BlockEnd.Length > 0.
	/// </summary>
	private bool LineMatchesBlockEnd(
		string line,
		out string matchedBlockEnd
	)
	{
		string endString = @"(" + GetRegEx(BlockEnd, IsBlockEndRegEx) + @") *$";
		if (BlockEndType == StartEndBlockType.AlwaysOnOwnLine)
		{
			endString = @"^ *?" + endString;
		}
		Regex regex = new(endString);
		Match match = regex.Match(line);
		if (match.Success)
		{
			matchedBlockEnd = match.Groups[1].ToString();
			return true; // line is a block end
		}
		matchedBlockEnd = "";
		return false;
	}

	/// <summary>
	/// Returns true if the current line matches the block start pattern.
	/// @pre BlockEnd.Length > 0
	/// </summary>
	private bool LineMatchesBlockStart(
		string line,
		int indent,
		out string matchedBlockStart,
		out int numSpacesTrimmedOffStart
	)
	{
		numSpacesTrimmedOffStart = 0;
		matchedBlockStart = "";
		string stCh = "."; // character before first block
		if (OnlyEmptyLineBeforeStartOfBlock)
		{
			stCh = " ";
		}

		//start of line than anything then block start
		string startString = @"^" + stCh + @"{" + indent.ToString() + @"}(" +
			GetRegEx(BlockStart, IsBlockStartRegEx) +
			@")";
		if (BlockStartType == StartEndBlockType.AlwaysOnOwnLine)
		{
			startString += @"[ ]*$"; //only spaces then end of line
		}
		Regex startRegex = new(startString);
		Match match = startRegex.Match(line);
		if (match.Success)
		{
			matchedBlockStart = match.Groups[1].ToString();
			return true;
		}
		// if the block start can be on the same line as comment text, then it may have
		// trailing spaces that don't happen when it is on a line by itself. so trim those
		// spaces and scan again
		if (BlockStartType != StartEndBlockType.AlwaysOnOwnLine)
		{
			startString = @"^" + stCh + @"{" + indent.ToString() + @"}(" +
				GetRegEx(BlockStart.TrimEnd(), IsBlockStartRegEx) +
				@")$";
			startRegex = new Regex(startString);
			match = startRegex.Match(line);
			if (match.Success)
			{
				matchedBlockStart = match.Groups[1].ToString();
				numSpacesTrimmedOffStart = BlockStart.Length - BlockStart.TrimEnd().Length;
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns true if the current line is the the continuation of a block (ie not
	/// block start but possible block end)
	/// </summary>
	public bool LineIsBlockContinuation(
		string currentLine,
		int indent
	)
	{
		// a line start that is optional is only white space, so any line indented enough
		if (LineStartIsOptional)
		{
			return Regex.IsMatch(currentLine, @"^ {" + indent.ToString() + "}");
		}
		// just normal line start
		string continString = @"^ {" + indent.ToString() + @"}" + Regex.Escape(LineStart);
		Regex continRegEx = new(continString);
		if (continRegEx.IsMatch(currentLine))
		{
			return true; // white space before and matches continuation
		}
		// right trimmed line start followed by end of line
		continString = @"^ {" + indent.ToString() + @"}" + Regex.Escape(LineStart.TrimEnd()) + @"$";
		continRegEx = new Regex(continString);
		if (continRegEx.IsMatch(currentLine))
		{
			return true; // white space before and matches continuation
		}
		return false;
	}

	/// <summary>
	/// Returns true if the line is a possible start or continuation of the block.
	/// @pre BlockStartType == Empty
	/// </summary>
	private bool LineIsPossibleBlockContinuationStart(
		string currentLine,
		int indent
	)
	{
		if (OnlyEmptyLineBeforeStartOfBlock)
		{
			return LineIsBlockContinuation(currentLine, indent);
		}

		// just normal line start
		string continString = @"^.{" + indent.ToString() + @"}" + Regex.Escape(LineStart);
		Regex continRegEx = new(continString);
		if (continRegEx.IsMatch(currentLine))
		{
			return true; // white space before and matches continuation
		}
		// right trimmed line start followed by end of line
		continString = @"^.{" + indent.ToString() + @"}" + Regex.Escape(LineStart.TrimEnd()) + @"$";
		continRegEx = new Regex(continString);
		if (continRegEx.IsMatch(currentLine))
		{
			return true; // white space before and matches continuation
		}
		return false;
	}

	/// <summary>
	/// Returns true if the current line is the first line in a block
	/// </summary>
	public bool LineIsStartOfBlock(
		string currentLine,
		string previousLine,
		int indent,
		out string matchedBlockStart,
		out int numSpacesTrimmedOffStart
	)
	{
		matchedBlockStart = "";
		numSpacesTrimmedOffStart = 0;

		if (BlockStartType != StartEndBlockType.Empty)
		{
			if (LineMatchesBlockStart(currentLine, indent, out matchedBlockStart, out numSpacesTrimmedOffStart))
			{
				// Have to worry about the case where the block start is exactly the same as the
				// block end, in which case we may have found a block end not a block start. So if
				// the previous line is a continuation assume we are an end, and return false, else
				// return true
				return (BlockStart.CompareTo(BlockEnd) != 0) ||
					(previousLine == null) ||
					!LineIsBlockContinuation(previousLine, indent);
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
		else if (LineIsPossibleBlockContinuationStart(currentLine, indent) &&
			((previousLine == null) ||
				!LineIsBlockContinuation(currentLine, indent) ||
				!LineIsPossibleBlockContinuationStart(previousLine, indent) ||
				((BlockEndType != StartEndBlockType.Empty) &&
					(LineMatchesBlockEnd(previousLine, out _)))))
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// Returns true if the current line is the last line in a block
	/// </summary>
	public bool LineIsEndOfBlock(
		string currentLine,
		string nextLine,
		int indent,
		out string matchedBlockEnd
	)
	{
		matchedBlockEnd = "";
		if (BlockEndType != StartEndBlockType.Empty)
		{
			if (LineMatchesBlockEnd(currentLine, out matchedBlockEnd))
			{
				// have to worry about the case where the block start is exactly the same as the
				// block end, in which case we may have found a block start not a block end! So if
				// the next line is a continuation assume we are an start and so return false
				return (BlockStart.CompareTo(BlockEnd) != 0) ||
					(nextLine == null) ||
					!LineIsBlockContinuation(nextLine, indent);
			}
		}
		// <pre>
		// if This line matches the line start pattern AND
		//      current line is the end of document OR
		//      next line is not have a continuation OR
		//      next line is a block start THEN
		//          this is a block end
		// </pre>
		else if (LineIsPossibleBlockContinuationStart(currentLine, indent) &&
			((nextLine == null) ||
				!LineIsBlockContinuation(nextLine, indent) ||
				((BlockStartType != StartEndBlockType.Empty) &&
					LineMatchesBlockStart(nextLine, indent, out _, out _))))
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// The name of the block. For display
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// A list of file assoiations of the type '*.ext' (ie not regexes)
	/// </summary>
	public string[] FileAssociations { get; set; }

	/// <summary>
	/// The type of the start block
	/// </summary>
	public StartEndBlockType BlockStartType { get; set; }

	/// <summary>
	/// The start block string
	/// </summary>
	public string BlockStart { get; set; }

	/// <summary>
	/// Whether the start block string is a reg ex
	/// </summary>
	public bool IsBlockStartRegEx { get; set; }

	/// <summary>
	/// The type of the end block
	/// </summary>
	public StartEndBlockType BlockEndType { get; set; }

	/// <summary>
	/// The end block string
	/// </summary>
	public string BlockEnd { get; set; }

	/// <summary>
	/// Whether the end block string is a reg ex
	/// </summary>
	public bool IsBlockEndRegEx { get; set; }

	/// <summary>
	/// The string for a line continuation. Never a regex
	/// </summary>
	public string LineStart { get; set; }

	/// <summary>
	/// Whether there can only be empty space before the start of block on the first
	/// line of the block
	/// </summary>
	public bool OnlyEmptyLineBeforeStartOfBlock { get; set; }
}
