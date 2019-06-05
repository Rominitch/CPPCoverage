using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace NubiloSoft.CoverageExt
{
    /// <summary>
    /// All event feature of options.
    /// </summary>
    public partial class Options : DialogWindow
    {
        internal Options()
        {
            InitializeComponent();
        }

        private void OptionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            timeoutLineText.Text = Settings.timeoutCoverage.ToString();
            SharableCB.IsChecked = Settings.isSharable;

            if (Settings.exclude != null)
            {
                foreach (var item in Settings.exclude)
                {
                    ExcludesList.Items.Add(item);
                }
            }
        }

        private void OptionsWindows_GotFocus(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionsWindows_LayoutUpdated(object sender, EventArgs e)
        {
        }

        private void OptionsWindows_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            
        }
 
        private void AddExclude(object sender, RoutedEventArgs e)
        {
            ExcludesList.Items.Add(ExcludeName.Text);
        }

        private void DeleteExclude(object sender, RoutedEventArgs e)
        {
            if(ExcludesList.SelectedItem != null)
            {
                ExcludesList.Items.RemoveAt(ExcludesList.Items.IndexOf(ExcludesList.SelectedItem));
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.timeoutCoverage = Int32.Parse(timeoutLineText.Text);
                Settings.isSharable = SharableCB.IsChecked ?? true;

                if (Settings.exclude == null)
                    Settings.exclude = new System.Collections.ArrayList();

                Settings.exclude.Clear();
                foreach (var item in ExcludesList.Items)
                {
                    Settings.exclude.Add(item.ToString());
                }

                Settings.SaveSettings();

                Close();
            }
            catch(Exception exp)
            {
                Debug.WriteLine("The product name is " + exp.ToString());
            }
        }
    }
}