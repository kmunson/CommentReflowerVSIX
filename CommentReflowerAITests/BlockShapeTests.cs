// Copyright (C) 2026  Kristofel Munson
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

using CommentReflowerLib;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tests of the shapes a block can have: the CommentBlock methods that recognise its
/// lines, called with strings, and reflowing blocks with every kind of start and end,
/// with starts shorter than, as long as and longer than the line start.
/// </summary>
static partial class UnitTests
{
	static CommentBlock DefaultBlock(string name)
	{
		return new ParameterSet().CommentBlocks.Single(b => b.Name == name);
	}

	static void CheckPattern(CommentBlock block, string line, bool expected, int expectedIndentation = 0)
	{
		bool found = block.LineHasBlockPattern(line, out int indentation);
		string what = block.Name + " on '" + line + "'";
		Check(found == expected, "LineHasBlockPattern for " + what + " returned " + found);
		Check(!found || (indentation == expectedIndentation), "LineHasBlockPattern for " + what + " gave an indentation of " + indentation + " not " + expectedIndentation);
	}

	static void CheckStart(CommentBlock block, string line, string previous, int indent, bool expected, string expectedStart = null, int expectedTrimmed = 0)
	{
		bool found = block.LineIsStartOfBlock(line, previous, indent, out string matched, out int trimmed);
		string what = block.Name + " on '" + line + "' after '" + previous + "'";
		Check(found == expected, "LineIsStartOfBlock for " + what + " returned " + found);
		Check(!found || (expectedStart == null) || (matched == expectedStart), "LineIsStartOfBlock for " + what + " matched '" + matched + "' not '" + expectedStart + "'");
		Check(!found || (trimmed == expectedTrimmed), "LineIsStartOfBlock for " + what + " trimmed " + trimmed + " spaces not " + expectedTrimmed);
	}

	static void CheckEnd(CommentBlock block, string line, string next, int indent, bool expected, string expectedEnd = null)
	{
		bool found = block.LineIsEndOfBlock(line, next, indent, out string matched);
		string what = block.Name + " on '" + line + "' before '" + next + "'";
		Check(found == expected, "LineIsEndOfBlock for " + what + " returned " + found);
		Check(!found || (expectedEnd == null) || (matched == expectedEnd), "LineIsEndOfBlock for " + what + " matched '" + matched + "' not '" + expectedEnd + "'");
	}

