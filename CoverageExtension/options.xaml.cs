using System;
using System.Windows;
using System.Windows.Input;
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
 
        private void addExclude(object sender, RoutedEventArgs e)
        {
            ExcludesList.Items.Add(ExcludeName.Text);
        }

        private void deleteExclude(object sender, RoutedEventArgs e)
        {
            if(ExcludesList.SelectedItem != null)
            {
                ExcludesList.Items.RemoveAt(ExcludesList.Items.IndexOf(ExcludesList.SelectedItem));
            }
        }

        private void onSave(object sender, RoutedEventArgs e)
        {
            Settings.timeoutCoverage = Int32.Parse(timeoutLineText.Text);

            if (Settings.exclude == null)
                Settings.exclude = new System.Collections.ArrayList();

            Settings.exclude.Clear();
            foreach(var item in ExcludesList.Items)
            {
                Settings.exclude.Add(item.ToString());
            }

            Settings.SaveSettings();

            Close();
        }
    }
}