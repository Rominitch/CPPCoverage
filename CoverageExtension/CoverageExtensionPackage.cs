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
using System.ComponentModel;
using System.Drawing;
using System.Linq;

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
    [ProvideToolWindow(typeof(NubiloSoft.CoverageExt.CoverageSelector))]
    [ProvideOptionPage(typeof(CoverageOptionPageGrid), "CppCoverage", "Settings", 3110, 3111, true)]
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

        private EnvDTE.DTE     dte;
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
                
                // Restore parameters
                var options = (CoverageOptionPageGrid)GetDialogPage(typeof(CoverageOptionPageGrid));
                if(options != null)
                    options.LoadSettingsFromStorage();

                // Add event tracker
                eventSolution = new SolutionEvent(solutionService, dte);

                // Build menu / dialog
                await CoverageMenu.InitializeAsync(this, dte as EnvDTE80.DTE2);
                await NubiloSoft.CoverageExt.CoverageSelectorCommand.InitializeAsync(this);

                // Call auto load
                eventSolution.OnAfterOpenSolution(null, 0);

                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "InitializeAsync CppCoverage done"));
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

            this.dte = this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE.DTE;

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

        // Option Page
        public class CoverageOptionPageGrid : DialogPage
        {
            [Category("Options")]
            [DisplayName("Show code coverage")]
            [Description("Should we show the code coverage or not")]
            public bool ShowCodeCoverage { get; set; } = true;

            [Category("Options")]
            [DisplayName("Verbose")]
            [Description("Show messages")]
            public bool Verbose { get; set; } = false;

            [Category("Options")]
            [DisplayName("Sharable")]
            [Description("Allow to shared the CodeCoverage.cov file to another computer.")]
            public bool OptionIsSharable { get; set; } = true;

            [Category("Options")]
            [DisplayName("Excludes")]
            [Description("List of keyword to find into file path to exclude this file into final coverage file.")]
            public string OptionExcludes { get; set; } = "";

            [Category("Colors")]
            [DisplayName("Uncovered Brush")]
            [Description("Uncovered Brush")]
            [TypeConverter(typeof(ColorConverter))]
            public Color UncoveredBrush { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xCF, 0xB8);

            [Category("Colors")]
            [DisplayName("Uncovered Pen")]
            [Description("Uncovered Pen")]
            [TypeConverter(typeof(ColorConverter))]
            public Color UncoveredPen { get; set; } = Color.FromArgb(0xD0, 0xFF, 0xCF, 0xB8);

            [Category("Colors")]
            [DisplayName("Covered Brush")]
            [Description("Covered Brush")]
            [TypeConverter(typeof(ColorConverter))]
            public Color CoveredBrush { get; set; } = Color.FromArgb(0xFF, 0xBD, 0xFC, 0xBF);

            [Category("Colors")]
            [DisplayName("Covered Pen")]
            [Description("Covered Pen")]
            [TypeConverter(typeof(ColorConverter))]
            public Color CoveredPen { get; set; } = Color.FromArgb(0xD0, 0xBD, 0xFC, 0xBF);

            public override void LoadSettingsFromStorage()
            {
                base.LoadSettingsFromStorage();
                UpdateSettings();
            }

            public override void SaveSettingsToStorage()
            {
                base.SaveSettingsToStorage();
                UpdateSettings();
            }

            public void UpdateSettings()
            {
                Func<Color, System.Windows.Media.Color> convert = (Color input) =>
                {
                    return System.Windows.Media.Color.FromArgb(input.A, input.R, input.G, input.B);
                };

                CoverageEnvironment.Verbose             = Verbose;

                CoverageEnvironment.ShowCodeCoverage    = ShowCodeCoverage;
                CoverageEnvironment.isSharable          = OptionIsSharable;
                CoverageEnvironment.excludes            = OptionExcludes.Split(';').ToList();

                CoverageEnvironment.UncoveredBrushColor = convert(UncoveredBrush);
                CoverageEnvironment.UncoveredPenColor   = convert(UncoveredPen);
                CoverageEnvironment.CoveredBrushColor   = convert(CoveredBrush);
                CoverageEnvironment.CoveredPenColor     = convert(CoveredPen);

                CoverageEnvironment.emitSettingsChanged();
            }

        }
    }
}
