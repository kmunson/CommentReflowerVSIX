// Comment Reflower VSIX Package
// Copyright (C) 2015  Kristofel Munson
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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CommentReflower
{
    /// <summary>
    /// Lets Visual Studio save and load the settings through its own settings mechanism
    /// (Tools > Import and Export Settings). It is registered with ProvideProfile only, not
    /// ProvideOptionPage, so it has no page in Tools > Options. The settings are still edited
    /// with the Settings dialog.
    /// </summary>
    [Guid(GuidList.guidSettingsProfileString)]
    public sealed class SettingsProfile : DialogPage
    {
        private const string CollectionName = "Settings";
        private const string PropertyName = "Params";

        /// <summary>
        /// Raised after imported settings have been written to the user settings store.
        /// </summary>
        internal static event EventHandler SettingsImported;

        // The settings as XML, as written by ParameterSet.writeToXmlMemoryStream.
        private string mXml;

        /// <summary>
        /// Reads the settings from the user settings store. Returns null if there are none.
        /// </summary>
        internal static CommentReflowerLib.ParameterSet ReadFromStore(SettingsManager settingsManager)
        {
            WritableSettingsStore userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!userSettingsStore.PropertyExists(CollectionName, PropertyName))
            {
                return null;
            }
            return new CommentReflowerLib.ParameterSet(userSettingsStore.GetMemoryStream(CollectionName, PropertyName));
        }

        /// <summary>
        /// Writes the settings to the user settings store.
        /// </summary>
        internal static void WriteToStore(SettingsManager settingsManager, CommentReflowerLib.ParameterSet parameters)
        {
            WritableSettingsStore userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            userSettingsStore.CreateCollection(CollectionName);
            userSettingsStore.SetMemoryStream(CollectionName, PropertyName, parameters.writeToXmlMemoryStream());
        }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CommentReflowerLib.ParameterSet parameters = null;
            try
            {
                parameters = ReadFromStore(new ShellSettingsManager(ServiceProvider.GlobalProvider));
            }
            catch (Exception)
            {
                // Unreadable settings are reset to the default, the same as when the package loads.
            }
            mXml = ToXml(parameters ?? new CommentReflowerLib.ParameterSet());
        }

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (mXml == null)
            {
                return;
            }
            WriteToStore(new ShellSettingsManager(ServiceProvider.GlobalProvider), FromXml(mXml));
            SettingsImported?.Invoke(this, EventArgs.Empty);
        }

        public override void SaveSettingsToXml(IVsSettingsWriter writer)
        {
            if (mXml != null)
            {
                ErrorHandler.ThrowOnFailure(writer.WriteSettingString(PropertyName, mXml));
            }
        }

        public override void LoadSettingsFromXml(IVsSettingsReader reader)
        {
            mXml = null;
            if (ErrorHandler.Failed(reader.ReadSettingString(PropertyName, out string xml)) || xml == null)
            {
                return;
            }
            try
            {
                // Check that the settings are valid before they are saved.
                FromXml(xml);
                mXml = xml;
            }
            catch (Exception e)
            {
                reader.ReportError("Comment Reflower settings could not be imported: " + e.Message,
                    (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            }
        }

        private static string ToXml(CommentReflowerLib.ParameterSet parameters)
        {
            using (MemoryStream memoryStream = parameters.writeToXmlMemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                return streamReader.ReadToEnd();
            }
        }

        private static CommentReflowerLib.ParameterSet FromXml(string xml)
        {
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                return new CommentReflowerLib.ParameterSet(memoryStream);
            }
        }
    }
}
