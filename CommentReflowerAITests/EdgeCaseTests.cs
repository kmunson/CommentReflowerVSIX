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
/// Tests of parts of a comment that are not reflowed like the rest: preformatted
/// sections between &lt;pre&gt; and &lt;/pre&gt;, comments with no text, and blank lines
/// between the paragraphs of a comment whose line start is blank.
/// </summary>
static partial class UnitTests
{
	static string Show(IEnumerable<string> lines)
	{
		return string.Join(" | ", lines.Select(l => l.Replace("\t", "\\t")));
	}

	static void TestPreformatted()
	{
		string[][] inputs =
		{
			new[]
			{
				"// Text before the preformatted part, which is long enough that it has to be wrapped at the wrap width.",
				"// <pre>",
				"//   if (x)   {   y();   }   // spaced out code that is far too long to fit in the wrap width of the block",
				"//     z();",
				"// </pre>",
				"// Text after the preformatted part, which is also long enough that it has to be wrapped at the wrap width."
			},
			new[]
			{
				"/**",
				" * Text before the preformatted part, which is long enough that it has to be wrapped at the wrap width.",
				" * <PRE>",
				" *   if (x)   {   y();   }   // spaced out code that is far too long to fit in the wrap width of the block",
				" *     z();",
				" * </PRE>",
				" * Text after the preformatted part, which is also long enough that it has to be wrapped at the wrap width.",
				" */"
			},
			new[]
			{
				"// An example comes after this text: <pre>",
				"//   if (x)   {   y();   }   // spaced out code that is far too long to fit in the wrap width of the block",
				"// </pre> and this text after it is long enough that it would be wrapped if it were not preformatted.",
				"// This line is after the preformatted part, and is long enough that it has to be wrapped at the wrap width."
			}
		};
		foreach (string[] input in inputs)
		{
			ParameterSet pset = new();
			string[] output = Reflow(pset, ".cpp", input);
			int preStart = Array.FindIndex(input, l => l.ToLower().Contains("<pre>"));
			int preEnd = Array.FindIndex(input, l => l.ToLower().Contains("</pre>"));
			string[] preformatted = input.Skip(preStart).Take(preEnd - preStart + 1).ToArray();
			int outStart = Array.IndexOf(output, preformatted[0]);
			Check(
				(outStart >= 0) && output.Skip(outStart).Take(preformatted.Length).SequenceEqual(preformatted),
				"The preformatted lines were changed: " + Show(output)
			);
			Check(output.Length > input.Length, "The text around the preformatted lines was not wrapped: " + Show(output));
			Check(
				output.Take(outStart).Concat(output.Skip(outStart + preformatted.Length)).All(l => l.Length <= 80),
				"A line outside the preformatted lines is wider than the wrap width: " + Show(output)
			);
			Check(CommentWords(output, pset).SequenceEqual(CommentWords(input, pset)), "Reflowing a comment with preformatted lines changed the words");
			Check(Reflow(pset, ".cpp", output).SequenceEqual(output), "Reflowing a comment with preformatted lines twice changed it");
		}

		Console.WriteLine("Preformatted tests PASSED.");
	}

