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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

/// <summary>
/// Tests of loading the settings: that the defaults in registration.json, which are what
/// Tools > Options shows, are the defaults of ParameterSet, that the values in it are
/// the ones ParameterSetLoader knows, and that ParameterSetLoader turns the values into
/// a ParameterSet as it should.
/// </summary>
static partial class UnitTests
{
	const string RegistrationFile = @"..\..\..\CommentReflowerAI\registration.json";
	const string CommentBlocksSetting = "commentReflowerAI.blocks.commentBlocks";
	const string BulletPointsSetting = "commentReflowerAI.bullets.bulletPoints";
	const string BreakFlowStringsSetting = "commentReflowerAI.breakFlowStrings.breakFlowStrings";

	static JObject ReadRegistrationProperties()
	{
		return (JObject)JObject.Parse(File.ReadAllText(RegistrationFile))["properties"];
	}

	/// <summary>
	/// Returns an item of an array setting as the dictionary Unified Settings gives.
	/// </summary>
	static Dictionary<string, object> ToSettingsItem(JObject item)
	{
		return item.Properties().ToDictionary(p => p.Name, p => ((JValue)p.Value).Value);
	}

	static List<IReadOnlyDictionary<string, object>> DefaultItems(JObject properties, string setting)
	{
		return ((JArray)properties[setting]["default"])
			.Cast<JObject>()
			.Select(i => (IReadOnlyDictionary<string, object>)ToSettingsItem(i))
			.ToList();
	}

	/// <summary>
	/// Returns the item that Tools > Options adds to an array setting, from the defaults
	/// of the item's properties.
	/// </summary>
	static Dictionary<string, object> NewItem(JObject properties, string setting)
	{
		return ((JObject)properties[setting]["items"]["properties"])
			.Properties()
			.ToDictionary(p => p.Name, p => ((JValue)p.Value["default"]).Value);
	}

	static string[] EnumValues(JObject properties, string setting, string property)
	{
		return properties[setting]["items"]["properties"][property]["enum"].Select(v => (string)v).ToArray();
	}

	static int DefaultInt(JObject properties, string setting)
	{
		return (int)(long)((JValue)properties[setting]["default"]).Value;
	}

	static ParameterSet LoadRegistrationDefaults(JObject properties)
	{
		return ParameterSetLoader.Load(
			DefaultInt(properties, "commentReflowerAI.general.wrapWidth"),
			DefaultInt(properties, "commentReflowerAI.general.minimumBlockWidth"),
			(bool)((JValue)properties["commentReflowerAI.general.useTabsToIndent"]["default"]).Value,
			DefaultItems(properties, CommentBlocksSetting),
			DefaultItems(properties, BulletPointsSetting),
			DefaultItems(properties, BreakFlowStringsSetting)
		);
	}

	/// <summary>
	/// Loads settings with the defaults for everything but the given items.
	/// </summary>
	static ParameterSet LoadItems(
		IEnumerable<IReadOnlyDictionary<string, object>> commentBlocks = null,
		IEnumerable<IReadOnlyDictionary<string, object>> bulletPoints = null,
		IEnumerable<IReadOnlyDictionary<string, object>> breakFlowStrings = null,
		int wrapWidth = 80,
		int minimumBlockWidth = 30
	)
	{
		return ParameterSetLoader.Load(
			wrapWidth,
			minimumBlockWidth,
			false,
			commentBlocks ?? new List<IReadOnlyDictionary<string, object>>(),
			bulletPoints ?? new List<IReadOnlyDictionary<string, object>>(),
			breakFlowStrings ?? new List<IReadOnlyDictionary<string, object>>()
		);
	}

	static void CheckThrowsWithMessage(Action action, string expectedText, string message)
	{
		try
		{
			action();
		}
		catch (ArgumentException e)
		{
			Check(e.Message.Contains(expectedText), message + " threw \"" + e.Message + "\", which does not contain \"" + expectedText + "\"");
			return;
		}
		throw new ApplicationException(message + " did not throw an ArgumentException");
	}

	/// <summary>
	/// Checks that two lists of settings objects are the same, naming the first item and
	/// property that differ.
	/// </summary>
	static void CheckSameItems<T>(List<T> actual, List<T> expected, string what)
	{
		for (int i = 0; i < Math.Min(actual.Count, expected.Count); i++)
		{
			if (!DeepEqual(actual[i], expected[i]))
			{
				string different = string.Join(", ", typeof(T).GetProperties()
					.Where(p => p.CanWrite && !DeepEqual(p.GetValue(actual[i], null), p.GetValue(expected[i], null)))
					.Select(p => p.Name));
				string name = (string)typeof(T).GetProperty("Name").GetValue(expected[i], null);
				throw new ApplicationException(what + " " + (i + 1) + " (\"" + name + "\") in registration.json differs from ParameterSet in " + different);
			}
		}
		Check(actual.Count == expected.Count, "registration.json has " + actual.Count + " " + what + "s, and ParameterSet has " + expected.Count);
	}

