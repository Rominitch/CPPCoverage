namespace NubiloSoft.CoverageExt.Data
{
    public abstract class IReportManager
    {
        public IReportManager()
        {
            CoverageEnvironment.OnFinishCoverage += SlotUpdateReport;
        }

        public abstract ICoverageData UpdateData();
        public abstract void ResetData();

        public void UpdateReport()
        {
            CoverageEnvironment.print("Coverage: Report has changed -> reading");

            CoverageEnvironment.report = UpdateData();

            CoverageEnvironment.print("Coverage: Report is ready into CoverageEnvironment");

            CoverageEnvironment.emitReportUpdated();
        }

        private void SlotUpdateReport(object sender, System.EventArgs e)
        {
            UpdateReport();
        }
    }
}
