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
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CommentReflower;

/// <summary>
/// The service that provides Unified Settings' ISettingsManager. Its type is only
/// in an internal interop assembly, so it is declared here with the same GUID,
/// which is what GetServiceAsync uses to find the service.
/// </summary>
[Guid("e3684f31-344e-42ea-9047-b620fdc7ac25")]
internal class SVsUnifiedSettingsManager
{
}

/// <summary>
/// Reads the settings shown in Tools > Options > Comment Reflower AI, which are
/// defined in registration.json, into a ParameterSet.
/// </summary>
internal sealed class UnifiedSettings : IDisposable
{
	private const string WrapWidth = "commentReflowerAI.general.wrapWidth";
	private const string MinimumBlockWidth = "commentReflowerAI.general.minimumBlockWidth";
	private const string UseTabsToIndent = "commentReflowerAI.general.useTabsToIndent";
	private const string CommentBlocks = "commentReflowerAI.blocks.commentBlocks";
	private const string BulletPoints = "commentReflowerAI.bullets.bulletPoints";
	private const string BreakFlowStrings = "commentReflowerAI.breakFlowStrings.breakFlowStrings";

	private readonly ISettingsReader _reader;
	private readonly IDisposable _subscription;
	private readonly object _lock = new();

	// The settings as last read, or null if they have changed since.
	private ParameterSet _params;

	public UnifiedSettings(ISettingsReader reader)
	{
		_reader = reader;
		_subscription = reader.SubscribeToChanges(
			update =>
			{
				lock (_lock)
				{
					_params = null;
				}
			},
			"commentReflowerAI.*"
		);
	}

	public void Dispose()
	{
		_subscription.Dispose();
	}

	/// <summary>
	/// Returns the current settings. Throws ArgumentException if they are invalid.
	/// </summary>
	public ParameterSet GetParams()
	{
		lock (_lock)
		{
			_params ??= Load();
			return _params;
		}
	}

	private ParameterSet Load()
	{
		return ParameterSetLoader.Load(
			GetValue<int>(WrapWidth),
			GetValue<int>(MinimumBlockWidth),
			GetValue<bool>(UseTabsToIndent),
			GetArray(CommentBlocks),
			GetArray(BulletPoints),
			GetArray(BreakFlowStrings)
		);
	}
	// RequireValidation falls back to the default value if the stored value is
	// invalid.
	private T GetValue<T>(string moniker)
	{
		var result = _reader.GetValue<T>(moniker, SettingReadOptions.RequireValidation);
		if (result.Outcome != SettingRetrievalOutcome.Success)
		{
			throw new ArgumentException(string.Format(
				"Unable to read setting {0}: {1} {2}",
				moniker,
				result.Outcome,
				result.Message
			));
		}
		return result.Value;
	}

	private IReadOnlyList<IReadOnlyDictionary<string, object>> GetArray(string moniker)
	{
		var result = _reader.GetArray<IReadOnlyDictionary<string, object>>(
			moniker,
			SettingReadOptions.RequireValidation
		);
		if (result.Outcome != SettingRetrievalOutcome.Success)
		{
			throw new ArgumentException(string.Format(
				"Unable to read setting {0}: {1} {2}",
				moniker,
				result.Outcome,
				result.Message
			));
		}
		return result.Value;
	}
}
