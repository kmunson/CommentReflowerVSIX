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
static partial class UnitTests
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
		foreach (PropertyInfo p in t.GetProperties().Where(p => p.CanWrite))
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
		foreach (PropertyInfo p in o.GetType().GetProperties().Where(p => p.CanWrite))
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

		// An association has to match the whole file name: *.vb is not *.vbproj, *.c is
		// not *.css, *.h is not *.html, and a directory is not part of the name.
		foreach (string fileName in new[] { "x.vbproj", "x.css", "x.html", "x.txt.bak", "Jamfile.bak", "MyJamfile", @"c:\Jamfile\x.unknown", @"c:\dir.cpp\readme", "cpp" })
		{
			Check(BlockNamesFor(pset, fileName).Count == 0, "GetBlocksForFileName(" + fileName + ") found a block");
		}
		Check(BlockNamesFor(pset, "x.CPP").Contains("C++ style"), "GetBlocksForFileName(x.CPP) has no C++ style block");
		Check(BlockNamesFor(pset, "c:/dir/x.cpp").Contains("C++ style"), "GetBlocksForFileName(c:/dir/x.cpp) has no C++ style block");

		// regex characters in an association are literal, and * can be anywhere in it
		ParameterSet custom = new();
		custom.CommentBlocks.Clear();
		custom.CommentBlocks.Add(new CommentBlock(
			"Custom",
			CommentBlock.CreateFileAssocFromString("a+b.c;test*.log"),
			StartEndBlockType.Empty,
			"",
			false,
			StartEndBlockType.Empty,
			"",
			false,
			"// ",
			false
		));
		Check(BlockNamesFor(custom, "a+b.c").Count == 1, "GetBlocksForFileName(a+b.c) found no block");
		Check(BlockNamesFor(custom, "aab.c").Count == 0, "GetBlocksForFileName(aab.c) found a block");
		Check(BlockNamesFor(custom, "axb.c").Count == 0, "GetBlocksForFileName(axb.c) found a block");
		Check(BlockNamesFor(custom, "test2.log").Count == 1, "GetBlocksForFileName(test2.log) found no block");
		Check(BlockNamesFor(custom, "test.log").Count == 1, "GetBlocksForFileName(test.log) found no block");
		Check(BlockNamesFor(custom, "mytest.log").Count == 0, "GetBlocksForFileName(mytest.log) found a block");

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

	static string ExpandTabs(string line, int tabSize = TabSize)
	{
		string ret = "";
		foreach (char c in line)
		{
			ret += (c == '\t') ? new string(' ', tabSize - (ret.Length % tabSize)) : c.ToString();
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

	/// <summary>
	/// Reflows every block in the given lines, or those in the selection if it is given
	/// as the first and last line numbers, with the editor's tab size and the file's line
	/// ending as given. Checks that the output has only that line ending.
	/// </summary>
	static string[] Reflow(
		ParameterSet pset,
		string extension,
		string[] selection,
		string[] lines,
		out bool blockFound,
		int tabSize = TabSize,
		string lineEnding = "\r\n"
	)
	{
		string dir = Path.Combine(Path.GetTempPath(), "CommentReflowerAITests");
		Directory.CreateDirectory(dir);
		string fileName = Path.Combine(dir, "UnitTest" + extension);
		File.WriteAllText(fileName, string.Join(lineEnding, lines) + lineEnding);
		TestFileWrapper file = new(fileName)
		{
			TabSize = tabSize
		};
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
		string output = File.ReadAllText(outName);
		Check(
			output.Replace(lineEnding, "").IndexOfAny(new[] { '\r', '\n' }) == -1,
			"The output has a line ending other than " + lineEnding.Replace("\r", "\\r").Replace("\n", "\\n")
		);
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

		// Every line, the first included, gets the block's indentation in tabs or in
		// spaces, depending on the setting.
		string[] spaces = Reflow(new ParameterSet { UseTabsToIndent = false }, ".cpp", input);
		Check(spaces.Length > 1, "The comment was not wrapped");
		Check(spaces.All(l => l.StartsWith("        // ")), "A line does not start with 8 spaces");
		Check(spaces.All(l => !l.Contains('\t')), "A line has a tab when tabs are not used to indent");

		string[] tabs = Reflow(new ParameterSet { UseTabsToIndent = true }, ".cpp", input);
		Check(tabs.All(l => l.StartsWith("\t\t// ")), "A line does not start with 2 tabs");

		// the same text apart from the indentation of the lines
		Check(
			spaces.Select(l => l.TrimStart('\t', ' ')).SequenceEqual(tabs.Select(l => l.TrimStart('\t', ' '))),
			"Tabs and spaces to indent give different text"
		);

		Console.WriteLine("Indentation tests PASSED.");
	}

	static void TestReplaceLeadingIndentation()
	{
		ParameterSet tabs = new() { UseTabsToIndent = true };

		// the spaces before a comment, which is wrapped, become tabs on every line
		foreach (string indentation in new[] { "        ", "\t    ", "    \t", "\t\t" })
		{
			string[] output = Reflow(tabs, ".cpp", indentation + "// " + LongComment);
			Check(output.Length > 1, "The comment was not wrapped");
			Check(output.All(l => l.StartsWith("\t\t// ")), "An indentation of '" + indentation.Replace("\t", "\\t") + "' did not become 2 tabs on every line");
		}

		// a width that is not a multiple of the tab size leaves spaces after the tabs
		string[] sixColumns = Reflow(tabs, ".cpp", "      // " + LongComment);
		Check(sixColumns.All(l => l.StartsWith("\t  // ")), "An indentation of 6 spaces did not become a tab and 2 spaces");

		// spaces in a block that is already short and so needs no wrapping
		string[] shortComment = { "        // short" };
		Check(Reflow(tabs, ".cpp", shortComment).SequenceEqual(new[] { "\t\t// short" }), "A short comment indented with spaces was not indented with tabs");

		// lines of a block that are not wrapped
		string[] lines = Reflow(tabs, ".cpp", "        // one", "        // two", "        // three");
		Check(lines.All(l => l.StartsWith("\t\t// ")), "Not every line of a block was indented with tabs");

		// the lines of a block with its own start and end lines
		string[] doxygen = Reflow(tabs, ".cpp", "    /**", "     * " + LongComment, "     */");
		Check(doxygen.First() == "\t/**", "The block start line was not indented with a tab");
		Check(doxygen.Last() == "\t */", "The block end line was not indented with a tab");
		Check(doxygen.Skip(1).Take(doxygen.Length - 2).All(l => l.StartsWith("\t * ")), "A block line was not indented with a tab");

		// the text after the comment start is not changed
		string[] indented = Reflow(tabs, ".cpp", "    //     four spaces of text");
		Check(indented.SequenceEqual(new[] { "\t//     four spaces of text" }), "The spaces after the comment start were changed");

		// code before a comment is not changed, and the lines that follow are indented with tabs
		string[] afterCode = Reflow(tabs, ".cpp", "int i; // " + LongComment);
		Check(afterCode.First().StartsWith("int i; // "), "The code before a comment was changed");
		Check(afterCode.Length > 1, "A comment after code was not wrapped");
		Check(afterCode.Skip(1).All(l => l.StartsWith("\t   // ")), "A line after the first of a comment after code is not indented with a tab and 3 spaces");

		// with tabs not in use, the tabs before a comment become spaces
		ParameterSet spaces = new() { UseTabsToIndent = false };
		foreach (string indentation in new[] { "        ", "\t    ", "    \t", "\t\t" })
		{
			string[] output = Reflow(spaces, ".cpp", indentation + "// " + LongComment);
			Check(output.Length > 1, "The comment was not wrapped");
			Check(output.All(l => l.StartsWith("        // ")), "An indentation of '" + indentation.Replace("\t", "\\t") + "' did not become 8 spaces on every line");
		}
		Check(Reflow(spaces, ".cpp", "\t  // short").SequenceEqual(new[] { "      // short" }), "A tab and 2 spaces did not become 6 spaces");
		Check(Reflow(spaces, ".cpp", "\t\t// short").SequenceEqual(new[] { "        // short" }), "A short comment indented with tabs was not indented with spaces");
		Check(Reflow(spaces, ".cpp", "        // short").SequenceEqual(new[] { "        // short" }), "A short comment indented with spaces was changed");

		string[] spaceDoxygen = Reflow(spaces, ".cpp", "\t/**", "\t * " + LongComment, "\t */");
		Check(spaceDoxygen.First() == "    /**", "The block start line was not indented with spaces");
		Check(spaceDoxygen.Last() == "     */", "The block end line was not indented with spaces");
		Check(spaceDoxygen.Skip(1).Take(spaceDoxygen.Length - 2).All(l => l.StartsWith("     * ")), "A block line was not indented with spaces");

		string[] spaceAfterCode = Reflow(spaces, ".cpp", "int i; // " + LongComment);
		Check(spaceAfterCode.First().StartsWith("int i; // "), "The code before a comment was changed");
		Check(spaceAfterCode.Skip(1).All(l => l.StartsWith("       // ") && !l.Contains('\t')), "A line after the first of a comment after code is not indented with 7 spaces");

		Console.WriteLine("Replacing leading indentation tests PASSED.");
	}

	static ParameterSet XmlParameterSet(int wrapWidth, int minimumBlockWidth, bool useTabs, string lineStart = "")
	{
		ParameterSet pset = new()
		{
			UseTabsToIndent = useTabs,
			WrapWidth = wrapWidth,
			MinimumBlockWidth = minimumBlockWidth
		};
		pset.CommentBlocks.Clear();
		pset.CommentBlocks.Add(new CommentBlock(
			"XML",
			CommentBlock.CreateFileAssocFromString("*.xml"),
			StartEndBlockType.NeverOnOwnLine,
			"<!-- ",
			false,
			StartEndBlockType.NeverOnOwnLine,
			" -->",
			false,
			lineStart,
			true
		));
		return pset;
	}

	static string[] Tokens(IEnumerable<string> lines)
	{
		return string.Join(" ", lines).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
	}

	/// <summary>
	/// A block whose start is longer than its line start, so that the text on its first
	/// line can either line up with the text on the lines after it, or have them start at
	/// the line start, as they do in an XML file.
	/// </summary>
	static void TestStartLongerThanLineStart()
	{
		string[] input =
		{
			"    <!-- This is a comment with enough short words in it that it has to be wrapped",
			"    when the wrap width is narrow and that it fills several lines",
			"    even at the default width so it is easy to see. -->"
		};
		foreach (bool useTabs in new[] { false, true })
		{
			ParameterSet pset = XmlParameterSet(60, 20, useTabs);
			string[] output = Reflow(pset, ".xml", input);
			string what = " with tabs " + (useTabs ? "on" : "off");
			string indentation = useTabs ? "\t" : "    ";

			Check(output.Length > 1, "The XML comment was not wrapped" + what);
			Check(output[0].StartsWith(indentation + "<!-- "), "The first line of the XML comment is wrong" + what);
			Check(
				output.Skip(1).All(l => l.StartsWith(indentation) && !char.IsWhiteSpace(l[indentation.Length])),
				"A line after the first of the XML comment is not flush with the block" + what
			);
			Check(output.Last().EndsWith(" -->"), "The last line of the XML comment does not end the comment" + what);
			Check(output.All(l => ExpandTabs(l).Length <= 60), "A line of the XML comment is wider than the wrap width" + what);
			Check(Tokens(output).SequenceEqual(Tokens(input)), "Reflowing the XML comment changed the words" + what);
			// the first line is filled with words from the lines after it
			Check(ExpandTabs(output[0]).Length > 50, "The first line of the XML comment was not filled" + what);
			Check(Reflow(pset, ".xml", output).SequenceEqual(output), "Reflowing the XML comment twice changed the result" + what);
		}

		// a short first line is joined with the line after it, flush with the line start
		string[] flush = Reflow(XmlParameterSet(80, 30, false), ".xml", "<!-- Short first line", "and the line after it. -->");
		Check(flush.SequenceEqual(new[] { "<!-- Short first line and the line after it. -->" }), "A short first line was not joined with the line after it");

		// and so is one with the line after it lined up with the text on it
		string[] aligned = Reflow(XmlParameterSet(80, 30, false), ".xml", "<!-- Aligned first line", "     and the line after it. -->");
		Check(aligned.SequenceEqual(new[] { "<!-- Aligned first line and the line after it. -->" }), "A first line was not joined with the line lined up with it");

		// a comment on a single line that is wrapped has nothing after it to show how to lay
		// out the lines it adds, so they start at the line start when it is blank
		foreach (bool useTabs in new[] { false, true })
		{
			string[] single = { "  <!-- This is the file that defines the IDs for all the commands exposed by VisualStudio. -->" };
			string[] wrapped = Reflow(XmlParameterSet(80, 30, useTabs), ".xml", single);
			string what = " with tabs " + (useTabs ? "on" : "off");
			Check(wrapped.Length > 1, "A long single line XML comment was not wrapped" + what);
			Check(
				wrapped.Skip(1).All(l => l.StartsWith("  ") && !char.IsWhiteSpace(l[2])),
				"A line added to a single line XML comment is not flush with the block" + what + ": " + string.Join(" | ", wrapped)
			);
			Check(Tokens(wrapped).SequenceEqual(Tokens(single)), "Reflowing a single line XML comment changed the words" + what);
			Check(Reflow(XmlParameterSet(80, 30, useTabs), ".xml", wrapped).SequenceEqual(wrapped), "Reflowing a single line XML comment twice changed the result" + what);
		}

		// but not if the line start is not blank: the lines added are lined up with the text
		string[] startBlock = Reflow(new ParameterSet(), ".cpp", "/*! This is a comment on one line that is long enough that it has to be wrapped onto a second line */");
		Check(startBlock.Length > 1 && startBlock.Skip(1).Take(startBlock.Length - 2).All(l => l.StartsWith(" *  ")), "A line added to a single line /*! comment is not lined up with its text: " + string.Join(" | ", startBlock));

		// the same with the default /*! block, whose start is 1 longer than its line start
		foreach (string second in new[] { " *  aligned second line", " * flush second line" })
		{
			string[] block = Reflow(new ParameterSet(), ".cpp", "/*! first line", second, " */");
			Check(block.Length == 1, "A /*! block was not joined: " + string.Join(" | ", block));
			Check(block[0].StartsWith("/*! first line ") && block[0].EndsWith(" second line */"), "A /*! block was joined wrongly: " + block[0]);
		}

		Console.WriteLine("Start longer than the line start tests PASSED.");
	}

	/// <summary>
	/// Writes the lines to a file with the given extension, and returns it as the test
	/// document with the file name.
	/// </summary>
	static TestFileWrapper OpenDocument(string extension, string[] lines, out string fileName)
	{
		string dir = Path.Combine(Path.GetTempPath(), "CommentReflowerAITests");
		Directory.CreateDirectory(dir);
		fileName = Path.Combine(dir, "UnitTestPoint" + extension);
		File.WriteAllLines(fileName, lines);
		return new TestFileWrapper(fileName);
	}

	/// <summary>
	/// A block with a blank line start, as in XML, has nothing on its lines after the first
	/// to show that they are in a comment, so finding the block from the point needs the
	/// start to be found by the indentation of the point's line.
	/// </summary>
	static void TestFindBlockFromAnyLine()
	{
		string[] lines =
		{
			"      <Parent />",
			"      <!-- First comment line with some words",
			"      second line flush with the start",
			"          third line indented more",
			"      last line -->",
			"      <Button id=\"x\" />",
			"    <Other />",
			"    <!-- Another comment, on one line -->",
			"        <Deep />",
			"    <!-- And one",
			"    more -->"
		};
		// the first and last line of the comment, or of the comments, for each line, or 0 for none
		int[][] expected =
		{
			new[] { 0, 0 }, new[] { 2, 5 }, new[] { 2, 5 }, new[] { 2, 5 }, new[] { 2, 5 },
			new[] { 0, 0 }, new[] { 0, 0 }, new[] { 8, 8 }, new[] { 0, 0 }, new[] { 10, 11 }, new[] { 10, 11 }
		};
		TestFileWrapper document = OpenDocument(".xml", lines, out string fileName);
		ParameterSet pset = XmlParameterSet(80, 30, false);
		for (int line = 1; line <= lines.Length; line++)
		{
			EnvDTE.EditPoint pt = document.CreateEditPoint(null);
			pt.MoveToLineAndOffset(line, 1);
			bool found = CommentReflowerObj.GetBlockContainingPoint(pset, fileName, pt, out CommentBlock block, out MatchedBlockData bdata);
			int[] range = expected[line - 1];
			string what = "line " + line + " (" + lines[line - 1].Trim() + ")";
			if (range[0] == 0)
			{
				Check(!found, "A block was found at " + what);
			}
			else
			{
				Check(found, "No block was found at " + what);
				Check((bdata.StartLine == range[0]) && (bdata.EndLine == range[1]), "The block at " + what + " is lines " + bdata.StartLine + " to " + bdata.EndLine);
				Check(block.Name == "XML", "The block at " + what + " is not the XML block");
				Check(bdata.Indentation == lines[range[0] - 1].Length - lines[range[0] - 1].TrimStart().Length, "The block at " + what + " has the wrong indentation");
			}
		}

		// reflowing from the last line of a comment reflows the whole of it
		string[] comment =
		{
			"    <!-- one two three four five six seven eight nine ten eleven twelve",
			"    thirteen fourteen fifteen sixteen -->"
		};
		document = OpenDocument(".xml", comment, out fileName);
		EnvDTE.EditPoint last = document.CreateEditPoint(null);
		last.MoveToLineAndOffset(2, 1);
		Check(CommentReflowerObj.WrapBlockContainingPoint(XmlParameterSet(40, 10, false), fileName, last), "Reflowing from the last line found no comment");
		string outName = fileName + ".out";
		document.WriteToFile(outName);
		string[] output = File.ReadAllLines(outName);
		Check(output.Length > 2, "Reflowing from the last line did not wrap the comment");
		// the end of the comment is added after wrapping, so the last line can go past the wrap width
		Check(output.Take(output.Length - 1).All(l => l.Length <= 40), "A line is wider than the wrap width after reflowing from the last line");
		Check(output[0].StartsWith("    <!-- one "), "The first line is wrong after reflowing from the last line");
		Check(Tokens(output).SequenceEqual(Tokens(comment)), "Reflowing from the last line changed the words");

		Console.WriteLine("Finding a block from any line tests PASSED.");
	}

	/// <summary>
	/// A line added by wrapping has the same whitespace after the line start as the line
	/// it was split from, so that the lines of a paragraph can't differ in whether they
	/// use tabs or spaces to line up.
	/// </summary>
	static void TestAddedLinesMatchExistingWhitespace()
	{
		// a comment whose lines after the first are lined up with the text of the first
		string[] lined =
		{
			"    <!-- UNIT_TEST compiles out the ThreadHelper.ThrowIfNotOnUIThread() calls in",
			"         CommentReflowerAISharedLib, which still uses the EnvDTE interfaces that the mock",
			"         editor objects implement, so the threading analyzer would flag every use of them. -->"
		};
		foreach (bool useTabs in new[] { false, true })
		{
			ParameterSet pset = XmlParameterSet(80, 80, useTabs);
			string[] output = Reflow(pset, ".xml", lined);
			string what = " with tabs " + (useTabs ? "on" : "off");
			string expected = useTabs ? "\t     " : "         ";

			Check(output.Length > 3, "The lined up comment was not wrapped" + what);
			Check(
				output.Skip(1).All(l => l.StartsWith(expected) && !char.IsWhiteSpace(l[expected.Length])),
				"A line after the first of the lined up comment does not start with the same whitespace" + what + ": " + string.Join(" | ", output.Select(l => l.Replace("\t", "\\t")))
			);
			Check(Tokens(output).SequenceEqual(Tokens(lined)), "Reflowing the lined up comment changed the words" + what);
			Check(Reflow(pset, ".xml", output).SequenceEqual(output), "Reflowing the lined up comment twice changed the result" + what);
		}

		// the whitespace is copied, not worked out again, so spaces stay spaces and tabs stay tabs
		foreach (string indentation in new[] { "      ", "\t  ", "  \t" })
		{
			string[] text =
			{
				"This paragraph is not indented.",
				indentation + "This one is, by the same amount on every line, and it has enough words in it that it has to be wrapped onto another line.",
				indentation + "Its second line is short."
			};
			string[] output = Reflow(new ParameterSet { UseTabsToIndent = true }, ".txt", text);
			string[] indented = output.Skip(1).ToArray();
			Check(indented.Length > 1, "The indented paragraph was not wrapped");
			Check(
				indented.All(l => l.StartsWith(indentation) && !char.IsWhiteSpace(l[indentation.Length])),
				"A line of the paragraph indented with '" + indentation.Replace("\t", "\\t") + "' is not indented the same: " + string.Join(" | ", output.Select(l => l.Replace("\t", "\\t")))
			);
		}

		Console.WriteLine("Added lines match existing whitespace tests PASSED.");
	}

	/// <summary>
	/// A block can have a line start of spaces, to line up the text of the lines with the
	/// text after the block start. The lines of a comment that leave it out, as they do in
	/// an XML file written with the lines flush with the block start, are still in the
	/// comment and are given the line start when it is reflowed.
	/// </summary>
	static void TestOptionalLineStart()
	{
		// the comment from the test project, with the settings it was reflowed with
		string[] flush =
		{
			"\t\t<!-- C# 10 for file-scoped namespaces, which need no runtime support. The",
			"\t\tdefault for net48 is 7.3. -->"
		};
		string[] lined =
		{
			"\t\t<!-- C# 10 for file-scoped namespaces, which need no runtime support. The",
			"\t\t     default for net48 is 7.3. -->"
		};
		string[] expected =
		{
			"\t\t<!-- C# 10 for file-scoped namespaces, which need no runtime support. The default for",
			"\t\t     net48 is 7.3. -->"
		};
		ParameterSet pset = XmlParameterSet(80, 80, true, "     ");
		Check(Reflow(pset, ".xml", flush).SequenceEqual(expected), "A comment with its second line flush was not reflowed to lined up: " + string.Join(" | ", Reflow(pset, ".xml", flush)));
		Check(Reflow(pset, ".xml", lined).SequenceEqual(expected), "A comment with its second line lined up was not reflowed to lined up");
		Check(Reflow(pset, ".xml", expected).SequenceEqual(expected), "Reflowing a lined up comment twice changed the result");

		// the same with spaces to indent, a comment that is wrapped, and a flush line in the middle
		string[] input =
		{
			"      <!-- This is a comment with enough short words in it that it has to be wrapped",
			"      when the wrap width is narrow, and the lines in the middle",
			"           are lined up with the text of the first, or not",
			"      at all, as they are in files written by hand. -->"
		};
		pset = XmlParameterSet(50, 20, false, "     ");
		string[] output = Reflow(pset, ".xml", input);
		Check(output.Length > 4, "The comment with lines flush and lined up was not wrapped");
		Check(output[0].StartsWith("      <!-- "), "The first line is wrong");
		Check(
			output.Skip(1).All(l => l.StartsWith("           ") && !char.IsWhiteSpace(l[11])),
			"A line after the first is not lined up with the text of the first: " + string.Join(" | ", output)
		);
		Check(output.Take(output.Length - 1).All(l => l.Length <= 50), "A line is wider than the wrap width");
		Check(Tokens(output).SequenceEqual(Tokens(input)), "Reflowing changed the words");
		Check(Reflow(pset, ".xml", output).SequenceEqual(output), "Reflowing twice changed the result");

		// every line of the comment is found from the cursor, whether or not it has the line start
		string[] lines =
		{
			"      <Before />",
			"      <!-- Start of a comment",
			"      flush middle line",
			"           lined up line",
			"      end line -->",
			"      <After />"
		};
		TestFileWrapper document = OpenDocument(".xml", lines, out string fileName);
		for (int line = 1; line <= lines.Length; line++)
		{
			EnvDTE.EditPoint pt = document.CreateEditPoint(null);
			pt.MoveToLineAndOffset(line, 1);
			bool found = CommentReflowerObj.GetBlockContainingPoint(pset, fileName, pt, out _, out MatchedBlockData bdata);
			bool inside = (line >= 2) && (line <= 5);
			Check(found == inside, "A block was " + (found ? "" : "not ") + "found at line " + line);
			Check(!found || ((bdata.StartLine == 2) && (bdata.EndLine == 5)), "The block at line " + line + " is lines " + bdata.StartLine + " to " + bdata.EndLine);
		}

		// a line start that is not blank is still needed on every line, so a comment whose
		// last line leaves it out is not a block, and is left alone
		string[] noLineStart = { "/* first line of a comment", "no line start on this line */" };
		Check(
			Reflow(new ParameterSet(), ".cpp", null, noLineStart, out bool noLineStartFound).SequenceEqual(noLineStart) && !noLineStartFound,
			"A C comment whose second line has no line start was reflowed"
		);
		Console.WriteLine("Optional line start tests PASSED.");
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

	////////////////////////////////////////
	// line endings, tab sizes and whole files
	////////////////////////////////////////

	const string InputDirectory = @"..\..\Input\";

	static void TestLineEndings()
	{
		string[][] inputs =
		{
			new[] { "\t\t// " + LongComment },
			new[] { "    /**", "     * " + LongComment, "     */" },
			new[] { "/*! first line", " * flush second line", " */" },
			File.ReadAllLines(InputDirectory + "Regression.cpp")
		};
		foreach (string[] input in inputs)
		{
			ParameterSet pset = new() { UseTabsToIndent = true };
			string[] crlf = Reflow(pset, ".cpp", null, input, out _, lineEnding: "\r\n");
			string[] lf = Reflow(pset, ".cpp", null, input, out _, lineEnding: "\n");
			Check(lf.SequenceEqual(crlf), "Reflowing with LF line endings gave different lines from CRLF, for input starting " + input[0]);
		}
		string[] xml = File.ReadAllLines(InputDirectory + "RegressionVsct.xml");
		Check(
			Reflow(XmlParameterSet(80, 30, true), ".xml", null, xml, out _, lineEnding: "\n")
				.SequenceEqual(Reflow(XmlParameterSet(80, 30, true), ".xml", null, xml, out _, lineEnding: "\r\n")),
			"Reflowing an XML file with LF line endings gave different lines from CRLF"
		);

		Console.WriteLine("Line ending tests PASSED.");
	}

	/// <summary>
	/// Returns the display column, from 1, of the first character of the line that is not
	/// a space or a tab.
	/// </summary>
	static int TextColumn(string line, int tabSize)
	{
		string expanded = ExpandTabs(line, tabSize);
		return expanded.Length - expanded.TrimStart(' ').Length + 1;
	}

	static void TestTabSizes()
	{
		string[] lined =
		{
			"    <!-- UNIT_TEST compiles out the ThreadHelper.ThrowIfNotOnUIThread() calls in",
			"         CommentReflowerAISharedLib, which still uses the EnvDTE interfaces that the mock",
			"         editor objects implement, so the threading analyzer would flag every use of them. -->"
		};
		foreach (int tabSize in new[] { 2, 4, 8 })
		{
			string what = " with a tab size of " + tabSize;

			// a comment lined up with the text after its start
			string[] output = Reflow(XmlParameterSet(80, 80, true), ".xml", null, lined, out _, tabSize);
			Check(output.Length > 3, "The lined up comment was not wrapped" + what);
			Check(TextColumn(output[0], tabSize) == 5, "The lined up comment's first line moved" + what);
			Check(output.Skip(1).All(l => TextColumn(l, tabSize) == 10), "A line of the lined up comment is not lined up" + what);
			Check(Reflow(XmlParameterSet(80, 80, true), ".xml", null, output, out _, tabSize).SequenceEqual(output), "Reflowing the lined up comment twice changed it" + what);

			// the indentation of a block becomes as many tabs as fit, then spaces
			string[] doxygen = Reflow(new ParameterSet { UseTabsToIndent = true }, ".cpp", null, new[] { "        /**", "         * " + LongComment, "         */" }, out _, tabSize);
			string indentation = new string('\t', 8 / tabSize);
			Check(doxygen.All(l => l.StartsWith(indentation) && (l[indentation.Length] != '\t') && (TextColumn(l, tabSize) == ((l == doxygen[0]) ? 9 : 10))), "The block indented 8 columns is not indented with tabs" + what + ": " + string.Join(" | ", doxygen.Select(l => l.Replace("\t", "\\t"))));

			// a block indented with tabs wraps at the wrap width measured with the tab size
			string[] tabbed = Reflow(new ParameterSet { UseTabsToIndent = true }, ".cpp", null, new[] { "\t\t// " + LongComment }, out _, tabSize);
			Check(tabbed.Length > 1, "The comment indented with tabs was not wrapped" + what);
			Check(tabbed.All(l => l.StartsWith("\t\t// ") && (ExpandTabs(l, tabSize).Length <= 80)), "A line of the comment indented with tabs is wrong" + what);
			Check(Words(tabbed, "//").SequenceEqual(Words(new[] { "\t\t// " + LongComment }, "//")), "Reflowing the comment indented with tabs changed the words" + what);
		}

		// A comment after code whose last line is lined up with tabs of 4 columns is not
		// lined up with tabs of 8, so it is a block only with a tab size of 4.
		string[] afterCode =
		{
			"int i; /* This is a mult line C syle comment after code. This is a mult line C syle comment after code.",
			"\t    * This is a mult line C syle comment after code. This is a mult line C syle comment after code. */"
		};
		foreach (int tabSize in new[] { 4, 8 })
		{
			string[] output = Reflow(new ParameterSet(), ".cpp", null, afterCode, out bool found, tabSize);
			Check(found == (tabSize == 4), "The comment after code lined up with tabs of 4 was " + (found ? "" : "not ") + "a block with a tab size of " + tabSize);
			Check((tabSize == 4) || output.SequenceEqual(afterCode), "The comment after code lined up with tabs of 4 was changed with a tab size of 8");
		}

		Console.WriteLine("Tab size tests PASSED.");
	}

	/// <summary>
	/// Returns the words of the lines without the line starts of the blocks, which
	/// wrapping adds a copy of to each line it adds.
	/// </summary>
	static string[] CommentWords(IEnumerable<string> lines, ParameterSet pset)
	{
		HashSet<string> lineStarts = new(pset.CommentBlocks.Select(b => b.LineStart.Trim()).Where(s => s.Length > 0));
		return Tokens(lines).Where(t => !lineStarts.Contains(t)).ToArray();
	}

	/// <summary>
	/// Finds the comment from every line of real XML files. A comment has to be found on
	/// exactly the lines from a line with &lt;!-- to the next line with --&gt;.
	/// </summary>
	static void TestFindBlockInFiles()
	{
		foreach (string file in new[] { "RegressionVsct.xml", "RegressionCsproj.xml" })
		{
			string[] lines = File.ReadAllLines(InputDirectory + file);
			foreach (string lineStart in new[] { "", "     " })
			{
				ParameterSet pset = XmlParameterSet(80, 30, true, lineStart);
				TestFileWrapper document = OpenDocument(".xml", lines, out string fileName);
				bool inComment = false;
				int commentStart = 0;
				int found = 0;
				for (int line = 1; line <= lines.Length; line++)
				{
					string text = lines[line - 1];
					if (text.Contains("<!--"))
					{
						inComment = true;
						commentStart = line;
					}
					bool expected = inComment;
					if (text.Contains("-->"))
					{
						inComment = false;
					}

					EnvDTE.EditPoint pt = document.CreateEditPoint(null);
					pt.MoveToLineAndOffset(line, 1);
					bool isFound = CommentReflowerObj.GetBlockContainingPoint(pset, fileName, pt, out _, out MatchedBlockData bdata);
					string what = file + " line " + line + " with a line start of " + lineStart.Length + " spaces";
					Check(isFound == expected, "A comment was " + (isFound ? "" : "not ") + "found at " + what);
					Check(!isFound || (bdata.StartLine == commentStart), "The comment at " + what + " starts at line " + bdata.StartLine + " not " + commentStart);
					found += isFound ? 1 : 0;
				}
				Check(found > 0, "No comment was found in " + file);
			}
		}

		Console.WriteLine("Finding blocks in files tests PASSED.");
	}

	/// <summary>
	/// Reflows every input file with each combination of settings, tab size and line
	/// start for XML, and checks that nothing fails, that no words are lost or moved, and
	/// that reflowing the result again changes nothing.
	/// </summary>
	static void TestSettingsSweep()
	{
		string[] files =
		{
			"Regression.cpp", "RegressionBlankLines.cpp", "RegressionBreakFlowStrings.cpp", "RegressionBullets.cpp",
			"ExampleFile.cpp", "Regression.vb", "Regression.JAM", "Regression.txt", "RegressionVsct.xml", "RegressionCsproj.xml"
		};
		int[][] widths = { new[] { 80, 30 }, new[] { 40, 10 }, new[] { 100, 60 } };
		int runs = 0;
		List<string> failures = new();
		foreach (string file in files)
		{
			string[] input = File.ReadAllLines(InputDirectory + file);
			string extension = Path.GetExtension(file);
			bool isXml = extension == ".xml";
			foreach (string lineStart in isXml ? new[] { "", "     " } : new[] { "" })
			{
				foreach (bool useTabs in new[] { false, true })
				{
					foreach (int[] width in widths)
					{
						foreach (int tabSize in new[] { 4, 8 })
						{
							ParameterSet pset = isXml ?
								XmlParameterSet(width[0], width[1], useTabs, lineStart) :
								new ParameterSet { UseTabsToIndent = useTabs, WrapWidth = width[0], MinimumBlockWidth = width[1] };
							string what = file + " with wrap width " + width[0] + ", minimum block width " + width[1] +
								", tabs " + (useTabs ? "on" : "off") + ", tab size " + tabSize +
								(isXml ? ", line start of " + lineStart.Length + " spaces" : "");
							runs++;
							string[] once;
							string[] twice;
							try
							{
								once = Reflow(pset, extension, null, input, out _, tabSize);
								twice = Reflow(pset, extension, null, once, out _, tabSize);
							}
							catch (ArgumentException e)
							{
								failures.Add("Reflowing " + what + " threw: " + e.Message);
								continue;
							}
							if (!CommentWords(once, pset).SequenceEqual(CommentWords(input, pset)))
							{
								failures.Add("Reflowing " + what + " changed the words");
							}
							else if (!twice.SequenceEqual(once))
							{
								failures.Add("Reflowing " + what + " a second time changed it");
							}
						}
					}
				}
			}
		}

		foreach (string failure in failures)
		{
			Console.WriteLine(failure);
		}
		Check(failures.Count == 0, failures.Count + " of " + runs + " combinations in the settings sweep failed");

		Console.WriteLine("Settings sweep tests PASSED (" + runs + " combinations).");
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
		TestReplaceLeadingIndentation();
		TestStartLongerThanLineStart();
		TestFindBlockFromAnyLine();
		TestAddedLinesMatchExistingWhitespace();
		TestOptionalLineStart();
		TestLongWord();
		TestNoBlock();
		TestBlocksAtEdges();
		TestSelection();
		TestCustomBlock();
		TestLineEndings();
		TestTabSizes();
		TestFindBlockInFiles();
		TestSettingsSweep();
		TestRegistrationDefaults();
		TestRegistrationEnums();
		TestParameterSetLoader();
		TestCommentBlockLines();
		TestBlockShapes();
		TestBlockMarkers();
	}
}
