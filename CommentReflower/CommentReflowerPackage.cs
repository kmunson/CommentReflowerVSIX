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

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Win32;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CommentReflower
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio is to
    /// implement the IVsPackage interface and register itself with the shell. This package uses the
    /// helper classes defined inside the Managed Package Framework (MPF) to do it: it derives from
    /// the Package class that provides the implementation of the IVsPackage interface and uses the
    /// registration attributes defined in the framework to register itself and its components with
    /// the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.1", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Initialize the package when a text editor window is opened. This ensures that the visibility
    // state of reflow commands is set correctly before opening the menu. Otherwise
    // QueryStateCallback will not happen until after the settings command is invoked.
    [ProvideAutoLoad(VSConstants.VsEditorFactoryGuid.TextEditor_string)]
    [Guid(GuidList.guidCommentReflowerPkgString)]
    public sealed class CommentReflowerPackage : Package
    {
        private CommentReflowerLib.ParameterSet Params { get; set; }
        private SettingsManager SettingsManager { get; set; }

        /// <summary>
        /// Default constructor of the package. Inside this method you can place any initialization
        /// code that does not require any Visual Studio service because at this point the package
        /// object is created but not sited yet inside Visual Studio environment. The place to do
        /// all the other initialization is the Initialize method.
        /// </summary>
        public CommentReflowerPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            SettingsManager = new ShellSettingsManager(this);

            WritableSettingsStore userSettingsStore = SettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (userSettingsStore.PropertyExists("Settings", "Params"))
            {
                try
                {
                    Params = new CommentReflowerLib.ParameterSet(userSettingsStore.GetMemoryStream("Settings", "Params"));
                }
                catch (Exception)
                {
                    ShowMessageBox("Comment Reflower Error", "Unable to read user settings. Resetting to default.");
                    Params = new CommentReflowerLib.ParameterSet();
                }
            }
            else
            {
                Params = new CommentReflowerLib.ParameterSet();
            }

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = (OleMenuCommandService)GetService(typeof(IMenuCommandService));

            CommandID menuCommandID;
            OleMenuCommand menuItem;

            menuCommandID = new CommandID(GuidList.guidCommentReflowerCmdSet, PkgCmdIDList.cmdidAlignParameters);
            menuItem = new OleMenuCommand(MenuItemCallback, null, QueryStatusCallback, menuCommandID);
            mcs.AddCommand(menuItem);

            menuCommandID = new CommandID(GuidList.guidCommentReflowerCmdSet, PkgCmdIDList.cmdidReflowPoint);
            menuItem = new OleMenuCommand(MenuItemCallback, null, QueryStatusCallback, menuCommandID);
            mcs.AddCommand(menuItem);

            menuCommandID = new CommandID(GuidList.guidCommentReflowerCmdSet, PkgCmdIDList.cmdidReflowSelection);
            menuItem = new OleMenuCommand(MenuItemCallback, null, QueryStatusCallback, menuCommandID);
            mcs.AddCommand(menuItem);

            menuCommandID = new CommandID(GuidList.guidCommentReflowerCmdSet, PkgCmdIDList.cmdidSettings);
            menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
            mcs.AddCommand(menuItem);
        }
        #endregion

        /// <summary>
        /// Sets visibility and enabled state of menu commands.
        /// </summary>
        private void QueryStatusCallback(object sender, EventArgs e)
        {
            var dte = (DTE)GetService(typeof(DTE));
            Document document = dte.ActiveDocument;
            var menuCommand = (OleMenuCommand)sender;
            int commandID = menuCommand.CommandID.ID;
            switch (commandID)
            {
                case PkgCmdIDList.cmdidAlignParameters:
                case PkgCmdIDList.cmdidReflowPoint:
                case PkgCmdIDList.cmdidReflowSelection:
                    if (commandID == PkgCmdIDList.cmdidAlignParameters && !Params.mEnableAlignParams)
                    {
                        menuCommand.Visible = false;
                        menuCommand.Enabled = false;
                    }
                    else if (document == null || Params.getBlocksForFileName(document.Name).Count == 0)
                    {
                        menuCommand.Visible = false;
                        menuCommand.Enabled = false;
                    }
                    else if (commandID == PkgCmdIDList.cmdidReflowSelection && ((TextSelection)document.Selection).IsEmpty)
                    {
                        menuCommand.Visible = true;
                        menuCommand.Enabled = false;
                    }
                    else
                    {
                        menuCommand.Visible = true;
                        menuCommand.Enabled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = (DTE)GetService(typeof(DTE));
            Document document = dte.ActiveDocument;
            var menuCommand = (OleMenuCommand)sender;
            int commandID = menuCommand.CommandID.ID;
            switch (commandID)
            {
                case PkgCmdIDList.cmdidAlignParameters:
                case PkgCmdIDList.cmdidReflowPoint:
                case PkgCmdIDList.cmdidReflowSelection:
                    var selection = (TextSelection)document.Selection;
                    selection.DTE.UndoContext.Open("CommentReflower");
                    try
                    {
                        switch (commandID)
                        {
                            case PkgCmdIDList.cmdidAlignParameters:
                                EditPoint finishPt;
                                if (!CommentReflowerLib.ParameterAlignerObj.go(selection.ActivePoint, out finishPt))
                                {
                                    ShowMessageBox("Comment Reflower", "There is no parameter list at the cursor.");
                                }
                                break;

                            case PkgCmdIDList.cmdidReflowPoint:
                                if (!CommentReflowerLib.CommentReflowerObj.WrapBlockContainingPoint(Params, document.Name, selection.ActivePoint))
                                {
                                    ShowMessageBox("Comment Reflower", "There is no comment at the cursor.");
                                }
                                break;
                            case PkgCmdIDList.cmdidReflowSelection:
                                if (!CommentReflowerLib.CommentReflowerObj.WrapAllBlocksInSelection(Params, document.Name, selection.TopPoint, selection.BottomPoint))
                                {
                                    ShowMessageBox("Comment Reflower", "There are no comments in the selection.");
                                }
                                break;
                        }
                    }
                    catch (Exception error)
                    {
                        ShowMessageBox("Comment Reflower Error", error.ToString());
                    }
                    finally
                    {
                        selection.DTE.UndoContext.Close();
                    }
                    break;
                case PkgCmdIDList.cmdidSettings:
                    string installPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    using (Settings settings = new Settings(Params, installPath))
                    {
                        if (settings.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            Params = settings._params;
                            WritableSettingsStore userSettingsStore = SettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                            userSettingsStore.CreateCollection("Settings");
                            userSettingsStore.SetMemoryStream("Settings", "Params", Params.writeToXmlMemoryStream());
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Simple wrapper for IVsUIShell.ShowMessageBox
        /// </summary>
        private void ShowMessageBox(string title, string format, params object[] args)
        {
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                uiShell.ShowMessageBox(
                0, // dwCompRole
                ref clsid, // rclsidComp
                title, // pszTitle
                string.Format(format, args), // pszText
                string.Empty, // pszHelpFile
                0, // dwHelpContextID
                OLEMSGBUTTON.OLEMSGBUTTON_OK, // msgbutton
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, // msgdefbutton
                OLEMSGICON.OLEMSGICON_INFO, // msgicon
                0, // fSysAlert
                out result // pnResult
                ));
        }
    }
}