	static void TestCommentBlockLines()
	{
		CommentBlock cpp = DefaultBlock("C++ style");
		CommentBlock doxygen = DefaultBlock("Doxygen C style (/**)");
		CommentBlock function = DefaultBlock("C style function block");
		CommentBlock hashBlock = DefaultBlock("# Block");

		// LineHasBlockPattern finds a line of the block, and the block's indentation
		CheckPattern(cpp, "    // text", true, 4);
		CheckPattern(cpp, "int x; // text", true, 7);
		CheckPattern(cpp, "    //", true, 4);
		CheckPattern(cpp, "int x;", false);
		CheckPattern(doxygen, "  /** text", true, 2);
		CheckPattern(doxygen, "  /**", true, 2);
		CheckPattern(doxygen, "   * text", true, 2);
		CheckPattern(doxygen, "   */", true, 2);
		CheckPattern(function, "  /*****", true, 2);
		CheckPattern(function, "x /*****", false);
		CheckPattern(function, "  /***** text", false);

		// LineIsStartOfBlock: a block start, with the trailing spaces of the start trimmed
		// if the line ends there
		CheckStart(doxygen, "/** text", null, 0, true, "/** ");
		CheckStart(doxygen, "/**", null, 0, true, "/**", 1);
		CheckStart(doxygen, "  /** text", null, 2, true, "/** ");
		// the start has to be at exactly the indentation given
		CheckStart(doxygen, "  /** text", null, 0, false);
		CheckStart(doxygen, " * text", "/** a", 0, false);
		CheckStart(function, "/*****", null, 0, true, "/*****");
		CheckStart(function, "/***** text", null, 0, false);
		// a block with no start starts where the line before isn't in it
		CheckStart(cpp, "// a", null, 0, true);
		CheckStart(cpp, "// a", "int x;", 0, true);
		CheckStart(cpp, "// a", "// b", 0, false);
		CheckStart(cpp, "    // a", "// b", 4, true);
		// a block whose start is its end is starting if the line before isn't in it
		CheckStart(hashBlock, "###", "# text", 0, false);
		CheckStart(hashBlock, "###", "x = 1", 0, true, "###");
		CheckStart(hashBlock, "###", null, 0, true, "###");

		// LineIsEndOfBlock
		CheckEnd(doxygen, " */", null, 0, true, " */");
		CheckEnd(doxygen, " * text */", null, 0, true, " */");
		CheckEnd(doxygen, " * text", " */", 0, false);
		CheckEnd(cpp, "// a", "// b", 0, false);
		CheckEnd(cpp, "// a", "int x;", 0, true);
		CheckEnd(cpp, "// a", null, 0, true);
		CheckEnd(hashBlock, "###", "# text", 0, false);
		CheckEnd(hashBlock, "###", "x = 1", 0, true, "###");

		// LineIsBlockContinuation needs the line start at the indentation, or the line
		// start without its trailing spaces at the end of the line
		Check(doxygen.LineIsBlockContinuation(" * text", 0), "' * text' is not a continuation at 0");
		Check(!doxygen.LineIsBlockContinuation("  * text", 0), "'  * text' is a continuation at 0");
		Check(doxygen.LineIsBlockContinuation("  * text", 1), "'  * text' is not a continuation at 1");
		Check(doxygen.LineIsBlockContinuation(" *", 0), "' *' is not a continuation at 0");
		Check(!doxygen.LineIsBlockContinuation("text", 0), "'text' is a continuation at 0");
		CommentBlock xml = XmlParameterSet(80, 30, false).CommentBlocks[0];
		Check(xml.LineStartIsOptional && !doxygen.LineStartIsOptional && !cpp.LineStartIsOptional, "LineStartIsOptional is wrong");
		Check(xml.LineIsBlockContinuation("    text", 4) && xml.LineIsBlockContinuation("      text", 4), "An XML line indented enough is not a continuation");
		Check(!xml.LineIsBlockContinuation("  text", 4), "An XML line not indented enough is a continuation");

		// LineIsAtIndentation: a continuation, or the block end at the indentation
		Check(doxygen.LineIsAtIndentation(" */", 0) && doxygen.LineIsAtIndentation("   */", 2), "A block end at the indentation is not at the indentation");
		Check(!doxygen.LineIsAtIndentation("   */", 0), "A block end past the indentation is at the indentation");
		Check(!cpp.LineIsAtIndentation("x */", 0), "A line of a block with no end is at the indentation because of the end");

		Console.WriteLine("CommentBlock line tests PASSED.");
	}

	const string MatrixLineStart = " : ";
	const string MatrixEnd = " }}}";

	/// <summary>
	/// Returns the lines of a block of the given kind holding the given words, indented 4
	/// spaces, as it would be written: the start and end on their own lines if they always
	/// are, and with the text otherwise.
	/// </summary>
	static string[] MatrixInput(StartEndBlockType startType, StartEndBlockType endType, string start, string[] textLines)
	{
		const string indentation = "    ";
		List<string> lines = new();
		if (startType == StartEndBlockType.AlwaysOnOwnLine)
		{
			lines.Add(indentation + start.TrimEnd());
			lines.AddRange(textLines.Select(t => indentation + MatrixLineStart + t));
		}
		else
		{
			lines.Add(indentation + start + textLines[0]);
			lines.AddRange(textLines.Skip(1).Select(t => indentation + MatrixLineStart + t));
		}
		if (endType == StartEndBlockType.NeverOnOwnLine)
		{
			lines[lines.Count - 1] += MatrixEnd;
		}
		else
		{
			lines.Add(indentation + MatrixEnd);
		}
		return lines.ToArray();
	}

