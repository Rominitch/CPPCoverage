using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

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

            Debug.WriteLine("Start with solution");

            try
            {
                // Release all GUI
                //->> ToDo !

                // Reload data
                string solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);

                // Configure settings
                CoverageEnvironment.configureSolution(solutionDir);
            }
            catch(Exception exp)
            {
                Debug.WriteLine("Error on loading solution: "+ exp.ToString());
            }
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
            CoverageEnvironment.configureSolution(string.Empty);
            return VSConstants.S_OK;
        }
    }
}