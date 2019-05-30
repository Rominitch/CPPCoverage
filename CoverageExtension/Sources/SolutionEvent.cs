using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace NubiloSoft.CoverageExt.Sources
{
    class SolutionEvent : IVsSolutionEvents
    {
        private EnvDTE80.DTE2 dte;
        private uint       cookie;

        public SolutionEvent(IVsSolution Solution, EnvDTE80.DTE2 ctxDte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            dte = ctxDte;
            Solution.AdviseSolutionEvents(this, out cookie);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            string solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
            Settings.ReadSettings(solutionDir);
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
    }
}