	static void TestRegistrationDefaults()
	{
		JObject properties = ReadRegistrationProperties();
		ParameterSet fromRegistration = LoadRegistrationDefaults(properties);
		ParameterSet defaults = new();

		Check(fromRegistration.WrapWidth == defaults.WrapWidth, "The default wrap width in registration.json differs from ParameterSet");
		Check(fromRegistration.MinimumBlockWidth == defaults.MinimumBlockWidth, "The default minimum block width in registration.json differs from ParameterSet");
		Check(fromRegistration.UseTabsToIndent == defaults.UseTabsToIndent, "The default for using tabs to indent in registration.json differs from ParameterSet");
		CheckSameItems(fromRegistration.CommentBlocks, defaults.CommentBlocks, "Comment block");
		CheckSameItems(fromRegistration.BulletPoints, defaults.BulletPoints, "Bullet");
		CheckSameItems(fromRegistration.BreakFlowStrings, defaults.BreakFlowStrings, "Break flow string");
		Check(DeepEqual(fromRegistration, defaults), "The defaults in registration.json differ from ParameterSet");

		// what Tools > Options adds when a new item is added has to be valid as it is
		CheckNotThrows(() => LoadItems(commentBlocks: new[] { NewItem(properties, CommentBlocksSetting) }), "Loading a new comment block");
		CheckNotThrows(() => LoadItems(bulletPoints: new[] { NewItem(properties, BulletPointsSetting) }), "Loading a new bullet");
		CheckNotThrows(() => LoadItems(breakFlowStrings: new[] { NewItem(properties, BreakFlowStringsSetting) }), "Loading a new break flow string");

		Console.WriteLine("registration.json default tests PASSED.");
	}

	static void TestRegistrationEnums()
	{
		JObject properties = ReadRegistrationProperties();

		// every block type that can be chosen loads as the type with that name, and every
		// type can be chosen
		foreach (string property in new[] { "blockStartType", "blockEndType" })
		{
			string[] values = EnumValues(properties, CommentBlocksSetting, property);
			Check(
				new HashSet<string>(values, StringComparer.OrdinalIgnoreCase).SetEquals(Enum.GetNames(typeof(StartEndBlockType))),
				"The values of " + property + " in registration.json are not the StartEndBlockType names"
			);
			foreach (string value in values)
			{
				Dictionary<string, object> item = NewItem(properties, CommentBlocksSetting);
				item[property] = value;
				item["blockStart"] = "start";
				item["blockEnd"] = "end";
				CommentBlock block = LoadItems(commentBlocks: new[] { item }).CommentBlocks[0];
				StartEndBlockType loaded = (property == "blockStartType") ? block.BlockStartType : block.BlockEndType;
				Check(string.Equals(loaded.ToString(), value, StringComparison.OrdinalIgnoreCase), property + " \"" + value + "\" loaded as " + loaded);
			}
		}

		// the values ParameterSetLoader compares with are the ones registration.json has
		string[] firstLine = EnumValues(properties, CommentBlocksSetting, "firstLine");
		Check(
			firstLine.OrderBy(v => v).SequenceEqual(new[] { "canContainTextBeforeStart", "mustNotContainTextBeforeStart" }),
			"The values of firstLine in registration.json are not the ones ParameterSetLoader knows"
		);
		foreach (string value in firstLine)
		{
			Dictionary<string, object> item = NewItem(properties, CommentBlocksSetting);
			item["firstLine"] = value;
			bool only = LoadItems(commentBlocks: new[] { item }).CommentBlocks[0].OnlyEmptyLineBeforeStartOfBlock;
			Check(only == (value == "mustNotContainTextBeforeStart"), "firstLine \"" + value + "\" loaded as " + only);
		}

		string[] wrapOnEdge = EnumValues(properties, BulletPointsSetting, "wrapOnEdge");
		Check(wrapOnEdge.OrderBy(v => v).SequenceEqual(new[] { "left", "right" }), "The values of wrapOnEdge in registration.json are not the ones ParameterSetLoader knows");
		foreach (string value in wrapOnEdge)
		{
			Dictionary<string, object> item = NewItem(properties, BulletPointsSetting);
			item["wrapOnEdge"] = value;
			bool right = LoadItems(bulletPoints: new[] { item }).BulletPoints[0].WrapIsAtRight;
			Check(right == (value == "right"), "wrapOnEdge \"" + value + "\" loaded as " + right);
		}

		Console.WriteLine("registration.json value tests PASSED.");
	}

