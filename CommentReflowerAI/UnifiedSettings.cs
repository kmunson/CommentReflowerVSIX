// Comment Reflower Unified Settings
// Copyright (C) 2026  Kristofel Munson
//
// This program is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation; either version 2 of the License, or (at your option) any later
// version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using CommentReflowerLib;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace CommentReflower
{
    /// <summary>
    /// The service that provides Unified Settings' ISettingsManager. Its type is only in an
    /// internal interop assembly, so it is declared here with the same GUID, which is what
    /// GetServiceAsync uses to find the service.
    /// </summary>
    [Guid("e3684f31-344e-42ea-9047-b620fdc7ac25")]
    internal interface SVsUnifiedSettingsManager
    {
    }

    /// <summary>
    /// Reads the settings shown in Tools > Options > Comment Reflower AI, which are defined in
    /// registration.json, into a ParameterSet.
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
        private readonly object _lock = new object();

        // The settings as last read, or null if they have changed since.
        private ParameterSet _params;

        public UnifiedSettings(ISettingsReader reader)
        {
            _reader = reader;
            _subscription = reader.SubscribeToChanges(update =>
            {
                lock (_lock)
                {
                    _params = null;
                }
            }, "commentReflowerAI.*");
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
                if (_params == null)
                {
                    _params = Load();
                }
                return _params;
            }
        }

        private ParameterSet Load()
        {
            var p = new ParameterSet();
            p.mCommentBlocks.Clear();
            p.mBulletPoints.Clear();
            p.mBreakFlowStrings.Clear();

            p.mWrapWidth = GetValue<int>(WrapWidth);
            p.mMinimumBlockWidth = GetValue<int>(MinimumBlockWidth);
            p.mUseTabsToIndent = GetValue<bool>(UseTabsToIndent);
            p.validateGeneralSettings();

            foreach (var item in GetArray(CommentBlocks))
            {
                string name = GetString(item, "name");
                try
                {
                    p.mCommentBlocks.Add(new CommentBlock(
                        name,
                        CommentBlock.createFileAssocFromString(GetString(item, "fileTypes")),
                        GetBlockType(item, "blockStartType"),
                        FromDisplay(GetString(item, "blockStart")),
                        GetBool(item, "blockStartIsRegex"),
                        GetBlockType(item, "blockEndType"),
                        FromDisplay(GetString(item, "blockEnd")),
                        GetBool(item, "blockEndIsRegex"),
                        FromDisplay(GetString(item, "lineStart")),
                        GetString(item, "firstLine") == "mustNotContainTextBeforeStart"));
                    p.validateCommentBlock(p.mCommentBlocks.Count - 1);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(string.Format("Block \"{0}\": {1}", name, e.Message), e);
                }
            }

            foreach (var item in GetArray(BulletPoints))
            {
                string name = GetString(item, "name");
                try
                {
                    p.mBulletPoints.Add(new BulletPoint(
                        name,
                        FromDisplay(GetString(item, "matchString")),
                        GetBool(item, "isRegex"),
                        GetString(item, "wrapOnEdge") == "right"));
                    p.validateBullet(p.mBulletPoints.Count - 1);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(string.Format("Bullet \"{0}\": {1}", name, e.Message), e);
                }
            }

            foreach (var item in GetArray(BreakFlowStrings))
            {
                string name = GetString(item, "name");
                try
                {
                    p.mBreakFlowStrings.Add(new BreakFlowString(
                        name,
                        FromDisplay(GetString(item, "matchString")),
                        GetBool(item, "isRegex"),
                        GetBool(item, "neverReflowLine"),
                        GetBool(item, "neverReflowIntoNextLine")));
                    p.validateBreakFlowString(p.mBreakFlowStrings.Count - 1);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(string.Format("Break flow string \"{0}\": {1}", name, e.Message), e);
                }
            }

            return p;
        }

        // RequireValidation falls back to the default value if the stored value is invalid.
        private T GetValue<T>(string moniker)
        {
            var result = _reader.GetValue<T>(moniker, SettingReadOptions.RequireValidation);
            if (result.Outcome != SettingRetrievalOutcome.Success)
            {
                throw new ArgumentException(string.Format("Unable to read setting {0}: {1} {2}", moniker, result.Outcome, result.Message));
            }
            return result.Value;
        }

        private IReadOnlyList<IReadOnlyDictionary<string, object>> GetArray(string moniker)
        {
            var result = _reader.GetArray<IReadOnlyDictionary<string, object>>(moniker, SettingReadOptions.RequireValidation);
            if (result.Outcome != SettingRetrievalOutcome.Success)
            {
                throw new ArgumentException(string.Format("Unable to read setting {0}: {1} {2}", moniker, result.Outcome, result.Message));
            }
            return result.Value;
        }

        private static object GetProperty(IReadOnlyDictionary<string, object> item, string name)
        {
            object value;
            if (!item.TryGetValue(name, out value) || value == null)
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
        /// The settings UI can't take a tab character, so tabs are typed as \t, as they were in
        /// the old settings dialog.
        /// </summary>
        private static string FromDisplay(string str)
        {
            return str.Replace("\\t", "\t");
        }
    }
}
