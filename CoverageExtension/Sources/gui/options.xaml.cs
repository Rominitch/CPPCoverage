using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using EnvDTE;
using System.Collections.ObjectModel;
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
            this.HasMaximizeButton = false;
            this.HasMinimizeButton = false;

            InitializeComponent();
        }

        private void OptionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            timeoutLineText.Text = Settings.timeoutCoverage.ToString();
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
        private void OnSave(object sender, MouseButtonEventArgs e)
        {
            Settings.SaveSettings();
        }
    }
}