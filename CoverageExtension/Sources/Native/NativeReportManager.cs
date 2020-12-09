using System;
using System.IO;
using EnvDTE;
using NubiloSoft.CoverageExt.Data;

namespace NubiloSoft.CoverageExt.Native
{
    public class NativeReportManager : Data.IReportManager
    {
        public NativeReportManager() : base()
        {
            this.activeCoverageReport = null;
            this.activeCoverageFilename = null;

            // Use event
            CoverageEnvironment.OnReportUpdated += SlotFinishChanged;
        }

        private Data.ICoverageData activeCoverageReport;
        private string activeCoverageFilename;

        private object lockObject = new object();

        private void SlotFinishChanged(object sender, EventArgs e)
        {
            CoverageEnvironment.UiInvoke(() => { ResetData(); return true; });
        }

        public override ICoverageData UpdateData()
        {
            // It makes no sense to have multiple instances of our coverage data in our memory, so
            // this is exposed as a singleton. Updating needs concurrency control. It's pretty fast, so 
            // a simple lock will do.
            //
            // We update as all-or-nothing, and use the result from UpdateData. This means no other concurrency
            // control is required; there are no conflicts.
            lock (lockObject)
            {
                return UpdateDataImpl();
            }
        }

        public override void ResetData()
        {
            lock (lockObject)
            {
                this.activeCoverageReport = null;
            }
        }

        private ICoverageData UpdateDataImpl()
        {
            try
            {
                var coverageFile = CoverageEnvironment.coverageFile();

                if (activeCoverageFilename != coverageFile)
                {
                    activeCoverageFilename = coverageFile;
                    activeCoverageReport = null;
                }

                if (File.Exists(coverageFile))
                {
                    if (activeCoverageReport != null)
                    {
                        var lastWT = new FileInfo(coverageFile).LastWriteTimeUtc;
                        if (lastWT > activeCoverageReport.FileDate)
                        {
                            activeCoverageReport = null;
                        }
                    }

                    if (activeCoverageReport == null)
                    {
                        CoverageEnvironment.console.WriteLine("Updating coverage results from: {0}", coverageFile);
                        activeCoverageReport = Load(coverageFile);
                        activeCoverageFilename = coverageFile;
                    }
                }
            }
            catch { }

            return activeCoverageReport;
        }

        private ICoverageData Load(string filename)
        {
            ICoverageData report = null;
            if (filename != null)
            {
                try
                {
                    report = new Native.NativeData(filename);
                }
                catch (Exception e)
                {
                    CoverageEnvironment.console.WriteLine("Error loading coverage report: {0}", e.Message);
                }
            }
            return report;
        }
    }
}
