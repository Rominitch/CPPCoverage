﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using NubiloSoft.CoverageExt.Data;

namespace NubiloSoft.CoverageExt.Sources.CodeRendering
{
    /// <summary>
    /// State of line
    /// </summary>
    public enum CoverageState : int
    {
        Irrelevant,
        Covered,
        Partially,
        Uncovered
    }

    /// <summary>
    /// CodeTextAdornment places red boxes behind all the "a"s in the editor window
    /// </summary>
    internal sealed class CodeTextAdornment : IDisposable
    {
        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer layer;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private IWpfTextView view;

        internal Brush uncoveredBrush;
        internal Pen   uncoveredPen;
        internal Brush coveredBrush;
        internal Pen   coveredPen;

        private EnvDTE.DTE dte;
        private CoverageState[] currentCoverage;
        private ProfileVector currentProfile;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeTextAdornment"/> class.
        /// </summary>
        /// <param name="view">Text view to create the adornment for</param>
        public CodeTextAdornment(IWpfTextView view, EnvDTE.DTE dte)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            this.dte = dte;
            this.view = view;
            this.layer = view.GetAdornmentLayer("CodeCoverage");
            this.layer.Opacity = 0.4;

            setupEvent(true);

            // make sure the brushes are at least initialized once
            InitializeColors();
        }

        private void setupEvent(bool setup)
        {
            if(setup)
            {
                // listen to events that change the setting properties
                CoverageEnvironment.OnSettingsChanged += slotSettingsChanged;
                CoverageEnvironment.OnReportUpdated   += SlotFinishChanged;

                // Listen to any event that changes the layout (text changes, scrolling, etc)
                view.LayoutChanged += OnLayoutChanged;
            }
            else
            {
                // listen to events that change the setting properties
                CoverageEnvironment.OnSettingsChanged -= slotSettingsChanged;
                CoverageEnvironment.OnReportUpdated   -= SlotFinishChanged;
                view.LayoutChanged                    -= OnLayoutChanged;
            }
        }

        /// <summary>
        /// Acts on change for the settings color(s)
        /// </summary>
        /// <param name="sender"></param>
        private void slotSettingsChanged(object sender, EventArgs e)
        {
            InitializeColors();
            Redraw();
        }

        private void SlotFinishChanged(object sender, EventArgs e)
        {
            CoverageEnvironment.UiInvoke(() => { Redraw(); return true; });
        }

        private void InitializeColors()
        {
            // Color for uncovered code:
            uncoveredBrush = new SolidColorBrush(CoverageEnvironment.UncoveredBrushColor);
            uncoveredBrush.Freeze();
            var penBrush = new SolidColorBrush(CoverageEnvironment.UncoveredPenColor);
            penBrush.Freeze();
            uncoveredPen = new Pen(penBrush, 0.5);
            uncoveredPen.Freeze();

            // Color for covered code:
            coveredBrush = new SolidColorBrush(CoverageEnvironment.CoveredBrushColor);
            coveredBrush.Freeze();
            penBrush = new SolidColorBrush(CoverageEnvironment.CoveredPenColor);
            penBrush.Freeze();
            coveredPen = new Pen(penBrush, 0.5);
            coveredPen.Freeze();
        }