	/// <summary>
	/// Returns what is wrong with the reflowed lines of a block of the given kind, which
	/// had the given lines before.
	/// </summary>
	static List<string> CheckBlockShape(
		string[] input,
		string[] output,
		string[] again,
		StartEndBlockType startType,
		StartEndBlockType endType,
		string start,
		string indentation,
		HashSet<string> markers,
		int wrapWidth
	)
	{
		List<string> problems = new();
		if (!Tokens(output).Where(t => !markers.Contains(t)).SequenceEqual(Tokens(input).Where(t => !markers.Contains(t))))
		{
			problems.Add("changed the words");
		}
		if (!again.SequenceEqual(output))
		{
			problems.Add("changed it when reflowed again");
		}
		if (!output.All(l => l.StartsWith(indentation) && ((l.Length == indentation.Length) || (l[indentation.Length] != '\t'))))
		{
			problems.Add("left a line not indented with '" + indentation.Replace("\t", "\\t") + "'");
		}
		bool startAlone = output[0] == indentation + start.TrimEnd();
		if ((startType == StartEndBlockType.NeverOnOwnLine) == startAlone)
		{
			problems.Add("put the start " + (startAlone ? "on" : "not on") + " its own line");
		}
		bool endAlone = output.Last() == indentation + MatrixEnd;
		if ((endType == StartEndBlockType.NeverOnOwnLine) == endAlone)
		{
			problems.Add("put the end " + (endAlone ? "on" : "not on") + " its own line");
		}
		int first = startAlone ? 1 : 0;
		int last = endAlone ? output.Length - 2 : output.Length - 1;
		for (int i = first; i <= last; i++)
		{
			if ((i > 0) && !output[i].StartsWith(indentation + MatrixLineStart))
			{
				problems.Add("left line " + (i + 1) + " without the line start");
			}
			if (ExpandTabs(output[i]).Length > wrapWidth + ((i == last) ? MatrixEnd.Length : 0))
			{
				problems.Add("made line " + (i + 1) + " wider than the wrap width");
			}
		}
		return problems;
	}

	static void TestBlockShapes()
	{
		StartEndBlockType[] types = { StartEndBlockType.AlwaysOnOwnLine, StartEndBlockType.OnOwnLineIfBlockIsMoreThanOne, StartEndBlockType.NeverOnOwnLine };
		// shorter than, as long as and longer than the line start
		string[] starts = { "{ ", "{{ ", "{{{ " };
		string[][] texts =
		{
			new[]
			{
				"This is a comment with enough short words in it that",
				"it has to be wrapped when the wrap width is narrow and that it fills several lines",
				"even at the default width so it is easy to see."
			},
			// a first line that is too long on its own, which is wrapped differently
			new[] { LongComment }
		};
		string[] shortText = { "A short", "comment." };
		const int wrapWidth = 60;
		List<string> failures = new();
		int runs = 0;
		foreach (StartEndBlockType startType in types)
		{
			foreach (StartEndBlockType endType in types)
			{
				foreach (string start in starts)
				{
					foreach (bool useTabs in new[] { false, true })
					{
						runs++;
						ParameterSet pset = new() { UseTabsToIndent = useTabs, WrapWidth = wrapWidth, MinimumBlockWidth = 20 };
						pset.CommentBlocks.Clear();
						pset.CommentBlocks.Add(new CommentBlock(
							"Matrix",
							CommentBlock.CreateFileAssocFromString("*.m"),
							startType,
							startType == StartEndBlockType.AlwaysOnOwnLine ? start.TrimEnd() : start,
							false,
							endType,
							MatrixEnd,
							false,
							MatrixLineStart,
							false
						));
						string what = "a block starting " + startType + " with '" + start + "' and ending " + endType + ", tabs " + (useTabs ? "on" : "off");
						string indentation = useTabs ? "\t" : "    ";
						HashSet<string> markers = new() { start.Trim(), MatrixEnd.Trim(), MatrixLineStart.Trim() };
						List<string> problems = new();
						List<string> shown = new();
						try
						{
							for (int i = 0; i < texts.Length; i++)
							{
								string[] input = MatrixInput(startType, endType, start, texts[i]);
								string[] output = Reflow(pset, ".m", input);
								string[] again = Reflow(pset, ".m", output);
								string which = (i == 0) ? "" : " (first line too long)";
								foreach (string problem in CheckBlockShape(input, output, again, startType, endType, start, indentation, markers, wrapWidth))
								{
									problems.Add(problem + which);
								}
								shown.Add("  " + string.Join(" ", output.Select(l => "|" + l.Replace("\t", "\\t") + "|")));
							}

							// a short comment fits on one line if neither its start nor its end
							// always has a line of its own
							string[] shortOutput = Reflow(pset, ".m", MatrixInput(startType, endType, start, shortText));
							bool oneLine = (startType != StartEndBlockType.AlwaysOnOwnLine) && (endType != StartEndBlockType.AlwaysOnOwnLine);
							int expectedLines = oneLine ? 1 :
								((startType == StartEndBlockType.AlwaysOnOwnLine) ? 1 : 0) + 1 + ((endType == StartEndBlockType.NeverOnOwnLine) ? 0 : 1);
							if (oneLine && !shortOutput.SequenceEqual(new[] { indentation + start + "A short comment." + MatrixEnd }))
							{
								problems.Add("did not put a short comment on one line");
							}
							if (shortOutput.Length != expectedLines)
							{
								problems.Add("did not give a short comment " + expectedLines + " lines");
							}
							shown.Add("  short: " + string.Join(" ", shortOutput.Select(l => "|" + l.Replace("\t", "\\t") + "|")));
						}
						catch (ArgumentException e)
						{
							problems.Add("threw: " + e.Message);
						}
						if (problems.Count > 0)
						{
							failures.Add("Reflowing " + what + " " + string.Join(", ", problems.Distinct()) + ":\n" + string.Join("\n", shown));
						}
					}
				}
			}
		}
		foreach (string failure in failures)
		{
			Console.WriteLine(failure);
		}
		Check(failures.Count == 0, failures.Count + " of " + runs + " block shapes failed");

		Console.WriteLine("Block shape tests PASSED (" + runs + " shapes).");
	}

