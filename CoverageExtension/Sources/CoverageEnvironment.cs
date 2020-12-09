using System.Collections.Generic;
using System.Windows.Media;

namespace NubiloSoft.CoverageExt
{
    public class CoverageEnvironment
    {
        public static string version = "3.2";
        public static CoverageExecution runner = null;

        /// <summary>
        /// Time of execution of subprocess coverage in ms.
        /// </summary>
        /// 
        #region general properties
        public static bool UseNativeCoverageSupport = true;
        public static int timeoutCoverage = 60000;
        public static bool isSharable = true;

        public static string solutionPath;
        public static string workingCoverageDir;

        public static List<string> excludes;
        public static bool ShowCodeCoverage = true;
        #endregion

        #region color definitions
        public static Color UncoveredBrushColor = Color.FromArgb(0xFF, 0xFF, 0xCF, 0xB8);
        public static Color UncoveredPenColor = Color.FromArgb(0xD0, 0xFF, 0xCF, 0xB8);
        public static Color CoveredBrushColor = Color.FromArgb(0xFF, 0xBD, 0xFC, 0xBF);
        public static Color CoveredPenColor = Color.FromArgb(0xD0, 0xBD, 0xFC, 0xBF);
        #endregion

        // Event
        public static event System.EventHandler OnSettingsChanged;
        public static event System.EventHandler OnStartCoverage;
        public static event System.EventHandler OnFinishCoverage;
        public static event System.EventHandler OnInterruptCoverage;

        public static CoverageExecution launchExecution(EnvDTE.DTE dte, OutputWindow output)
        {
            if (CoverageEnvironment.runner == null)
            {
                CoverageEnvironment.runner = new CoverageExecution(dte, output);
            }
            return CoverageEnvironment.runner;
        }

        public static bool hasSolution()
        {
            return solutionPath != string.Empty;
        }

        public static void configureSolution(string solution)
        {
            solutionPath = solution;
            if (solution != string.Empty)
            {
                workingCoverageDir = System.IO.Path.Combine(solutionPath, ".coverage");
                if (!System.IO.Directory.Exists(workingCoverageDir))
                {
                    var di = System.IO.Directory.CreateDirectory(workingCoverageDir);
                    di.Attributes = System.IO.FileAttributes.Directory | System.IO.FileAttributes.Hidden;
                }
            }
            else
            {
                workingCoverageDir = string.Empty;
            }
        }

        public static string coverageFile()
        {
            return System.IO.Path.Combine(solutionPath, "CodeCoverage.cov");
        }

        public static void emitSettingsChanged()
        {
            OnSettingsChanged?.Invoke(typeof(CoverageEnvironment), System.EventArgs.Empty);
        }
        public static void emitStartCoverage()
        {
            OnStartCoverage?.Invoke(typeof(CoverageEnvironment), System.EventArgs.Empty);
        }
        public static void emitFinishCoverage()
        {
            OnFinishCoverage?.Invoke(typeof(CoverageEnvironment), System.EventArgs.Empty);
        }

        public static void emitInterruptCoverage()
        {
            OnInterruptCoverage?.Invoke(typeof(CoverageEnvironment), System.EventArgs.Empty);
        }

        // Generic UI call
        public static T UiInvoke<T>(System.Func<T> function)
        {
            // If we’re already on the UI thread, just execute the method directly.
            if (Microsoft.VisualStudio.Shell.ThreadHelper.CheckAccess())
            {
                return function();
            }
            T result = default(T);
            // Prefer BeginInvoke over Invoke since BeginInvoke is potentially saver than Invoke.
            using (System.Threading.ManualResetEventSlim eventHandle = new System.Threading.ManualResetEventSlim(false))
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.Generic.BeginInvoke(() => { result = function(); eventHandle.Set(); });
                // Wait for the invoke to complete.
                var success = eventHandle.Wait(System.TimeSpan.FromSeconds(5));
                // If the operation timed out, fail.
                if (!success)
                {
                    throw new System.TimeoutException();
                }

                return result;
            }
        }
    }
}
