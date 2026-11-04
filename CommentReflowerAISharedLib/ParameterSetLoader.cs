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

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CommentReflowerLib;

/// <summary>
/// Makes a ParameterSet from the values of the settings in Tools > Options > Comment
/// Reflower AI, which are defined in registration.json. The arrays have an item for each
/// block, bullet or break flow string, with its properties by the names used there.
/// </summary>
public static class ParameterSetLoader
{
	/// <summary>
	/// Returns the settings as a ParameterSet. Throws ArgumentException if they are
	/// invalid, naming the block, bullet or break flow string that is.
	/// </summary>
	public static ParameterSet Load(
		int wrapWidth,
		int minimumBlockWidth,
		bool useTabsToIndent,
		IEnumerable<IReadOnlyDictionary<string, object>> commentBlocks,
		IEnumerable<IReadOnlyDictionary<string, object>> bulletPoints,
		IEnumerable<IReadOnlyDictionary<string, object>> breakFlowStrings
	)
	{
		var p = new ParameterSet();
		p.CommentBlocks.Clear();
		p.BulletPoints.Clear();
		p.BreakFlowStrings.Clear();

		p.WrapWidth = wrapWidth;
		p.MinimumBlockWidth = minimumBlockWidth;
		p.UseTabsToIndent = useTabsToIndent;
		p.ValidateGeneralSettings();

		foreach (var item in commentBlocks)
		{
			string name = GetString(item, "name");
			try
			{
				var commentBlock = new CommentBlock(
					name,
					CommentBlock.CreateFileAssocFromString(GetString(item, "fileTypes")),
					GetBlockType(item, "blockStartType"),
					FromDisplay(GetString(item, "blockStart")),
					GetBool(item, "blockStartIsRegex"),
					GetBlockType(item, "blockEndType"),
					FromDisplay(GetString(item, "blockEnd")),
					GetBool(item, "blockEndIsRegex"),
					FromDisplay(GetString(item, "lineStart")),
					GetString(item, "firstLine") == "mustNotContainTextBeforeStart"
				);
				p.CommentBlocks.Add(commentBlock);
				p.ValidateCommentBlock(p.CommentBlocks.Count - 1);
			}
			catch (ArgumentException e)
			{
				throw new ArgumentException(string.Format("Block \"{0}\": {1}", name, e.Message), e);
			}
		}

		foreach (var item in bulletPoints)
		{
			string name = GetString(item, "name");
			try
			{
				var bulletPoint = new BulletPoint(
					name,
					FromDisplay(GetString(item, "matchString")),
					GetBool(item, "isRegex"),
					GetString(item, "wrapOnEdge") == "right"
				);
				p.BulletPoints.Add(bulletPoint);
				p.ValidateBullet(p.BulletPoints.Count - 1);
			}
			catch (ArgumentException e)
			{
				throw new ArgumentException(string.Format("Bullet \"{0}\": {1}", name, e.Message), e);
			}
		}

		foreach (var item in breakFlowStrings)
		{
			string name = GetString(item, "name");
			try
			{
				var breakFlowString = new BreakFlowString(
					name,
					FromDisplay(GetString(item, "matchString")),
					GetBool(item, "isRegex"),
					GetBool(item, "neverReflowLine"),
					GetBool(item, "neverReflowIntoNextLine")
				);
				p.BreakFlowStrings.Add(breakFlowString);
				p.ValidateBreakFlowString(p.BreakFlowStrings.Count - 1);
			}
			catch (ArgumentException e)
			{
				throw new ArgumentException(string.Format("Break flow string \"{0}\": {1}", name, e.Message), e);
			}
		}

		return p;
	}

	private static object GetProperty(IReadOnlyDictionary<string, object> item, string name)
	{
		if (!item.TryGetValue(name, out object value) || value == null)
		{
			throw new ArgumentException(string.Format("Missing value for \"{0}\"", name));
		}
		return value;
	}

	private static string GetString(IReadOnlyDictionary<string, object> item, string name)
	{
		return Convert.ToString(GetProperty(item, name), CultureInfo.InvariantCulture);
	}

	private static bool GetBool(IReadOnlyDictionary<string, object> item, string name)
	{
		return Convert.ToBoolean(GetProperty(item, name), CultureInfo.InvariantCulture);
	}

	private static StartEndBlockType GetBlockType(IReadOnlyDictionary<string, object> item, string name)
	{
		return (StartEndBlockType)Enum.Parse(typeof(StartEndBlockType), GetString(item, name), true);
	}

	/// <summary>
	/// The settings UI can't take a tab character, so tabs are typed as \t, as they
	/// were in the old settings dialog.
	/// </summary>
	private static string FromDisplay(string str)
	{
		return str.Replace("\\t", "\t");
	}
}