	static void TestParameterSetLoader()
	{
		JObject properties = ReadRegistrationProperties();

		// \t is a tab in every string that can have one
		Dictionary<string, object> block = NewItem(properties, CommentBlocksSetting);
		block["blockStartType"] = "alwaysOnOwnLine";
		block["blockStart"] = "/*\\t";
		block["blockEndType"] = "alwaysOnOwnLine";
		block["blockEnd"] = "\\t*/";
		block["lineStart"] = "//\\t";
		CommentBlock loadedBlock = LoadItems(commentBlocks: new[] { block }).CommentBlocks[0];
		Check(
			(loadedBlock.BlockStart == "/*\t") && (loadedBlock.BlockEnd == "\t*/") && (loadedBlock.LineStart == "//\t"),
			"\\t in a comment block was not loaded as a tab"
		);
		Dictionary<string, object> bullet = NewItem(properties, BulletPointsSetting);
		bullet["matchString"] = "\\t- ";
		Check(LoadItems(bulletPoints: new[] { bullet }).BulletPoints[0].MatchString == "\t- ", "\\t in a bullet was not loaded as a tab");
		Dictionary<string, object> breakFlow = NewItem(properties, BreakFlowStringsSetting);
		breakFlow["matchString"] = "a\\tb";
		Check(LoadItems(breakFlowStrings: new[] { breakFlow }).BreakFlowStrings[0].MatchString == "a\tb", "\\t in a break flow string was not loaded as a tab");

		// the file types are split on ; and trimmed
		block = NewItem(properties, CommentBlocksSetting);
		block["fileTypes"] = "*.a; *.b ;Makefile";
		Check(LoadItems(commentBlocks: new[] { block }).CommentBlocks[0].FileAssociations.SequenceEqual(new[] { "*.a", "*.b", "Makefile" }), "The file types were not split and trimmed");

		// a boolean can come as a string
		bullet = NewItem(properties, BulletPointsSetting);
		bullet["isRegex"] = "False";
		bullet["matchString"] = "(";
		Check(!LoadItems(bulletPoints: new[] { bullet }).BulletPoints[0].IsRegEx, "isRegex given as the string \"False\" was not loaded as false");

		// errors name the setting that is wrong
		block = NewItem(properties, CommentBlocksSetting);
		block.Remove("fileTypes");
		CheckThrowsWithMessage(() => LoadItems(commentBlocks: new[] { block }), "Block \"New comment block\": Missing value for \"fileTypes\"", "Loading a block with no file types");
		block = NewItem(properties, CommentBlocksSetting);
		block["fileTypes"] = null;
		CheckThrowsWithMessage(() => LoadItems(commentBlocks: new[] { block }), "Missing value for \"fileTypes\"", "Loading a block whose file types are null");
		block = NewItem(properties, CommentBlocksSetting);
		block["blockStartType"] = "alwaysOnOwnLine";
		block["blockStart"] = "(";
		block["blockStartIsRegex"] = true;
		CheckThrowsWithMessage(() => LoadItems(commentBlocks: new[] { block }), "Block \"New comment block\": Block start regular expression is invalid", "Loading a block with an invalid regex");
		block = NewItem(properties, CommentBlocksSetting);
		block["blockStartType"] = "sometimes";
		CheckThrowsWithMessage(() => LoadItems(commentBlocks: new[] { block }), "Block \"New comment block\"", "Loading a block with an unknown block start type");
		bullet = NewItem(properties, BulletPointsSetting);
		CheckThrowsWithMessage(() => LoadItems(bulletPoints: new[] { bullet, NewItem(properties, BulletPointsSetting) }), "Bullet \"New Bullet Point\": Bullet name must be unique", "Loading two bullets with the same name");
		breakFlow = NewItem(properties, BreakFlowStringsSetting);
		breakFlow["isRegex"] = true;
		breakFlow["matchString"] = "[";
		CheckThrowsWithMessage(() => LoadItems(breakFlowStrings: new[] { breakFlow }), "Break flow string \"New break flow string\": Force line break regular expression is invalid", "Loading a break flow string with an invalid regex");
		CheckThrowsWithMessage(() => LoadItems(wrapWidth: 9), "Comment wrap width", "Loading a wrap width of 9");
		CheckThrowsWithMessage(() => LoadItems(minimumBlockWidth: 9), "Minimum Block width", "Loading a minimum block width of 9");

		Console.WriteLine("ParameterSetLoader tests PASSED.");
	}
}