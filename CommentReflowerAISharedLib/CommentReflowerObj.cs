// Comment Reflower main entry class
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

using Microsoft.VisualStudio.Shell;
using System;
using System.Text.RegularExpressions;

namespace CommentReflowerLib;

public class MatchedBlockData
{
	/// <summary>
	/// The line of the file containg the start of the block
	/// </summary>
	public int StartLine { get; set; }
	/// <summary>
	/// The line of the file containing the end of the block
	/// </summary>
	public int EndLine { get; set; }
	/// <summary>
	/// The indentation of the block within the file
	/// </summary>
	public int Indentation { get; set; }
	/// <summary>
	/// The actual string matches as the start of block.
	/// </summary>
	public string MatchedBlockStart { get; set; }
	/// <summary>
	/// The number of spaces that should be added to the start of block if it is put on
	/// the same line as comment. Only valid if the comment block type was
	/// OnOwnLineIfBlockIsMoreThanOne
	/// </summary>
	public int SpacesTrimmedOffBlockStart { get; set; }
	/// <summary>
	/// The actual string matches as the start of block.
	/// </summary>
	public string MatchedBlockEnd { get; set; }
}

/// <summary>
/// Summary description for CommentReflowerObj.
/// </summary>
public class CommentReflowerObj
{
	public CommentReflowerObj()
	{
	}

	/// <summary>
	/// Wraps all blocks that have any part between the two given text points Returns if
	/// any blocks found.
	/// </summary>
	public static bool WrapAllBlocksInSelection(
		ParameterSet pset,
		string fileName,
		EnvDTE.TextPoint selectionStart,
		EnvDTE.TextPoint selectionEnd
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		bool blockFound = false;
		EnvDTE.EditPoint selStart = selectionStart.CreateEditPoint();
		selStart.StartOfLine();
		while (selStart.LessThan(selectionEnd))
		{
			if (GetBlockContainingPoint(
				pset,
				fileName,
				selStart,
				out CommentBlock block,
				out MatchedBlockData bdata))
			{
				blockFound = true;
				selStart.LineDown((bdata.EndLine - bdata.StartLine) + 1);
				WrapBlock(pset, block, bdata, selectionStart);
			}
			else
			{
				selStart.LineDown(1);
			}
		}
		return blockFound;
	}

	/// <summary>
	/// Wraps the block that contains the given text point. Returns if a block was
	/// found.
	/// </summary>
	public static bool WrapBlockContainingPoint(
		ParameterSet pset,
		string fileName,
		EnvDTE.TextPoint pt
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		if (!GetBlockContainingPoint(
			pset,
			fileName,
			pt,
			out CommentBlock block,
			out MatchedBlockData bdata))
		{
			return false;
		}
		WrapBlock(pset, block, bdata, pt);
		return true;
	}