	static void TestBlockMarkers()
	{
		// regular expression markers keep the length they had
		ParameterSet pset = new() { WrapWidth = 50, MinimumBlockWidth = 20 };
		pset.CommentBlocks.Clear();
		pset.CommentBlocks.Add(new CommentBlock(
			"Regex",
			CommentBlock.CreateFileAssocFromString("*.m"),
			StartEndBlockType.AlwaysOnOwnLine,
			@"\{\{\{+",
			true,
			StartEndBlockType.AlwaysOnOwnLine,
			@" \}\}\}+",
			true,
			MatrixLineStart,
			true
		));
		string[] input = { "    {{{{{{", "     : " + LongComment, "     }}}}}}}}" };
		string[] output = Reflow(pset, ".m", input);
		Check(output.Length > 3, "The block with regular expression markers was not wrapped");
		Check((output.First() == input.First()) && (output.Last() == input.Last()), "The regular expression markers were changed: " + string.Join(" | ", output));
		Check(output.Skip(1).Take(output.Length - 2).All(l => l.StartsWith("     : ") && (l.Length <= 50)), "A line of the block with regular expression markers is wrong");

		// text before the start: allowed or not
		foreach (bool onlySpaceBefore in new[] { false, true })
		{
			pset = new() { WrapWidth = 60, MinimumBlockWidth = 20 };
			pset.CommentBlocks.Clear();
			pset.CommentBlocks.Add(new CommentBlock(
				"After code",
				CommentBlock.CreateFileAssocFromString("*.m"),
				StartEndBlockType.NeverOnOwnLine,
				"{{{ ",
				false,
				StartEndBlockType.NeverOnOwnLine,
				MatrixEnd,
				false,
				MatrixLineStart,
				onlySpaceBefore
			));
			string[] afterCode = { "code(); {{{ " + LongComment + MatrixEnd };
			output = Reflow(pset, ".m", null, afterCode, out bool found);
			if (onlySpaceBefore)
			{
				Check(!found && output.SequenceEqual(afterCode), "A comment after code was reflowed when only spaces can be before its start");
			}
			else
			{
				Check(found && (output.Length > 1), "A comment after code was not reflowed when text can be before its start");
				Check(output[0].StartsWith("code(); {{{ "), "The code before a comment was changed");
				// "code(); " is 8 characters, so the block is indented 8
				Check(output.Skip(1).All(l => l.StartsWith(new string(' ', 8) + MatrixLineStart)), "A line after the first of a comment after code is not indented to the start: " + string.Join(" | ", output));
				Check(Reflow(pset, ".m", output).SequenceEqual(output), "Reflowing a comment after code twice changed it");
			}
		}

		// an XML line start longer than the start: the text of the first line can't be lined
		// up with the text of the lines after it, which start further right
		string[] xml = { "    <!-- " + LongComment + " -->" };
		ParameterSet longLineStart = XmlParameterSet(60, 20, false, "      ");
		output = Reflow(longLineStart, ".xml", xml);
		Check(output.Length > 1, "An XML comment with a line start longer than its start was not wrapped");
		Check(output.Skip(1).All(l => l.StartsWith("          ") && !char.IsWhiteSpace(l[10])), "A line of an XML comment with a line start longer than its start does not start with the line start: " + string.Join(" | ", output));
		Check(Reflow(longLineStart, ".xml", output).SequenceEqual(output), "Reflowing an XML comment with a line start longer than its start twice changed it");
		Console.WriteLine("Block marker tests PASSED.");
	}
}