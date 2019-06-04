using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using NubiloSoft.CoverageExt.Report;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.ObjectModel;
using NubiloSoft.CoverageExt.Sources;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NubiloSoft.CoverageExt
{
    public class SampleToolWindowState
    {
        public EnvDTE80.DTE2 DTE { get; set; }
    }

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#1110", "#1112", "1.0", IconResourceID = 1400)] // Info on this package for Help/About
    [ProvideToolWindow(typeof(CoverageReportToolWindow))]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(CoverageMenuPackage.PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CoverageMenuPackage : AsyncPackage
    {
        /// <summary>
        /// CoverageMenuPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "064052bb-e23a-4200-bdd9-b3cc715b8288";

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageMenuPackage"/> class.
        /// </summary>
        public CoverageMenuPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Build CppCoverage"));
        }

        private EnvDTE80.DTE2  dte;
        private DteInitializer dteInitializer;
        private IVsSolution    solutionService;
        private SolutionEvent  eventSolution;
        public ObservableCollection<string> EventsList { get; set; }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "InitializeAsync CppCoverage"));

                // When initialized asynchronously, the current thread may be a background thread at this point.
                // Do any initialization that requires the UI thread after switching to the UI thread.
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                //Critical initialization
                solutionService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
                // Get DTE
                InitializeDTE();

                // Add event tracker
                eventSolution = new SolutionEvent(solutionService, this.dte);

                // Build menu
                await CoverageMenu.InitializeAsync(this, this.dte);
            }
            catch(Exception)
            {
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "InitializeAsync Error"));
            }
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            return toolWindowType.Equals(Guid.Parse(CoverageReportToolWindow.WindowGuidString)) ? this : null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            return toolWindowType == typeof(CoverageReportToolWindow) ? CoverageReportToolWindow.Title : base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            // Perform as much work as possible in this method which is being run on a background thread.
            // The object returned from this method is passed into the constructor of the SampleToolWindow 
            var dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            return new SampleToolWindowState
            {
                DTE = dte
            };
        }

        // See http://www.mztools.com/articles/2013/MZ2013029.aspx
        private void InitializeDTE()
        {
            IVsShell shellService;

            this.dte = this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;

            if (this.dte == null) // The IDE is not yet fully initialized
            {
                shellService = this.GetService(typeof(SVsShell)) as IVsShell;
                this.dteInitializer = new DteInitializer(shellService, this.InitializeDTE);
            }
            else
            {
                this.dteInitializer = null;
            }
        }
    }
}