	/// <summary>
	/// Determines if a block contains the given point, and is fo returns the
	/// CommentBlock and MatchedBlockData for the match.
	/// </summary>
	public static bool GetBlockContainingPoint(
		ParameterSet pset,
		string fileName,
		EnvDTE.TextPoint pt,
		out CommentBlock retblock,
		out MatchedBlockData bdata
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		retblock = null;
		bdata = new MatchedBlockData { EndLine = 0, StartLine = 0, Indentation = 0 };

		EnvDTE.EditPoint ep = pt.CreateEditPoint();
		EnvDTE.EditPoint enddoc = pt.CreateEditPoint();
		enddoc.EndOfDocument();
		string line = GetUntabbedLine(enddoc, ep.Line);

		foreach (CommentBlock block in pset.GetBlocksForFileName(fileName))
		{
			foreach (int indentation in block.GetPossibleIndentations(line))
			{
				bdata.Indentation = indentation;
				int currentLineNumber = ep.Line;
				// scan up for block start
				bool foundStart = false;
				for (; currentLineNumber >= 1; currentLineNumber--)
				{
					string currentLine = GetUntabbedLine(enddoc, currentLineNumber);
					string previousLine = GetUntabbedLine(enddoc, currentLineNumber - 1);
					string nextLine = GetUntabbedLine(enddoc, currentLineNumber + 1);
					if (block.LineIsStartOfBlock(
						currentLine,
						previousLine,
						bdata.Indentation,
						out string matchedBlockStart,
						out int spacesTrimmedOffBlockStart))
					{
						bdata.MatchedBlockStart = matchedBlockStart;
						bdata.SpacesTrimmedOffBlockStart = spacesTrimmedOffBlockStart;
						bdata.StartLine = currentLineNumber;
						foundStart = true;
						break;
					}
					else if (!block.LineIsBlockContinuation(currentLine, bdata.Indentation) &&
						!block.LineIsEndOfBlock(currentLine, nextLine, bdata.Indentation, out _))
					{
						break;
					}
				}
				if (foundStart)
				{
					bool foundEnd = false;
					for (; currentLineNumber <= enddoc.Line; currentLineNumber++)
					{
						string currentLine = GetUntabbedLine(enddoc, currentLineNumber);
						string nextLine = GetUntabbedLine(enddoc, currentLineNumber + 1);
						// A line after the first has to be indented as the block is, even if it
						// has the block end on it, or it can't be reflowed. That isn't so if it was
						// written with a different tab size from the editor's.
						if (block.LineIsEndOfBlock(currentLine, nextLine, bdata.Indentation, out string matchedBlockEnd) &&
							((currentLineNumber == bdata.StartLine) || block.LineIsAtIndentation(currentLine, bdata.Indentation)))
						{
							bdata.MatchedBlockEnd = matchedBlockEnd;
							bdata.EndLine = currentLineNumber;
							foundEnd = true;
							break;
						}
						else if ((currentLineNumber != bdata.StartLine) &&
							(!block.LineIsBlockContinuation(currentLine, bdata.Indentation)))
						{
							break;
						}
					}
					// The block has to include the point. A block whose lines can't be told from
					// the code around it could end before the point.
					if (foundEnd && (ep.Line <= bdata.EndLine))
					{
						retblock = block;
						return true;
					}
					// else try next indentation or block
				}
				// else try next indentation or block
			}
		}
		return false;
	}

