﻿using EnvDTE;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace NubiloSoft.CoverageExt.Sources.CodeRendering
{
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CodeTextAdornmentTextViewCreationListener : IWpfTextViewCreationListener
    {
        // Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169

        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered
        /// after the selection layer in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("CodeCoverage")]
        [Order(After = PredefinedAdornmentLayers.BraceCompletion, Before = PredefinedAdornmentLayers.Selection)]
        private AdornmentLayerDefinition editorAdornmentLayer;

#pragma warning restore 649, 169

        [Import]
        public SVsServiceProvider ServiceProvider = null;

        private CodeTextAdornment coverage;

        #region IWpfTextViewCreationListener

        /// <summary>
        /// Called when a text view having matching roles is created over a text data model having a matching content type.
        /// Instantiates a CodeTextAdornment manager when the textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));

            // Store this thing somewhere so our GC doesn't incidentally destroy it.
            this.coverage = new CodeTextAdornment(textView, dte);
            // The adornment will listen to any event that changes the layout (text changes, scrolling, etc)
            //new CodeTextAdornment(textView);
            textView.Closed += (object sender, System.EventArgs e) =>
            {
                this.coverage.Close();
            };
            
        }

        #endregion
    }
}
