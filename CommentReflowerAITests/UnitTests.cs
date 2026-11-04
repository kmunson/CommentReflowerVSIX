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

using CommentReflowerLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// Tests of the settings model and of the reflow engine with settings other than the
/// defaults. Unlike the golden file tests in MainClass, these check properties of the
/// result, or the result of a single call, and need no Compare files.
/// </summary>
static class UnitTests
{
	const int TabSize = 4;

	static void Check(bool ok, string message)
	{
		if (!ok)
		{
			throw new ApplicationException(message);
		}
	}

	static void CheckThrows(Action action, string message)
	{
		try
		{
			action();
		}
		catch (ArgumentException)
		{
			return;
		}
		throw new ApplicationException(message + " did not throw an ArgumentException");
	}

	static void CheckNotThrows(Action action, string message)
	{
		try
		{
			action();
		}
		catch (ArgumentException e)
		{
			throw new ApplicationException(message + " threw an ArgumentException: " + e.Message);
		}
	}

	/// <summary>
	/// Compares two objects property by property, recursing into collections and
	/// arrays, so that a property added later is compared without changing the test.
	/// </summary>
	static bool DeepEqual(object a, object b)
	{
		if ((a == null) || (b == null))
		{
			return a == b;
		}
		Type t = a.GetType();
		if (t != b.GetType())
		{
			return false;
		}
		if (t.IsPrimitive || t.IsEnum || (t == typeof(string)))
		{
			return a.Equals(b);
		}
		if (a is IEnumerable ea)
		{
			List<object> la = ea.Cast<object>().ToList();
			List<object> lb = ((IEnumerable)b).Cast<object>().ToList();
			return (la.Count == lb.Count) && la.Zip(lb, DeepEqual).All(x => x);
		}
		foreach (PropertyInfo p in t.GetProperties())
		{
			if (!DeepEqual(p.GetValue(a, null), p.GetValue(b, null)))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Checks that every public property of the object has a non-default value, so a
	/// copy that leaves a property out can't compare equal by accident.
	/// </summary>
	static void CheckNoPropertyIsDefault(object o)
	{
		foreach (PropertyInfo p in o.GetType().GetProperties())
		{
			object v = p.GetValue(o, null);
			bool isDefault = v == null ||
				(v is string s && s.Length == 0) ||
				(v is IEnumerable e && !e.Cast<object>().Any()) ||
				(p.PropertyType.IsValueType && v.Equals(Activator.CreateInstance(p.PropertyType)));
			Check(!isDefault, o.GetType().Name + "." + p.Name + " is a default value in the test object");
		}
	}

	////////////////////////////////////////
	// file associations
	////////////////////////////////////////

	static List<string> BlockNamesFor(ParameterSet pset, string fileName)
	{
		return pset.GetBlocksForFileName(fileName).Select(b => b.Name).ToList();
	}

	static void TestFileAssociations()
	{
		string[] assoc = CommentBlock.CreateFileAssocFromString("*.a; *.b ;c");
		Check(
			assoc.SequenceEqual(new[] { "*.a", "*.b", "c" }),
			"CreateFileAssocFromString did not split on ; and trim the entries"
		);
		CheckThrows(() => CommentBlock.CreateFileAssocFromString(""), "CreateFileAssocFromString with an empty string");
		CheckThrows(() => CommentBlock.CreateFileAssocFromString("  "), "CreateFileAssocFromString with spaces");

		ParameterSet pset = new();
		foreach (string fileName in new[] { "x.cpp", "X.CPP", @"c:\dir\x.cpp", "x.h", "x.cs" })
		{
			List<string> names = BlockNamesFor(pset, fileName);
			Check(names.Contains("C++ style"), "GetBlocksForFileName(" + fileName + ") has no C++ style block");
			Check(names.Contains("C style"), "GetBlocksForFileName(" + fileName + ") has no C style block");
			Check(
				!names.Any(n => n.StartsWith("Visual Basic")),
				"GetBlocksForFileName(" + fileName + ") has a Visual Basic block"
			);
			Check(!names.Contains("# Comment"), "GetBlocksForFileName(" + fileName + ") has a # block");
		}
		foreach (string fileName in new[] { "x.vb", "X.VBS" })
		{
			List<string> names = BlockNamesFor(pset, fileName);
			Check(
				names.Contains("Visual Basic Single Quote (')"),
				"GetBlocksForFileName(" + fileName + ") has no Visual Basic block"
			);
			Check(!names.Contains("C++ style"), "GetBlocksForFileName(" + fileName + ") has a C++ block");
		}
		foreach (string fileName in new[] { "Jamfile", "Jamrules", "x.jam", @"c:\dir\Jamfile" })
		{
			Check(
				BlockNamesFor(pset, fileName).Contains("# Comment"),
				"GetBlocksForFileName(" + fileName + ") has no # block"
			);
		}
		Check(
			BlockNamesFor(pset, "x.txt").SequenceEqual(new[] { "Empty Block for Text Files" }),
			"GetBlocksForFileName(x.txt) is not just the text file block"
		);
		Check(BlockNamesFor(pset, "x.unknown").Count == 0, "GetBlocksForFileName(x.unknown) found a block");

		Console.WriteLine("File association tests PASSED.");
	}

	////////////////////////////////////////
	// validation
	////////////////////////////////////////

	static void TestValidation()
	{
		ParameterSet pset = new();
		CheckNotThrows(pset.ValidateGeneralSettings, "Validating the default settings");
		for (int i = 0; i < pset.CommentBlocks.Count; i++)
		{
			int index = i;
			CheckNotThrows(() => pset.ValidateCommentBlock(index), "Validating default comment block " + index);
		}
		for (int i = 0; i < pset.BulletPoints.Count; i++)
		{
			int index = i;
			CheckNotThrows(() => pset.ValidateBullet(index), "Validating default bullet " + index);
		}
		for (int i = 0; i < pset.BreakFlowStrings.Count; i++)
		{
			int index = i;
			CheckNotThrows(() => pset.ValidateBreakFlowString(index), "Validating default break flow string " + index);
		}

		// general settings
		pset = new() { WrapWidth = 9 };
		CheckThrows(pset.ValidateGeneralSettings, "Validating a wrap width of 9");
		pset = new() { WrapWidth = 10, MinimumBlockWidth = 10 };
		CheckNotThrows(pset.ValidateGeneralSettings, "Validating a wrap width and minimum block width of 10");
		pset = new() { MinimumBlockWidth = 9 };
		CheckThrows(pset.ValidateGeneralSettings, "Validating a minimum block width of 9");

		// comment blocks
		pset = new();
		pset.CommentBlocks[0].Name = "  ";
		CheckThrows(() => pset.ValidateCommentBlock(0), "Validating a comment block with a blank name");

		pset = new();
		pset.CommentBlocks[1].Name = pset.CommentBlocks[0].Name;
		CheckThrows(() => pset.ValidateCommentBlock(1), "Validating a comment block with a duplicate name");
		CheckThrows(() => pset.ValidateCommentBlock(0), "Validating a comment block whose name is used again");

		pset = new();
		pset.CommentBlocks[0].BlockStart = "  ";
		pset.CommentBlocks[0].IsBlockStartRegEx = false;
		CheckThrows(() => pset.ValidateCommentBlock(0), "Validating a comment block with a blank block start");

		pset = new();
		pset.CommentBlocks[0].BlockStart = "(";
		pset.CommentBlocks[0].IsBlockStartRegEx = true;
		CheckThrows(() => pset.ValidateCommentBlock(0), "Validating a comment block with an invalid start regex");
		pset.CommentBlocks[0].IsBlockStartRegEx = false;
		CheckNotThrows(() => pset.ValidateCommentBlock(0), "Validating a start of ( that is not a regex");

		pset = new();
		pset.CommentBlocks[0].BlockEnd = "  ";
		pset.CommentBlocks[0].IsBlockEndRegEx = false;
		CheckThrows(() => pset.ValidateCommentBlock(0), "Validating a comment block with a blank block end");

		pset = new();
		pset.CommentBlocks[0].BlockEnd = "(";
		pset.CommentBlocks[0].IsBlockEndRegEx = true;
		CheckThrows(() => pset.ValidateCommentBlock(0), "Validating a comment block with an invalid end regex");

		pset = new();
		pset.CommentBlocks[0].BlockStartType = StartEndBlockType.Empty;
		pset.CommentBlocks[0].BlockStart = "left over";
		pset.CommentBlocks[0].BlockEndType = StartEndBlockType.Empty;
		pset.CommentBlocks[0].BlockEnd = "left over";
		CheckNotThrows(() => pset.ValidateCommentBlock(0), "Validating a comment block with empty start and end");
		Check(
			(pset.CommentBlocks[0].BlockStart == "") && (pset.CommentBlocks[0].BlockEnd == ""),
			"Validating a comment block with empty start and end did not clear the strings"
		);

		// bullets
		pset = new();
		pset.BulletPoints[0].Name = "";
		CheckThrows(() => pset.ValidateBullet(0), "Validating a bullet with an empty name");

		pset = new();
		pset.BulletPoints[1].Name = pset.BulletPoints[0].Name;
		CheckThrows(() => pset.ValidateBullet(1), "Validating a bullet with a duplicate name");

		pset = new();
		pset.BulletPoints[0].MatchString = "(";
		pset.BulletPoints[0].IsRegEx = true;
		CheckThrows(() => pset.ValidateBullet(0), "Validating a bullet with an invalid regex");
		pset.BulletPoints[0].IsRegEx = false;
		CheckNotThrows(() => pset.ValidateBullet(0), "Validating a bullet of ( that is not a regex");

		// force line breaks
		pset = new();
		pset.BreakFlowStrings[0].Name = " ";
		CheckThrows(() => pset.ValidateBreakFlowString(0), "Validating a force line break with a blank name");

		pset = new();
		pset.BreakFlowStrings[1].Name = pset.BreakFlowStrings[0].Name;
		CheckThrows(() => pset.ValidateBreakFlowString(1), "Validating a force line break with a duplicate name");

		pset = new();
		pset.BreakFlowStrings[0].MatchString = "[";
		pset.BreakFlowStrings[0].IsRegEx = true;
		CheckThrows(() => pset.ValidateBreakFlowString(0), "Validating a force line break with an invalid regex");
		pset.BreakFlowStrings[0].IsRegEx = false;
		CheckNotThrows(() => pset.ValidateBreakFlowString(0), "Validating a force line break of [ that is not a regex");

		Console.WriteLine("Validation tests PASSED.");
	}

	////////////////////////////////////////
	// copy constructors
	////////////////////////////////////////

	static void TestCopyConstructors()
	{
		CommentBlock block = new(
			"name",
			new[] { "*.a", "*.b" },
			StartEndBlockType.AlwaysOnOwnLine,
			"start",
			true,
			StartEndBlockType.NeverOnOwnLine,
			"end",
			true,
			"line start",
			true
		);
		CheckNoPropertyIsDefault(block);
		CommentBlock blockCopy = new(block);
		Check(DeepEqual(block, blockCopy), "The CommentBlock copy is not equal to the original");
		block.FileAssociations[0] = "changed";
		Check(blockCopy.FileAssociations[0] == "*.a", "The CommentBlock copy shares its file associations");

		BulletPoint bullet = new("name", "match", true, true);
		CheckNoPropertyIsDefault(bullet);
		Check(DeepEqual(bullet, new BulletPoint(bullet)), "The BulletPoint copy is not equal to the original");

		BreakFlowString breakFlow = new("name", "match", true, true, true);
		CheckNoPropertyIsDefault(breakFlow);
		Check(DeepEqual(breakFlow, new BreakFlowString(breakFlow)), "The BreakFlowString copy is not equal to the original");

		ParameterSet pset = new()
		{
			UseTabsToIndent = true,
			WrapWidth = 61,
			MinimumBlockWidth = 17
		};
		CheckNoPropertyIsDefault(pset);
		ParameterSet psetCopy = new(pset);
		Check(DeepEqual(pset, psetCopy), "The ParameterSet copy is not equal to the original");
		Check(
			(psetCopy.CommentBlocks.Count > 0) && (psetCopy.BulletPoints.Count > 0) && (psetCopy.BreakFlowStrings.Count > 0),
			"The ParameterSet copy has an empty list"
		);

		// the copy is deep: changing the original must not change it
		string commentBlockName = psetCopy.CommentBlocks[0].Name;
		string bulletName = psetCopy.BulletPoints[0].Name;
		string breakFlowName = psetCopy.BreakFlowStrings[0].Name;
		int commentBlockCount = psetCopy.CommentBlocks.Count;
		pset.CommentBlocks[0].Name = "changed";
		pset.BulletPoints[0].Name = "changed";
		pset.BreakFlowStrings[0].Name = "changed";
		pset.CommentBlocks.RemoveAt(1);
		pset.WrapWidth = 99;
		Check(
			(psetCopy.CommentBlocks[0].Name == commentBlockName) &&
			(psetCopy.BulletPoints[0].Name == bulletName) &&
			(psetCopy.BreakFlowStrings[0].Name == breakFlowName) &&
			(psetCopy.CommentBlocks.Count == commentBlockCount) &&
			(psetCopy.WrapWidth == 61),
			"The ParameterSet copy shares state with the original"
		);

		Console.WriteLine("Copy constructor tests PASSED.");
	}

	////////////////////////////////////////
	// bullet and force line break matching
	////////////////////////////////////////

	static void CheckBullet(
		BulletPoint bullet,
		string line,
		bool expectedMatch,
		int expectedThisIndent = 0,
		int expectedNextLineIndent = 0
	)
	{
		bool match = bullet.Matches(line, out int thisIndent, out int nextLineIndent);
		string what = "Bullet '" + bullet.MatchString + "' on '" + line + "'";
		Check(match == expectedMatch, what + " returned " + match);
		Check(thisIndent == expectedThisIndent, what + " has indent " + thisIndent + " not " + expectedThisIndent);
		Check(
			nextLineIndent == expectedNextLineIndent,
			what + " has next line indent " + nextLineIndent + " not " + expectedNextLineIndent
		);
	}

	static void TestBulletMatching()
	{
		// a literal string, with the wrapped lines under the bullet or under the text
		CheckBullet(new("literal", "- ", false, false), "  - text", true, 4, 2);
		CheckBullet(new("literal right", "- ", false, true), "  - text", true, 4, 4);
		CheckBullet(new("literal", "- ", false, false), "text - more", false);
		CheckBullet(new("literal", "- ", false, false), "", false);

		// a literal string is not a regex
		CheckBullet(new("star", "*) ", false, true), "* ) x", false);
		CheckBullet(new("star", "*) ", false, true), " *) x", true, 4, 4);

		// a regex without a group is wrapped in one
		CheckBullet(new("number", @"[0-9]+\) ", true, true), "  12) text", true, 6, 6);
		CheckBullet(new("number", @"[0-9]+\) ", true, true), "ab) text", false);

		// a regex that starts with a group is used as it is
		CheckBullet(new("group", @"(\d+\.) ", true, false), "3. text", true, 2, 0);

		// the bullets in the default settings
		ParameterSet pset = new();
		Check(pset.MatchesBulletPoint("- text"), "The default bullets do not match '- text'");
		Check(pset.MatchesBulletPoint("  @param x text"), "The default bullets do not match '@param x text'");
		Check(!pset.MatchesBulletPoint("text"), "The default bullets match 'text'");
		Check(pset.MatchesBulletPoint("1) text", out int thisIndent, out int nextLineIndent), "The default bullets do not match '1) text'");
		Check((thisIndent == 3) && (nextLineIndent == 3), "The default bullets give the wrong indent for '1) text'");

		Console.WriteLine("Bullet matching tests PASSED.");
	}

	static void TestBreakFlowMatching()
	{
		BreakFlowString never = new("never", "$Id", false, true, true);
		Check(never.Matches("a $Id: x $", false), "A literal never-reflow string did not match");
		Check(never.Matches("a $Id: x $", true), "A literal never-reflow string did not match with the next line");
		Check(!never.Matches("a Id", false), "A literal never-reflow string matched a line without it");
		BreakFlowString dot = new("dot", "a.c", false, true, true);
		Check(dot.Matches("a.c", false), "A literal string did not match itself");
		Check(!dot.Matches("abc", false), "A literal string was used as a regex");
		BreakFlowString dotRegex = new("dot regex", "a.c", true, true, true);
		Check(dotRegex.Matches("abc", false), "A regex string was not used as a regex");

		BreakFlowString intoNext = new("into next", "<BR>", false, false, true);
		Check(!intoNext.Matches("a <BR>", false), "A never-flow-into-next-line string matched without the next line");
		Check(intoNext.Matches("a <BR>", true), "A never-flow-into-next-line string did not match with the next line");

		BreakFlowString neither = new("neither", "x", false, false, false);
		Check(!neither.Matches("x", false) && !neither.Matches("x", true), "A string with neither option matched");

		ParameterSet pset = new();
		Check(pset.MatchesBreakFlowString("a $Id: x $", false), "The default settings do not match a $Id line");
		Check(!pset.MatchesBreakFlowString("a <BR>", false), "The default settings match <BR> without the next line");
		Check(pset.MatchesBreakFlowString("a <BR>", true), "The default settings do not match <BR> with the next line");
		Check(!pset.MatchesBreakFlowString("plain text", true), "The default settings match plain text");

		Console.WriteLine("Break flow matching tests PASSED.");
	}

	////////////////////////////////////////
	// reflowing with settings other than the defaults
	////////////////////////////////////////

	const string LongComment =
		"This is a comment with enough short words in it that it has to be wrapped when the wrap " +
		"width is narrow and that it fills several lines even at the default width so it is easy to see.";

	static string ExpandTabs(string line)
	{
		string ret = "";
		foreach (char c in line)
		{
			ret += (c == '\t') ? new string(' ', TabSize - (ret.Length % TabSize)) : c.ToString();
		}
		return ret;
	}

	static string[] Words(IEnumerable<string> lines, string commentStart)
	{
		return string.Join(" ", lines)
			.Replace(commentStart, " ")
			.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
	}

	/// <summary>
	/// Reflows every block in the given lines as if they were in a file with the given
	/// extension, and returns the resulting lines.
	/// </summary>
	static string[] Reflow(ParameterSet pset, string extension, params string[] lines)
	{
		return Reflow(pset, extension, null, lines, out _);
	}

	static string[] Reflow(
		ParameterSet pset,
		string extension,
		string[] selection,
		string[] lines,
		out bool blockFound
	)
	{
		string dir = Path.Combine(Path.GetTempPath(), "CommentReflowerAITests");
		Directory.CreateDirectory(dir);
		string fileName = Path.Combine(dir, "UnitTest" + extension);
		File.WriteAllLines(fileName, lines);
		TestFileWrapper file = new(fileName);
		EnvDTE.EditPoint start = file.CreateEditPoint(null);
		EnvDTE.EditPoint end = file.CreateEditPoint(null);
		if (selection == null)
		{
			start.StartOfDocument();
			end.EndOfDocument();
		}
		else
		{
			start.MoveToLineAndOffset(int.Parse(selection[0]), 1);
			end.MoveToLineAndOffset(int.Parse(selection[1]), 5);
		}
		blockFound = CommentReflowerObj.WrapAllBlocksInSelection(pset, fileName, start, end);
		string outName = fileName + ".out";
		file.WriteToFile(outName);
		return File.ReadAllLines(outName);
	}

	static void TestWrapWidth()
	{
		string[] input = { "\t\t// " + LongComment };
		string[] wide = Reflow(new ParameterSet(), ".cpp", input);
		string[] narrow = Reflow(new ParameterSet { WrapWidth = 40, MinimumBlockWidth = 10 }, ".cpp", input);

		Check(wide.All(l => ExpandTabs(l).Length <= 80), "A line is wider than the default wrap width");
		Check(narrow.All(l => ExpandTabs(l).Length <= 40), "A line is wider than a wrap width of 40");
		Check(narrow.Length > wide.Length, "A narrower wrap width did not give more lines");
		Check(narrow.Length > 1, "The comment was not wrapped at a wrap width of 40");
		Check(narrow.All(l => l.TrimStart('\t', ' ').StartsWith("// ")), "A wrapped line does not start with //");
		Check(
			Words(wide, "//").SequenceEqual(Words(input, "//")) && Words(narrow, "//").SequenceEqual(Words(input, "//")),
			"Reflowing changed the words"
		);

		// a second pass changes nothing
		Check(Reflow(new ParameterSet { WrapWidth = 40, MinimumBlockWidth = 10 }, ".cpp", narrow).SequenceEqual(narrow), "Reflowing twice at a wrap width of 40 changed the result");

		Console.WriteLine("Wrap width tests PASSED.");
	}

	static void TestMinimumBlockWidth()
	{
		// The block is indented 8 columns and its lines start with "// ", so with a
		// minimum width of 30 the wrap column is 8 + 3 + 30 = 41, not the wrap width of 20.
		string[] input = { "\t\t// " + LongComment };
		string[] output = Reflow(new ParameterSet { WrapWidth = 20, MinimumBlockWidth = 30 }, ".cpp", input);
		Check(output.All(l => ExpandTabs(l).Length <= 41), "A line is wider than the indentation plus the minimum block width");
		Check(output.Any(l => ExpandTabs(l).Length > 30), "The minimum block width did not widen the block");
		Check(Words(output, "//").SequenceEqual(Words(input, "//")), "Reflowing changed the words");

		Console.WriteLine("Minimum block width tests PASSED.");
	}

	static void TestIndentation()
	{
		string[] input = { "\t\t// " + LongComment };

		// The first line keeps the indentation it had. The lines added by wrapping get
		// the block's indentation in tabs or in spaces, depending on the setting.
		string[] spaces = Reflow(new ParameterSet { UseTabsToIndent = false }, ".cpp", input);
		Check(spaces.Length > 1, "The comment was not wrapped");
		Check(spaces.Skip(1).All(l => l.StartsWith("        // ")), "An added line does not start with 8 spaces");
		Check(spaces.Skip(1).All(l => !l.Contains('\t')), "An added line has a tab when tabs are not used to indent");

		string[] tabs = Reflow(new ParameterSet { UseTabsToIndent = true }, ".cpp", input);
		Check(tabs.All(l => l.StartsWith("\t\t// ")), "A line does not start with 2 tabs");

		// the same text apart from the indentation of the lines
		Check(
			spaces.Select(l => l.TrimStart('\t', ' ')).SequenceEqual(tabs.Select(l => l.TrimStart('\t', ' '))),
			"Tabs and spaces to indent give different text"
		);

		Console.WriteLine("Indentation tests PASSED.");
	}

	static void TestLongWord()
	{
		string longWord = new('x', 100);
		string[] input = { "// a short start " + longWord + " and a short end that goes on a little" };
		string[] output = Reflow(new ParameterSet { WrapWidth = 40, MinimumBlockWidth = 10 }, ".cpp", input);

		Check(output.Any(l => l.Contains(longWord)), "A word longer than the wrap width was split");
		Check(Words(output, "//").SequenceEqual(Words(input, "//")), "Reflowing a long word changed the words");

		Console.WriteLine("Long word tests PASSED.");
	}

	static void TestNoBlock()
	{
		string[] code = { "int x;", "int y = x + 1;", "", "return y;" };
		string[] output = Reflow(new ParameterSet(), ".cpp", null, code, out bool blockFound);
		Check(!blockFound, "A block was found in a file with no comments");
		Check(output.SequenceEqual(code), "A file with no comments was changed");

		// a file type with no block
		string[] comment = { "// " + LongComment };
		output = Reflow(new ParameterSet(), ".unknown", null, comment, out blockFound);
		Check(!blockFound, "A block was found in a file of an unknown type");
		Check(output.SequenceEqual(comment), "A file of an unknown type was changed");

		Console.WriteLine("No block tests PASSED.");
	}

	static void TestBlocksAtEdges()
	{
		// a comment on the first and on the last line of the file
		string[] input =
		{
			"// " + LongComment,
			"int x;",
			"// " + LongComment
		};
		string[] output = Reflow(new ParameterSet(), ".cpp", input);
		Check(output.Length > 3, "The comments on the first and last lines were not wrapped");
		Check(output.Count(l => l == "int x;") == 1, "The code line was changed");
		int codeLine = Array.IndexOf(output, "int x;");
		Check(
			output.Take(codeLine).All(l => l.StartsWith("// ")) && output.Skip(codeLine + 1).All(l => l.StartsWith("// ")),
			"A line around the code line is not a comment"
		);
		Check(
			Words(output.Take(codeLine), "//").SequenceEqual(Words(new[] { input[0] }, "//")) &&
			Words(output.Skip(codeLine + 1), "//").SequenceEqual(Words(new[] { input[2] }, "//")),
			"Reflowing the comments at the ends of the file changed the words"
		);

		// a comment that is already short is left alone
		string[] shortComment = { "// short" };
		Check(Reflow(new ParameterSet(), ".cpp", shortComment).SequenceEqual(shortComment), "A short comment was changed");

		Console.WriteLine("Blocks at the edges of the file tests PASSED.");
	}

	static void TestSelection()
	{
		string[] input =
		{
			"// " + LongComment + " (first)",
			"int a;",
			"// " + LongComment + " (second)",
			"int b;",
			"// " + LongComment + " (third)"
		};
		// from the start of the first comment into the middle of the second one
		string[] output = Reflow(new ParameterSet(), ".cpp", new[] { "1", "3" }, input, out bool blockFound);
		Check(blockFound, "No block was found in the selection");
		Check(!output.Contains(input[0]), "The first comment was not wrapped");
		Check(!output.Contains(input[2]), "The second comment was not wrapped");
		int lastLine = output.Length - 1;
		Check(output.Take(lastLine).All(l => l.Length <= 80), "A line of the selected comments is wider than the wrap width");
		Check(
			(output[lastLine] == input[4]) && (output[lastLine - 1] == "int b;"),
			"The third comment, which is after the selection, or the code before it was changed"
		);

		// a selection that is only in the code
		output = Reflow(new ParameterSet(), ".cpp", new[] { "2", "2" }, input, out blockFound);
		Check(!blockFound, "A block was found in a selection of only code");
		Check(output.SequenceEqual(input), "A selection of only code changed the file");

		Console.WriteLine("Selection tests PASSED.");
	}

	static void TestCustomBlock()
	{
		ParameterSet pset = new();
		pset.CommentBlocks.Add(new CommentBlock(
			"Semicolon comment",
			CommentBlock.CreateFileAssocFromString("*.lisp"),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			";; ",
			false
		));
		pset.ValidateCommentBlock(pset.CommentBlocks.Count - 1);

		string[] input =
		{
			"(defun foo ()",
			";; " + LongComment,
			"  1)"
		};
		string[] output = Reflow(pset, ".lisp", null, input, out bool blockFound);
		Check(blockFound, "The custom block was not found");
		Check(output.Length > input.Length, "The custom block was not wrapped");
		Check(output.First() == input.First() && output.Last() == input.Last(), "The code around the custom block was changed");
		string[] comment = output.Skip(1).Take(output.Length - 2).ToArray();
		Check(comment.All(l => l.StartsWith(";; ") && (l.Length <= 80)), "A line of the custom block is wrong");
		Check(Words(comment, ";;").SequenceEqual(Words(new[] { input[1] }, ";;")), "Reflowing the custom block changed the words");

		Console.WriteLine("Custom block tests PASSED.");
	}

	/// <summary>
	/// Runs the tests, which throw an exception if one fails.
	/// </summary>
	public static void Run()
	{
		TestFileAssociations();
		TestValidation();
		TestCopyConstructors();
		TestBulletMatching();
		TestBreakFlowMatching();
		TestWrapWidth();
		TestMinimumBlockWidth();
		TestIndentation();
		TestLongWord();
		TestNoBlock();
		TestBlocksAtEdges();
		TestSelection();
		TestCustomBlock();
	}
}
