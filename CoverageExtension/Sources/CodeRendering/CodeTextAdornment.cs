using System;
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
    internal sealed class CodeTextAdornment
    {
        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer layer;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private readonly IWpfTextView view;

        /// <summary>
        /// Adornment brush.
        /// </summary>
        private readonly Brush brush;

        /// <summary>
        /// Adornment pen.
        /// </summary>
        private readonly Pen pen;

        private readonly Brush uncoveredBrush;
        private readonly Pen   uncoveredPen;
        private readonly Brush coveredBrush;
        private readonly Pen   coveredPen;

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

            this.layer = view.GetAdornmentLayer("CodeTextAdornment");

            this.dte  = dte;
            this.view = view;
            this.view.LayoutChanged += this.OnLayoutChanged;

            // Create the pen and brush to color the box behind the a's
            /*
            this.brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0xff));
            this.brush.Freeze();

            var penBrush = new SolidColorBrush(Colors.Red);
            penBrush.Freeze();
            this.pen = new Pen(penBrush, 0.5);
            this.pen.Freeze();
            */

            // Color for uncovered code:
            uncoveredBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xCF, 0xB8));
            uncoveredBrush.Freeze();
            var penBrush = new SolidColorBrush(Color.FromArgb(0xD0, 0xFF, 0xCF, 0xB8));
            penBrush.Freeze();
            uncoveredPen = new Pen(penBrush, 0.5);
            uncoveredPen.Freeze();

            // Color for covered code:
            coveredBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xBD, 0xFC, 0xBF));
            coveredBrush.Freeze();
            penBrush = new SolidColorBrush(Color.FromArgb(0xD0, 0xBD, 0xFC, 0xBF));
            penBrush.Freeze();
            coveredPen = new Pen(penBrush, 0.5);
            coveredPen.Freeze();
        }

        public string GetActiveFilename()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                return dte.ActiveDocument.FullName;
            }
            catch
            {
                return null;
            }
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
            CoverageState[] currentFile = new CoverageState[0];
            ProfileVector currentProf = new Data.ProfileVector(0);

            string activeFilename = GetActiveFilename();
            if (activeFilename != null)
            {
                Tuple<BitVector, ProfileVector> activeReport = null;
                DateTime activeFileLastWrite = File.GetLastWriteTimeUtc(activeFilename);

                var dataProvider = Data.ReportManagerSingleton.Instance(dte);
                if (dataProvider != null)
                {
                    var coverageData = dataProvider.UpdateData();
                    if (coverageData != null && activeFilename != null)
                    {
                        if (coverageData.FileDate >= activeFileLastWrite)
                        {
                            activeReport = coverageData.GetData(activeFilename);
                        }
                    }
                }

                if (activeReport != null)
                {
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
            }

            this.currentCoverage = currentFile;
            this.currentProfile = currentProf;

            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                HighlightCoverage(currentCoverage, currentProfile, line);
            }
            //             foreach (ITextViewLine line in e.NewOrReformattedLines)
            //             {
            //                 this.CreateVisuals(line);
            //             }
        }
        

        /// <summary>
        /// Adds the scarlet box behind the 'a' characters within the given line
        /// </summary>
        /// <param name="line">Line to add the adornments</param>
        /*
        private void CreateVisuals(ITextViewLine line)
        {
            IWpfTextViewLineCollection textViewLines = this.view.TextViewLines;

            // Loop through each character, and place a box around any 'a'
            for (int charIndex = line.Start; charIndex < line.End; charIndex++)
            {
                if (this.view.TextSnapshot[charIndex] == 'a')
                {
                    SnapshotSpan span = new SnapshotSpan(this.view.TextSnapshot, Span.FromBounds(charIndex, charIndex + 1));
                    Geometry geometry = textViewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var drawing = new GeometryDrawing(this.brush, this.pen, geometry);
                        drawing.Freeze();

                        var drawingImage = new DrawingImage(drawing);
                        drawingImage.Freeze();

                        var image = new Image
                        {
                            Source = drawingImage,
                        };

                        // Align the image with the top of the bounds of the text geometry
                        Canvas.SetLeft(image, geometry.Bounds.Left);
                        Canvas.SetTop(image, geometry.Bounds.Top);

                        this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
                    }
                }
            }
        }
        */
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
    }
}