	/// <summary>
	/// Returns the line at the given number with any tabs before any non-space
	/// character converted to spaces.
	/// @param enddoc is just used to get the line.
	/// @ret null if the given line is outside the bounds of the document.
	/// </summary>
	private static string GetUntabbedLine(EnvDTE.EditPoint enddoc, int lineNum)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		if ((lineNum < 1) || (lineNum > enddoc.Line))
		{
			return null;
		}
		else
		{
			string temp = enddoc.GetLines(lineNum, lineNum + 1);
			string ret = "";
			for (int i = 0; i < temp.Length; i++)
			{
				if (temp[i] == '\t')
				{
					int spaces = enddoc.Parent.TabSize - (ret.Length % enddoc.Parent.TabSize);
					ret += new System.String(' ', spaces);
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

	/// <summary>
	/// Rewrites the indentation of every line of the block, up to the block's own
	/// indentation, replacing the spaces and tabs that are there. It is written as
	/// tabs followed by any spaces left over, or as spaces only if the tab size is 0.
	/// </summary>
	private static void IndentBlock(
		EnvDTE.TextPoint pt,
		MatchedBlockData bdata,
		int tabSize
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		string indentation = GetIndentationString(1, bdata.Indentation, tabSize);
		EnvDTE.EditPoint lineStart = pt.CreateEditPoint();
		for (int line = bdata.StartLine; line <= bdata.EndLine; line++)
		{
			lineStart.MoveToLineAndOffset(line, 1);
			EnvDTE.EditPoint indentationEnd = lineStart.CreateEditPoint();
			SkipColumns(indentationEnd, bdata.Indentation);
			string existing = lineStart.GetText(indentationEnd);
			// The first line of a block that follows code has that code where the indentation
			// would be, so only whitespace is replaced.
			if ((existing != indentation) && (existing.Trim().Length == 0))
			{
				lineStart.Delete(indentationEnd);
				lineStart.Insert(indentation);
			}
		}
	}
	private static void WrapBlock(
		ParameterSet pset,
		CommentBlock block,
		MatchedBlockData bdata,
		EnvDTE.TextPoint pt
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		int blockTabSize = pt.Parent.TabSize;
		if (!pset.UseTabsToIndent)
		{
			blockTabSize = 0;
		}
		IndentBlock(pt, bdata, blockTabSize);

		bool isInPreformmatedBlock = false;
		bool isLastLine = false;
		bool isStartOnSeparateLine = false;

		EnvDTE.EditPoint curPoint = pt.CreateEditPoint();
		curPoint.MoveToLineAndOffset(bdata.StartLine, 1);

		// seems we have to pick the reight line ending ourselves
		string eol = "\r\n";
		if (curPoint.GetText(-1).Length == 1)
		{
			eol = "\n";
		}

		EnvDTE.EditPoint endPoint = pt.CreateEditPoint();
		endPoint.MoveToLineAndOffset(bdata.EndLine, 1);

		if ((block.BlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) ||
			(block.BlockEndType == StartEndBlockType.NeverOnOwnLine))
		{
			// delete the end block string
			endPoint.StartOfLine();
			SkipColumns(endPoint, bdata.Indentation);
			if (GetTextOnLine(endPoint, bdata.MatchedBlockEnd.Length) == bdata.MatchedBlockEnd)
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
				int endOffset = endPoint.GetText(eolPoint).LastIndexOf(bdata.MatchedBlockEnd);
				endPoint.CharRight(endOffset);
				endPoint.Delete(eolPoint);
			}
		}
		else if (block.BlockEndType == StartEndBlockType.AlwaysOnOwnLine)
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
			if ((curPoint.Line == bdata.StartLine) &&
				(block.BlockStartType == StartEndBlockType.AlwaysOnOwnLine))
			{
				curPoint.LineDown(1); // simply go to the next line
				continue;
			}
			else if ((curPoint.Line == bdata.StartLine) &&
				(block.BlockStartType == StartEndBlockType.NeverOnOwnLine))
			{
				SkipColumns(curPoint, bdata.Indentation);
				SkipString(curPoint, bdata.MatchedBlockStart);
			}
			else if ((curPoint.Line == bdata.StartLine) &&
				(block.BlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne))
			{
				// we will automatically pull up the next line if we can. We also handle the case
				// where trailing spaces from the block start have been removed
				SkipColumns(curPoint, bdata.Indentation);
				SkipString(curPoint, bdata.MatchedBlockStart);

				if ((!AtLineEndIgnoringWhiteSpace(curPoint)) || isLastLine)
				{
					isStartOnSeparateLine = false;
				}
				else if (LineJustContainsContinuation(curPoint.Line + 1, bdata.Indentation, block.LineStart, pset, pt))
				{
					isStartOnSeparateLine = true;
				}
				else
				{
					isStartOnSeparateLine = false;
					if (bdata.SpacesTrimmedOffBlockStart > 0)
					{
						curPoint.Insert(new String(' ', bdata.SpacesTrimmedOffBlockStart));
						// change these as we reprocess line!
						bdata.MatchedBlockStart += new String(' ', bdata.SpacesTrimmedOffBlockStart);
						bdata.SpacesTrimmedOffBlockStart = 0;
					}
					EnvDTE.EditPoint nextLineStartPoint = curPoint.CreateEditPoint();
					nextLineStartPoint.LineDown(1);
					nextLineStartPoint.StartOfLine();
					SkipColumns(nextLineStartPoint, bdata.Indentation);
					SkipLineStart(nextLineStartPoint, block);
					curPoint.Delete(nextLineStartPoint);
					continue; // reprocess the line as it now may be last
				}
			}
			else // just a regular line start
			{
				// pass over line start
				SkipColumns(curPoint, bdata.Indentation);
				if (GetTextOnLine(curPoint, block.LineStart.Length) != block.LineStart)
				{
					if ((block.BlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
						(GetTextOnLine(curPoint, bdata.MatchedBlockEnd.Length) == bdata.MatchedBlockEnd))
					{
						break; // we are done!
					}
					else if (block.LineStartIsOptional)
					{
						// the line leaves out the line start, or has only some of it
						SkipLineStart(curPoint, block);
					}
					else if (GetTextOnLine(curPoint, block.LineStart.TrimEnd().Length) == block.LineStart.TrimEnd())
					{
						curPoint.LineDown(1);
						// empty line with just trimmed block line start
						continue;
					}
					else
					{
						throw new System.ArgumentException("Error parsing block line start");
					}
				}
				else
				{
					curPoint.CharRight(block.LineStart.Length);
				}
			}

			// ASSERT: past the comment block start or comment line start for the line

			int lastStartPre = GetRestOfLine(curPoint).ToLower().LastIndexOf("<pre>");
			int lastEndPre = GetRestOfLine(curPoint).ToLower().LastIndexOf("</pre>");

			// work out if we are in a pre-formatted block
			if ((lastStartPre != -1) && (lastStartPre > lastEndPre))
			{
				isInPreformmatedBlock = true;
			}

			// check all cases that stop wrapping of this line to the next
			if (isInPreformmatedBlock ||
				AtLineEndIgnoringWhiteSpace(curPoint) ||
				pset.MatchesBreakFlowString(GetRestOfLine(curPoint), false))
			{
				if ((lastEndPre != -1) && (lastEndPre > lastStartPre))
				{
					isInPreformmatedBlock = false;
				}
				curPoint.LineDown(1);
				continue;
			}
			bool breakLine = pset.MatchesBreakFlowString(GetRestOfLine(curPoint), true);

			// work out if we are in a pre-formatted block
			if ((lastEndPre != -1) && (lastEndPre > lastStartPre))
			{
				isInPreformmatedBlock = false;
			}

			// work out indent for current line. If it is whitespace, keep it to use for the
			// lines added after this one, so that they match.
			string indentText = null;
			if (pset.MatchesBulletPoint(GetRestOfLine(curPoint), out int thisIndent, out int currentIndent))
			{
				// We need to convert current indent to number of columns as at the moment it is
				// number of characters.
				EnvDTE.EditPoint tempPoint = curPoint.CreateEditPoint();
				tempPoint.CharRight(currentIndent);
				currentIndent = tempPoint.DisplayColumn - curPoint.DisplayColumn;
				// Now advance on this line by the size of the bullet point.
				curPoint.CharRight(thisIndent);
			}
			else
			{
				EnvDTE.EditPoint indentStart = curPoint.CreateEditPoint();
				currentIndent = SkipWhitespace(curPoint);
				indentText = indentStart.GetText(curPoint);
			}
			int startMarkerIndent = 0;
			if ((block.BlockStartType == StartEndBlockType.NeverOnOwnLine) &&
				(curPoint.Line == bdata.StartLine))
			{
				// Need to account that on the first line the block start may have a different
				// length to the line start, so that the text on it lines up with the text on
				// the lines that follow. If those lines are not lined up with it, but start at
				// the line start, the check for wrapping into the next line below takes this
				// back out.
				startMarkerIndent = bdata.MatchedBlockStart.Length - block.LineStart.Length;
				currentIndent += startMarkerIndent;
				indentText = null;
			}

			int blockWrapWidth = GetBlockWrapWidth(pset, bdata, block, currentIndent);

			// Now see if we can wrap into the next line, or if we wrap by inserting a new line
			// before it.
			EnvDTE.EditPoint nextLinePoint = null;
			bool wrapIntoNextLine = false;
			if (!isLastLine && !breakLine)
			{
				nextLinePoint = curPoint.CreateEditPoint();
				nextLinePoint.LineDown(1);
				nextLinePoint.StartOfLine();
				SkipColumns(nextLinePoint, bdata.Indentation);
				if ((block.LineStart.TrimEnd().Length != block.LineStart.Length) &&
					(GetRestOfLine(nextLinePoint).CompareTo(block.LineStart.TrimEnd()) == 0))
				{
					// handle the next line that is completely empty with rimmed line end
					nextLinePoint = null;
				}
				else
				{
					SkipLineStart(nextLinePoint, block);
					int nextLineIndent = -1;
					if (!pset.MatchesBulletPoint(GetRestOfLine(nextLinePoint)) &&
						!pset.MatchesBreakFlowString(GetRestOfLine(nextLinePoint), false))
					{
						nextLineIndent = SkipWhitespace(nextLinePoint);
					}
					if (nextLineIndent == currentIndent)
					{
						wrapIntoNextLine = true;
					}
					else if ((startMarkerIndent != 0) && (nextLineIndent >= 0) && (nextLineIndent == (currentIndent - startMarkerIndent)))
					{
						// The text on the first line is not lined up with the text on the next
						// line, which starts at the line start. Treat the block as having no
						// indent after the line start, so the first line wraps into it, and any
						// lines added wrap with it.
						currentIndent -= startMarkerIndent;
						blockWrapWidth = GetBlockWrapWidth(pset, bdata, block, currentIndent);
						wrapIntoNextLine = true;
					}
					else
					{
						nextLinePoint = null;
					}
				}
			}

			// With no line after the first to show which way the block is laid out, the lines
			// added by wrapping the first line start at the line start if it is blank, as the
			// lines of an XML comment do, and are lined up with the text of the first line if
			// it is not.
			if ((startMarkerIndent != 0) && !wrapIntoNextLine && (block.LineStart.Trim().Length == 0))
			{
				currentIndent -= startMarkerIndent;
				blockWrapWidth = GetBlockWrapWidth(pset, bdata, block, currentIndent);
			}

			// if on the first line and we can't wrap the text into the next line, then push
			// any text on the first line down to a separate line
			if ((block.BlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
				(curPoint.Line == bdata.StartLine) &&
				!wrapIntoNextLine &&
				!isLastLine &&
				!isStartOnSeparateLine)
			{
				isStartOnSeparateLine = true;
				curPoint.StartOfLine();
				SkipColumns(curPoint, bdata.Indentation);
				SkipString(curPoint, bdata.MatchedBlockStart.TrimEnd());
				curPoint.Delete(bdata.MatchedBlockStart.Length - bdata.MatchedBlockStart.TrimEnd().Length);
				curPoint.Insert(eol + GetIndentationString(1, bdata.Indentation, blockTabSize) + block.LineStart);
				continue;
			}

			// ASSERT: if we have got here, there is at least one word on this line, and if
			// wrapIntoNextLine then there is at least one word on the next line

			// now go to the start of the first word that passes the wrap point, or the end of
			// the line if it is sooner
			int wordCount = 0;
			while (((curPoint.DisplayColumn - 1) <= blockWrapWidth) &&
				!AtLineEndIgnoringWhiteSpace(curPoint))
			{
				wordCount++;
				GoToEndOfNextWord(curPoint);
			}

			if ((curPoint.DisplayColumn - 1) <= blockWrapWidth)
			{
				// the end of line occurs at or before the wrap point. Try to fill in the gap from
				// the next line if we can
				if (!wrapIntoNextLine)
				{
					curPoint.LineDown(1);
				}
				else
				{
					int charsToFill = blockWrapWidth - ((curPoint.DisplayColumn - 1) + 1); //+1 for extra space

					EnvDTE.EditPoint nextLineEndPoint = nextLinePoint.CreateEditPoint();
					int numChars = 0;
					int numWords = 0;
					while ((numChars < charsToFill) &&
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
						// the first word on the next line is too long to pull up

						// push comment block start onto separate line and retry if we can
						if ((curPoint.Line == bdata.StartLine) &&
							(block.BlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
							!isStartOnSeparateLine)
						{
							isStartOnSeparateLine = true;
							curPoint.StartOfLine();
							SkipColumns(curPoint, bdata.Indentation);
							SkipString(curPoint, bdata.MatchedBlockStart.TrimEnd());
							curPoint.Delete(bdata.MatchedBlockStart.Length - bdata.MatchedBlockStart.TrimEnd().Length);
							curPoint.Insert(eol + GetIndentationString(1, bdata.Indentation, blockTabSize) + block.LineStart);
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
							// no text left on next line, so delete the whole thing
							nextLineEndPoint.EndOfLine();
							curPoint.Delete(nextLineEndPoint);
							curPoint.Insert(" " + st);
							// don't move curPoint down as we want to reprocess this line in case there is more
							// room left
						}
						else
						{
							// there is still text left on the next line

							// push comment block start onto separate line if necessary
							if ((curPoint.Line == bdata.StartLine) &&
								(block.BlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
								!isStartOnSeparateLine)
							{
								isStartOnSeparateLine = true;
								curPoint.StartOfLine();
								SkipColumns(curPoint, bdata.Indentation);
								SkipString(curPoint, bdata.MatchedBlockStart.TrimEnd());
								curPoint.Delete(bdata.MatchedBlockStart.Length - bdata.MatchedBlockStart.TrimEnd().Length);
								curPoint.Insert(eol + GetIndentationString(1, bdata.Indentation, blockTabSize) + block.LineStart);
								// reprocess this moved down line!
							}
							else
							{
								// remove trailing string as well as trailing spaces
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
				// push start onto separate line if necessary
				if ((curPoint.Line == bdata.StartLine) &&
					(block.BlockStartType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
					!isStartOnSeparateLine)
				{
					isStartOnSeparateLine = true;
					curPoint.StartOfLine();
					SkipColumns(curPoint, bdata.Indentation);
					SkipString(curPoint, bdata.MatchedBlockStart.TrimEnd());
					curPoint.Delete(bdata.MatchedBlockStart.Length - bdata.MatchedBlockStart.TrimEnd().Length);
					curPoint.Insert(eol + GetIndentationString(1, bdata.Indentation, blockTabSize) + block.LineStart);
					// forces reprocess even if isLatLine was just true
					isLastLine = false;
					continue;// reprocess the just moved line
				}

				// this line overflows the word wrap. Move back to the end of the last word that
				// doesn't overflow if possible
				if (wordCount > 1)
				{
					GoToEndOfPreviousWord(curPoint);
				}
				if (AtLineEndIgnoringWhiteSpace(curPoint))
				{
					// the single line is too long, so forget about it and go to the next line
					curPoint.LineDown(1);
				}
				else
				{
					// copy the remainder of the line after a space but delete the space as well
					if (!wrapIntoNextLine)
					{
						EnvDTE.EditPoint dataStartPoint = curPoint.CreateEditPoint();
						SkipWhitespace(dataStartPoint);
						curPoint.Delete(dataStartPoint);
						// insert a whole new line
						curPoint.Insert(eol + GetIndentationString(1, bdata.Indentation, blockTabSize) + block.LineStart);
						if (currentIndent > 0)
						{
							curPoint.Insert(indentText ?? GetIndentationString(curPoint.DisplayColumn, currentIndent, blockTabSize));
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
					// if there is enough there to create one or multiple lines, then create them
					while (true)
					{
						curPoint.StartOfLine();
						SkipColumns(curPoint, bdata.Indentation);
						SkipString(curPoint, block.LineStart);
						SkipColumns(curPoint, currentIndent);
						int myWordCount = 0;
						while ((curPoint.DisplayColumn - 1) <= blockWrapWidth &&
							!AtLineEndIgnoringWhiteSpace(curPoint))
						{
							myWordCount++;
							GoToEndOfNextWord(curPoint);
						}
						if ((curPoint.DisplayColumn - 1) <= blockWrapWidth)
						{
							// don't move down a line as we want to reprocess this line
							break;
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
						curPoint.Insert(eol + GetIndentationString(1, bdata.Indentation, blockTabSize) + block.LineStart);
						if (currentIndent > 0)
						{
							curPoint.Insert(indentText ?? GetIndentationString(curPoint.DisplayColumn, currentIndent, blockTabSize));
						}
						// process the just created line in the next iteration
					}
				}
			}
		}

		if ((block.BlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) &&
			(endPoint.Line != bdata.StartLine))
		{
			endPoint.EndOfLine();
			endPoint.Insert(eol + GetIndentationString(1, bdata.Indentation, blockTabSize) + bdata.MatchedBlockEnd);
		}
		else if ((block.BlockEndType == StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne) ||
			(block.BlockEndType == StartEndBlockType.NeverOnOwnLine))
		{
			endPoint.EndOfLine();
			endPoint.Insert(bdata.MatchedBlockEnd);
		}
	}
	/// <summary>
	/// Helper function returns -1 if the line is empty else returns the amount of
	/// whitespace pt is indented by
	/// </summary>
	private static int SkipWhitespace(
		EnvDTE.EditPoint pt
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
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
			return pt.DisplayColumn - start;
		}
	}

	private static void SkipColumns(
		EnvDTE.EditPoint pt,
		int blockIndentation
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		int start = pt.DisplayColumn;
		int startLine = pt.Line;
		while ((pt.DisplayColumn - start) < blockIndentation)
		{
			pt.CharRight(1);
			if (pt.Line != startLine)
			{
				throw new System.ArgumentException("Error parsing columns");
			}
		}
		if ((pt.DisplayColumn - start) != blockIndentation)
		{
			throw new System.ArgumentException("Error parsing columns");
		}
	}

	/// <summary>
	/// Returns the column to wrap the block at, which is past the wrap width if the
	/// minimum block width needs it to be.
	/// </summary>
	private static int GetBlockWrapWidth(
		ParameterSet pset,
		MatchedBlockData bdata,
		CommentBlock block,
		int currentIndent
	)
	{
		return Math.Max(
			pset.WrapWidth,
			bdata.Indentation + block.LineStart.Length + currentIndent + pset.MinimumBlockWidth
		);
	}

	private static string GetIndentationString(
		int startColumn,
		int numberColumns,
		int tabSize
	)
	{
		if (tabSize > 0)
		{
			string ret = "";
			int endColumn = startColumn + numberColumns;
			while ((startColumn + tabSize) <= endColumn)
			{
				ret += "\t";
				startColumn += tabSize - ((startColumn - 1) % tabSize);
			}
			if (startColumn < endColumn)
			{

				ret += new System.String(' ', endColumn - startColumn);
			}
			return ret;
		}
		else
		{
			return new System.String(' ', numberColumns);
		}
	}

	/// <summary>
	/// Skips the line start of a line of the block. If the block can leave it out, skips
	/// as much of it as the line has, which may be none, and adds the rest to the line
	/// if it has any text, so that the line gets the line start that the lines added
	/// by wrapping will have.
	/// </summary>
	private static void SkipLineStart(EnvDTE.EditPoint pt, CommentBlock block)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		if (!block.LineStartIsOptional)
		{
			SkipString(pt, block.LineStart);
			return;
		}
		int length = 0;
		while ((length < block.LineStart.Length) &&
			(GetTextOnLine(pt, length + 1) == new System.String(' ', length + 1)))
		{
			length++;
		}
		pt.CharRight(length);
		if ((length < block.LineStart.Length) && !AtLineEndIgnoringWhiteSpace(pt))
		{
			pt.Insert(new System.String(' ', block.LineStart.Length - length));
		}
	}

	private static void SkipString(EnvDTE.EditPoint pt, string wanted)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		string actual = GetTextOnLine(pt, wanted.Length);
		if (actual != wanted)
		{
			throw new System.ArgumentException("Error parsing expected string", wanted);
		}
		pt.CharRight(wanted.Length);
	}

	private static string GetTextOnLine(EnvDTE.EditPoint pt, int length)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		if (length > (pt.LineLength - (pt.LineCharOffset - 1)))
		{
			length = pt.LineLength - (pt.LineCharOffset - 1);
		}
		string st = pt.GetText(length);
		return st;
	}

	private static string GetRestOfLine(EnvDTE.EditPoint pt)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		return pt.GetText(pt.LineLength - (pt.LineCharOffset - 1));
	}

	private static bool LineJustContainsContinuation(
		int lineNum,
		int indentation,
		string lineStart,
		ParameterSet pset,
		EnvDTE.TextPoint pt
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		EnvDTE.EditPoint temp = pt.CreateEditPoint();
		temp.MoveToLineAndOffset(lineNum, 1);
		SkipColumns(temp, indentation);
		// note the trim here, which allows the match of blank lines
		SkipString(temp, lineStart.TrimEnd());
		return AtLineEndIgnoringWhiteSpace(temp) ||
			pset.MatchesBreakFlowString(GetRestOfLine(temp), false) ||
			pset.MatchesBulletPoint(GetRestOfLine(temp));
	}

	private static bool AtLineEndIgnoringWhiteSpace(
		EnvDTE.EditPoint pt
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
		EnvDTE.EditPoint temp = pt.CreateEditPoint();
		while ((!temp.AtEndOfLine) &&
			((temp.GetText(1) == " ") ||
			(temp.GetText(1) == "\t")))
		{
			temp.CharRight(1);
		}
		return temp.AtEndOfLine;
	}

	// pre: must be a word before the end of the line moves to the end of the word
	private static void GoToEndOfNextWord(
		EnvDTE.EditPoint pt
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
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

	private static void GoToEndOfPreviousWord(
		EnvDTE.EditPoint pt
	)
	{
#if !UNIT_TEST
		ThreadHelper.ThrowIfNotOnUIThread();
#endif
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