        public string GetActiveFilename()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if(dte.ActiveDocument != null)
                    return dte.ActiveDocument.FullName;
            }
            catch
            {}
            return null;
        }

        /// <summary>
        /// Acts on changes for the settings OnShowCodeCoverage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Redraw()
        {
            try
            {
                if (CoverageEnvironment.ShowCodeCoverage)
                {
                    InitCurrent();

                    foreach (ITextViewLine line in view.TextViewLines)
                    {
                        HighlightCoverage(currentCoverage, currentProfile, line);
                    }
                }
                else
                {
                    layer.RemoveAllAdornments();
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Initializes the current coverage data
        /// </summary>
        private void InitCurrent()
        {
            CoverageState[] currentFile = new CoverageState[0];
            ProfileVector currentProf   = new Data.ProfileVector(0);

            // Check report exists
            if (CoverageEnvironment.ShowCodeCoverage && CoverageEnvironment.report != null)
            {
                string activeFilename = System.IO.Path.GetFullPath(GetActiveFilename());
                if (activeFilename != null)
                {
                    CoverageEnvironment.print("Coverage: Print report on " + activeFilename);

                    Tuple<BitVector, ProfileVector> activeReport = null;
                    DateTime activeFileLastWrite = File.GetLastWriteTimeUtc(activeFilename);

                    if (CoverageEnvironment.report.FileDate >= activeFileLastWrite)
                    {
                        CoverageEnvironment.print("Coverage: Report is up to date");
                        activeReport = CoverageEnvironment.report.GetData(activeFilename);

                        if (activeReport != null)
                        {
                            CoverageEnvironment.print("Coverage: File is inside report");
                            currentProf = activeReport.Item2;
                            currentFile = new CoverageState[activeReport.Item1.Count];

                            foreach (var item in activeReport.Item1.Enumerate())
                            {
                                if (item.Value)
                                {
                                    currentFile[item.Key] = CoverageState.Covered;
                                }
                                else
                                {
                                    currentFile[item.Key] = CoverageState.Uncovered;
                                }
                            }
                        }
                        else
                            CoverageEnvironment.print("Coverage: File "+ activeFilename + " is not inside report");
                    }
                    else
                        CoverageEnvironment.print("Coverage: Report is too old compare to file.");
                }
            }

            this.currentCoverage = currentFile;
            this.currentProfile  = currentProf;
        }

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            InitCurrent();
            
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                HighlightCoverage(currentCoverage, currentProfile, line);
            }
        }

        private CoverageState[] UpdateCoverageData(List<KeyValuePair<int, int>> data)
        {
            List<Tuple<int, int, int>> newdata = new List<Tuple<int, int, int>>();
            foreach (var item in data)
            {
                int line = item.Key;

                // case 1:
                //   10: 0
                //   0xfeefee: 10
                //   --> 10: #=2, cov=1
                //
                // case 2:
                //   10: 0
                //   10: 10 
                //   --> 10: 10.
                //
                // case 3:
                //   10: 0
                //   12: 2
                //   --> 10: 0, 12: 2

                if (newdata.Count > 0 && newdata[newdata.Count - 1].Item1 == line)
                {
                    newdata.Add(new Tuple<int, int, int>(item.Key, 1, item.Value > 0 ? 1 : 0));
                }
                else if (line >= 0xf00000)
                {
                    var last = newdata[newdata.Count - 1];
                    newdata[newdata.Count - 1] = new Tuple<int, int, int>(last.Item1, last.Item2 + 1, last.Item3 + ((item.Value > 0) ? 1 : 0));
                }
                else
                {
                    newdata.Add(new Tuple<int, int, int>(item.Key, 1, item.Value > 0 ? 1 : 0));
                }
            }

            if (newdata.Count == 0)
            {
                newdata.Add(new Tuple<int, int, int>(0, 1, 1));
            }

            newdata.Sort();
            int max = newdata[newdata.Count - 1].Item1 + 1;

            // Initialize everything to 'covered'.
            CoverageState[] lines = new CoverageState[max];
            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = CoverageState.Covered;
            }

            int lastline = 0;
            CoverageState lastState = CoverageState.Covered;
            foreach (var item in newdata)
            {
                for (int i = lastline; i < item.Item1; ++i)
                {
                    lines[i] = lastState;
                }

                lastline = item.Item1;
                lastState = (item.Item3 == 0) ? CoverageState.Uncovered : (item.Item3 == item.Item2) ? CoverageState.Covered : CoverageState.Partially;
            }

            for (int i = lastline; i < lines.Length; ++i)
            {
                lines[i] = lastState;
            }

            return lines;
        }

        private void HighlightCoverage(CoverageState[] coverdata, ProfileVector profiledata, ITextViewLine line)
        {
            if (view == null || profiledata == null || line == null || view.TextSnapshot == null) { return; }

            IWpfTextViewLineCollection textViewLines = view.TextViewLines;

            if (textViewLines == null || line.Extent == null) { return; }

            int lineno = 1 + view.TextSnapshot.GetLineNumberFromPosition(line.Extent.Start);

            CoverageState covered = lineno < coverdata.Length ? coverdata[lineno] : CoverageState.Irrelevant;

            if (covered != CoverageState.Irrelevant)
            {
                SnapshotSpan span = new SnapshotSpan(view.TextSnapshot, Span.FromBounds(line.Start, line.End));
                Geometry g = textViewLines.GetMarkerGeometry(span);

                if (g != null)
                {
                    g = new RectangleGeometry(new Rect(g.Bounds.X, g.Bounds.Y, view.ViewportWidth, g.Bounds.Height));

                    GeometryDrawing drawing = (covered == CoverageState.Covered) ?
                        new GeometryDrawing(coveredBrush, coveredPen, g) :
                        new GeometryDrawing(uncoveredBrush, uncoveredPen, g);

                    drawing.Freeze();

                    DrawingImage drawingImage = new DrawingImage(drawing);
                    drawingImage.Freeze();

                    Image image = new Image();
                    image.Source = drawingImage;

                    //Align the image with the top of the bounds of the text geometry
                    Canvas.SetLeft(image, g.Bounds.Left);
                    Canvas.SetTop(image, g.Bounds.Top);

                    layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
                }
            }

            var profile = profiledata.Get(lineno);
            if (profile != null && profile.Item1 != 0 || profile.Item2 != 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(profile.Item1);
                sb.Append("%/");
                sb.Append(profile.Item2);
                sb.Append("%");

                SnapshotSpan span = new SnapshotSpan(view.TextSnapshot, Span.FromBounds(line.Start, line.End));
                Geometry g = textViewLines.GetMarkerGeometry(span);

                if (g != null) // Yes, this apparently happens...
                {
                    double x = g.Bounds.X + g.Bounds.Width + 20;
                    if (x < view.ViewportWidth / 2) { x = view.ViewportWidth / 2; }
                    g = new RectangleGeometry(new Rect(x, g.Bounds.Y, 30, g.Bounds.Height));

                    Label lbl = new Label();
                    lbl.FontSize = 7;
                    lbl.Foreground = Brushes.Black;
                    lbl.Background = Brushes.Transparent;
                    lbl.FontFamily = new FontFamily("Verdana");
                    lbl.Content = sb.ToString();

                    Canvas.SetLeft(lbl, g.Bounds.Left);
                    Canvas.SetTop(lbl, g.Bounds.Top);

                    layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, lbl, null);
                }
            }
        }

        public void Close()
        {
            if (view != null)
            {
                setupEvent(false);
                view = null;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