	static void TestEmptyComments()
	{
		string[][] comments =
		{
			new[] { "//" },
			new[] { "// " },
			new[] { "    //" },
			new[] { "//", "//" },
			new[] { "/* */" },
			new[] { "/**/" },
			new[] { "/** */" },
			new[] { "/**", " */" },
			new[] { "/**", " *", " */" },
			new[] { "/*****", " *****/" },
			new[] { "/*! */" },
			new[] { "/* ", " */" }
		};
		foreach (string[] comment in comments)
		{
			foreach (bool withCode in new[] { false, true })
			{
				string[] input = withCode ? new[] { "int x;" }.Concat(comment).Concat(new[] { "int y;" }).ToArray() : comment;
				ParameterSet pset = new();
				string[] output;
				try
				{
					output = Reflow(pset, ".cpp", input);
				}
				catch (ArgumentException e)
				{
					throw new ApplicationException("Reflowing the empty comment " + Show(input) + " threw: " + e.Message);
				}
				// a comment on one line is left as it is; one on more lines may be put on one,
				// as "/**" and " */" become "/** */", but reflowing it again changes nothing
				if (comment.Length == 1)
				{
					Check(output.SequenceEqual(input), "Reflowing the empty comment " + Show(input) + " changed it to " + Show(output));
				}
				Check(Reflow(pset, ".cpp", output).SequenceEqual(output), "Reflowing the empty comment " + Show(input) + " twice changed it");
			}
		}

		string[][] xmlComments = { new[] { "<!-- -->" }, new[] { "    <!-- -->" }, new[] { "<!---->" }, new[] { "<!--", "-->" } };
		foreach (string[] comment in xmlComments)
		{
			foreach (string lineStart in new[] { "", "     " })
			{
				string[] output;
				try
				{
					output = Reflow(XmlParameterSet(80, 30, false, lineStart), ".xml", comment);
				}
				catch (ArgumentException e)
				{
					throw new ApplicationException("Reflowing the empty XML comment " + Show(comment) + " threw: " + e.Message);
				}
				Check((comment.Length > 1) || output.SequenceEqual(comment), "Reflowing the empty XML comment " + Show(comment) + " changed it to " + Show(output));
				Check(Reflow(XmlParameterSet(80, 30, false, lineStart), ".xml", output).SequenceEqual(output), "Reflowing the empty XML comment " + Show(comment) + " twice changed it");
			}
		}

		Console.WriteLine("Empty comment tests PASSED.");
	}

	/// <summary>
	/// A blank line between paragraphs of a comment whose line start is blank, as in XML,
	/// is in the comment, so the comment is found from any of its lines and each
	/// paragraph is reflowed on its own.
	/// </summary>
	static void TestBlankLinesInComment()
	{
		foreach (string blank in new[] { "", "  ", "    " })
		{
			foreach (string lineStart in new[] { "", "     " })
			{
				string[] input =
				{
					"  <Before />",
					"    <!-- First paragraph that is long enough that it has to be wrapped when the wrap width is narrow.",
					"    It continues here.",
					blank,
					"    Second paragraph, which is also long enough that it has to be wrapped at a narrow width. -->",
					"  <After />"
				};
				string what = " with a blank line of " + blank.Length + " spaces and a line start of " + lineStart.Length + " spaces";
				ParameterSet pset = XmlParameterSet(60, 20, false, lineStart);

				TestFileWrapper document = OpenDocument(".xml", input, out string fileName);
				for (int line = 1; line <= input.Length; line++)
				{
					EnvDTE.EditPoint pt = document.CreateEditPoint(null);
					pt.MoveToLineAndOffset(line, 1);
					bool found = CommentReflowerObj.GetBlockContainingPoint(pset, fileName, pt, out _, out MatchedBlockData bdata);
					bool inside = (line >= 2) && (line <= 5);
					Check(found == inside, "The comment was " + (found ? "" : "not ") + "found at line " + line + what);
					Check(!found || ((bdata.StartLine == 2) && (bdata.EndLine == 5)), "The comment found at line " + line + what + " is lines " + bdata.StartLine + " to " + bdata.EndLine);
				}

				string[] output;
				try
				{
					output = Reflow(pset, ".xml", input);
				}
				catch (ArgumentException e)
				{
					throw new ApplicationException("Reflowing the comment" + what + " threw: " + e.Message);
				}
				int blankLine = Array.FindIndex(output, l => l.Trim().Length == 0);
				Check(blankLine > 2, "The blank line is missing or the first paragraph was not wrapped" + what + ": " + Show(output));
				Check(output.Count(l => l.Trim().Length == 0) == 1, "The comment has more than one blank line" + what + ": " + Show(output));
				Check(output.Skip(blankLine + 1).First().TrimStart().StartsWith("Second paragraph"), "The paragraphs were joined" + what + ": " + Show(output));
				Check(output.Length > input.Length + 1, "The paragraphs were not wrapped" + what + ": " + Show(output));
				Check(output.Where((l, i) => (i > 0) && (i < output.Length - 2)).All(l => l.Length <= 60), "A line is wider than the wrap width" + what + ": " + Show(output));
				Check(Tokens(output).SequenceEqual(Tokens(input)), "Reflowing the comment" + what + " changed the words");
				Check(Reflow(pset, ".xml", output).SequenceEqual(output), "Reflowing the comment" + what + " twice changed it");
			}
		}

		Console.WriteLine("Blank lines in a comment tests PASSED.");
	}
}