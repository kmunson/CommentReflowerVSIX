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
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // This attribute registers the product name and description. Visual Studio 2026's Help >
    // About dialog doesn't list installed products, so no icon is registered: an icon resource
    // would stop 'dotnet build' from building VSPackage.resx.
    [InstalledProductRegistration("#110", "#112", "1.3.3")]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers registration.json, which defines the settings shown in
    // Tools > Options > Comment Reflower AI.
    [ProvideSettingsManifest]
    // Initialize the package when a text editor window is opened. The reflow commands are
    // invisible until QueryStatusCallback shows them, and that won't happen until the package
    // is loaded.
    [ProvideAutoLoad(VSConstants.VsEditorFactoryGuid.TextEditor_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(GuidList.guidCommentReflowerPkgString)]
    public sealed class CommentReflowerPackage : AsyncPackage
    {
        private UnifiedSettings Settings { get; set; }

        /// <summary>
        /// Default constructor of the package. Inside this method you can place any initialization
        /// code that does not require any Visual Studio service because at this point the package
        /// object is created but not sited yet inside Visual Studio environment. The place to do
        /// all the other initialization is the Initialize method.
        /// </summary>
        public CommentReflowerPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var settingsManager = (ISettingsManager)await GetServiceAsync(typeof(SVsUnifiedSettingsManager));
            Assumes.Present(settingsManager);
            Settings = new UnifiedSettings(settingsManager.GetReader());

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = (IMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService));
            Assumes.Present(mcs);

            CommandID menuCommandID;
            OleMenuCommand menuItem;

            menuCommandID = new CommandID(GuidList.guidCommentReflowerCmdSet, PkgCmdIDList.cmdidReflowPoint);
            menuItem = new OleMenuCommand(MenuItemCallback, null, QueryStatusCallback, menuCommandID);
            mcs.AddCommand(menuItem);

            menuCommandID = new CommandID(GuidList.guidCommentReflowerCmdSet, PkgCmdIDList.cmdidReflowSelection);
            menuItem = new OleMenuCommand(MenuItemCallback, null, QueryStatusCallback, menuCommandID);
            mcs.AddCommand(menuItem);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Settings != null)
            {
                Settings.Dispose();
                Settings = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Sets visibility and enabled state of menu commands.
        /// </summary>
        private void QueryStatusCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)GetService(typeof(DTE));
            Assumes.Present(dte);
            Document document = dte.ActiveDocument;
            var menuCommand = (OleMenuCommand)sender;
            int commandID = menuCommand.CommandID.ID;
            switch (commandID)
            {
                case PkgCmdIDList.cmdidReflowPoint:
                case PkgCmdIDList.cmdidReflowSelection:
                    // If the settings are invalid, the commands stay available so that running
                    // one reports the problem.
                    bool hasBlocks;
                    try
                    {
                        hasBlocks = document != null && Settings.GetParams().getBlocksForFileName(document.Name).Count > 0;
                    }
                    catch (ArgumentException)
                    {
                        hasBlocks = document != null;
                    }

                    if (!hasBlocks)
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
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)GetService(typeof(DTE));
            Assumes.Present(dte);
            Document document = dte.ActiveDocument;
            var menuCommand = (OleMenuCommand)sender;
            int commandID = menuCommand.CommandID.ID;
            switch (commandID)
            {
                case PkgCmdIDList.cmdidReflowPoint:
                case PkgCmdIDList.cmdidReflowSelection:
                    CommentReflowerLib.ParameterSet parameters;
                    try
                    {
                        parameters = Settings.GetParams();
                    }
                    catch (ArgumentException error)
                    {
                        VsShellUtilities.ShowMessageBox(this,
                            "The settings in Tools > Options > Comment Reflower AI are invalid:\n" + error.Message,
                            "Comment Reflower Settings Error",
                            OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        break;
                    }

                    var selection = (TextSelection)document.Selection;
                    selection.DTE.UndoContext.Open("CommentReflower");
                    try
                    {
                        switch (commandID)
                        {
                            case PkgCmdIDList.cmdidReflowPoint:
                                if (!CommentReflowerLib.CommentReflowerObj.WrapBlockContainingPoint(parameters,document.Name, selection.ActivePoint))
                                {
                                    ShowStatusBarText("Comment Reflower: There is no comment at the cursor.");
                                }
                                break;
                            case PkgCmdIDList.cmdidReflowSelection:
                                if (!CommentReflowerLib.CommentReflowerObj.WrapAllBlocksInSelection(parameters,document.Name, selection.TopPoint, selection.BottomPoint))
                                {
                                    ShowStatusBarText("Comment Reflower: There are no comments in the selection.");
                                }
                                break;
                        }
                    }
                    catch (Exception error)
                    {
                        VsShellUtilities.ShowMessageBox(this, error.ToString(), "Comment Reflower Error",
                            OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    finally
                    {
                        selection.DTE.UndoContext.Close();
                    }
                    break;
            }
        }

        /// <summary>
        /// Shows a message in the status bar, for results that don't need the user to act.
        /// </summary>
        private void ShowStatusBarText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var statusBar = (IVsStatusbar)GetService(typeof(SVsStatusbar));
            Assumes.Present(statusBar);

            // Don't overwrite the status bar while another component has frozen it.
            int frozen;
            statusBar.IsFrozen(out frozen);
            if (frozen == 0)
            {
                statusBar.SetText(text);
            }
        }
    }
}
