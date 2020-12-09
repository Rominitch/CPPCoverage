using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NubiloSoft.CoverageExt.Sources;

namespace NubiloSoft.CoverageExt
{
    /// <summary>
    /// Interaction logic for CoverageSelectorControl.
    /// </summary>
    public partial class CoverageSelectorControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageSelectorControl"/> class.
        /// </summary>
        public CoverageSelectorControl()
        {
            this.InitializeComponent();

            // listen to events that change the setting properties
            CoverageEnvironment.OnStartCoverage  += slotUpdateStart;
            CoverageEnvironment.OnFinishCoverage += slotUpdateFinish;
        }
        private void updateGUI()
        {
            var sol = CoverageEnvironment.hasSolution();
            var locked = CoverageEnvironment.runner != null && CoverageEnvironment.runner.isRunning();
            
            this.CleanCoverage.IsEnabled  = sol && !locked;
            this.StopCoverage.IsEnabled   = sol && locked;
            this.UpdateCoverage.IsEnabled = sol && !locked;
        }

        private void CoverageSelector_Loaded(object sender, RoutedEventArgs e)
        {
            updateGUI();
        }

        private void CoverageSelector_GotFocus(object sender, RoutedEventArgs e)
        {
            
        }

        private void CoverageSelector_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            updateGUI();
        }

        private void slotUpdateStart(object sender, System.EventArgs e)
        {
            CoverageEnvironment.UiInvoke(() =>
            {
                updateGUI();
                readCoverageFile(); 
                return true;
            });
        }

        private void slotUpdateFinish(object sender, System.EventArgs e)
        {
            CoverageEnvironment.UiInvoke(() =>
            {
                updateGUI();
                readCoverageFile();
                return true;
            });
        }

        private void onStop(object sender, RoutedEventArgs e)
        {
            CoverageEnvironment.emitInterruptCoverage();
        }
        private void onClean(object sender, RoutedEventArgs e)
        {
            try
            {
                var covFile = CoverageEnvironment.coverageFile();
                // Check if file exists with its full path    
                if (System.IO.File.Exists(covFile))
                {
                    System.IO.File.Delete(covFile);
                }
            }
            catch (System.IO.IOException)
            {}
            // If file found, delete it    
            
        }
        private void onUpdate(object sender, RoutedEventArgs e)
        {
            readCoverageFile();
        }

        private void ContextMenuMerge(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Feature is not implemented yet !");
        }

        private void ContextMenuDelete(object sender, RoutedEventArgs e)
        {
            CoverageItem item = this.coverageFiles.SelectedItem as CoverageItem;

            try
            {
                if (System.IO.File.Exists(item.RealPath))
                {
                    System.IO.File.Delete(item.RealPath);
                }
            }
            catch(System.IO.IOException)
            {
                MessageBox.Show("Impossible to delete file: "+ item.RealPath);
            }
            
            coverageFiles.Items.Remove(item);
        }

        public class CoverageItem
        {
            public string Name { get; set; }
            public string RealPath { get; set; }
        }

        private void readCoverageFile()
        {
            if (!CoverageEnvironment.hasSolution())
                return;

            var files = System.IO.Directory.GetFiles(CoverageEnvironment.workingCoverageDir, "*.*", System.IO.SearchOption.AllDirectories)
                                           .Where(s => s.EndsWith(".cov"));

            coverageFiles.Items.Clear();
            foreach (var file in files)
            {
                this.coverageFiles.Items.Add(new CoverageItem { Name = System.IO.Path.GetFileNameWithoutExtension(file), RealPath = file });
            }
        }
    }
}