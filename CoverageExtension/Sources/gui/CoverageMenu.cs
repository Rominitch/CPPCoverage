using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NubiloSoft.CoverageExt.Properties;
using Task = System.Threading.Tasks.Task;
using NubiloSoft.CoverageExt.Report;
using Microsoft.VisualStudio.VCProjectEngine;

namespace NubiloSoft.CoverageExt
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CoverageMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int ReportId     = 0x0100;
        public const int PreferenceId = 0x0101;
        public const int AboutId      = 0x0102;
        public const int CtxRunId     = 0x0103;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet     = new Guid("f35ab8cc-88d2-418f-a169-b48e6ea4c6dc");
        public static readonly Guid ContextMenuSet = new Guid("CAA6399C-8807-43AB-A999-2CA0DB82A56C");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CoverageMenu(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var reportID = new CommandID(CommandSet, ReportId);
            var reportItem = new MenuCommand(this.OnShowReport, reportID);
            commandService.AddCommand(reportItem);

            var preferenceID = new CommandID(CommandSet, PreferenceId);
            var preferenceItem = new MenuCommand(this.OnShowPreferences, preferenceID);
            commandService.AddCommand(preferenceItem);

            var aboutID = new CommandID(CommandSet, AboutId);
            var aboutItem = new MenuCommand(this.OnShowAbout, aboutID);
            commandService.AddCommand(aboutItem);

            // Create the command for the context menu
            CommandID contextMenuCommandID = new CommandID(ContextMenuSet, CtxRunId);
            OleMenuCommand menuItem = new OleMenuCommand(ProjectContextMenuItemCallback, contextMenuCommandID);
            menuItem.BeforeQueryStatus += ProjectContextMenuItem_BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CoverageMenu Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }
        private EnvDTE80.DTE2 dte;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package, EnvDTE80.DTE2 ctxDTE)
        {
            // Switch to the main thread - the call to AddCommand in CoverageMenu's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CoverageMenu(package, commandService);
            Instance.dte = ctxDTE;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnShowReport(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(CoverageReportToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void OnShowPreferences(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Options options = new Options();
            options.ShowModal();
            /*
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(OptionsToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            */
        }

        private void OnShowAbout(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "CppCoverage v3.1.0\nVisite https://github.com/atlaste/CPPCoverage \n\nNubiloSoft");
            string title = "About";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private EnvDTE80.DTE2 GetDTE()
        {
            return this.dte;
        }

        /// <summary>
        /// Checks if we should render the context menu item or not.
        /// </summary>
        private void ProjectContextMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var dte = GetDTE();
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (menuCommand != null && dte != null)
            {
                menuCommand.Visible = false;  // default to not visible
                Array selectedProjects = (Array)dte.ActiveSolutionProjects;
                //only support 1 selected project
                if (selectedProjects.Length == 1)
                {
                    EnvDTE.Project project = (EnvDTE.Project)selectedProjects.GetValue(0);

                    // TODO FIXME: We should probably check if it's a DLL as well.

                    if (project.FullName.EndsWith(".vcxproj"))
                    {
                        menuCommand.Visible = true;
                    }
                }
            }
        }

        private void ProjectContextMenuItemCallback(object sender, EventArgs e)
        {
            var dte = GetDTE().DTE;

            OutputWindow outputWindow = null;
            try
            {
                outputWindow = new OutputWindow(dte);

                OleMenuCommand menuCommand = sender as OleMenuCommand;
                if (menuCommand != null && dte != null)
                {
                    Array selectedProjects = (Array)dte.ActiveSolutionProjects;
                    //only support 1 selected project
                    if (selectedProjects.Length == 1)
                    {
                        EnvDTE.Project project = (EnvDTE.Project)selectedProjects.GetValue(0);
                        var vcproj = project.Object as VCProject;
                        if (vcproj != null)
                        {
                            IVCCollection configs = (IVCCollection)vcproj.Configurations;
                            VCConfiguration cfg = (VCConfiguration)vcproj.ActiveConfiguration;
                            VCDebugSettings debug = (VCDebugSettings)cfg.DebugSettings;

                            string command = null;
                            string arguments = null;
                            string workingDirectory = null;
                            if (debug != null)
                            {
                                command = cfg.Evaluate(debug.Command);
                                workingDirectory = cfg.Evaluate(debug.WorkingDirectory);
                                arguments = cfg.Evaluate(debug.CommandArguments);
                            }

                            VCPlatform currentPlatform = (VCPlatform)cfg.Platform;

                            string platform = currentPlatform == null ? null : currentPlatform.Name;
                            if (platform != null)
                            {
                                platform = platform.ToLower();
                                if (platform.Contains("x64"))
                                {
                                    platform = "x64";
                                }
                                else if (platform.Contains("x86") || platform.Contains("win32"))
                                {
                                    platform = "x86";
                                }
                                else
                                {
                                    throw new NotSupportedException("Platform is not supported.");
                                }
                            }
                            else
                            {
                                cfg = (VCConfiguration)configs.Item("Debug|x64");
                                platform = "x64";

                                if (cfg == null)
                                {
                                    throw new NotSupportedException("Cannot find x64 platform for project.");
                                }
                            }

                            if (command == null || String.IsNullOrEmpty(command))
                                command = cfg.PrimaryOutput;

                            if (command != null)
                            {
                                var solutionFolder = System.IO.Path.GetDirectoryName(dte.Solution.FileName);

                                CoverageExecution executor = new CoverageExecution(dte, outputWindow);
                                executor.Start(
                                    solutionFolder,
                                    platform,
                                    System.IO.Path.GetDirectoryName(command),
                                    System.IO.Path.GetFileName(command),
                                    workingDirectory,
                                    arguments);
                            }
                        }
                    }
                }
            }
            catch (NotSupportedException ex)
            {
                if (outputWindow != null)
                {
                    outputWindow.WriteLine("Error running coverage: {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                if (outputWindow != null)
                {
                    outputWindow.WriteLine("Unexpected code coverage failure; error: {0}", ex.ToString());
                }
            }
        }
    }
}
