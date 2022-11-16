using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HandyControl.Themes;
using Microsoft.Win32;

namespace Advocate
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        public static string RePakPath
        {
            get { return Properties.Settings.Default.RePakPath; }
            set { Properties.Settings.Default.RePakPath = value; }
        }

        public static string OutputPath
        {
            get { return Properties.Settings.Default.OutputPath; }
            set { Properties.Settings.Default.OutputPath = value; }
        }

        public static string Description
        {
            get { return Properties.Settings.Default.Description; }
            set { Properties.Settings.Default.Description = value; }
        }

        public void RePakPath_TextBox_TextChanged(object sender, EventArgs e)
        {
            RePakPath = RePakPath_TextBox.Text;
        }

        public void OutputPath_TextBox_TextChanged(object sender, EventArgs e)
        {
            OutputPath = OutputPath_TextBox.Text;
        }

        public void Description_TextBox_TextChanged(object sender, EventArgs e)
        {
            Description = Description_TextBox.Text;
        }

        public void LoadSettings()
        {
            RePakPath_TextBox.Text = RePakPath;
            OutputPath_TextBox.Text = OutputPath;
            Description_TextBox.Text = Description;
        }

        private void SelectRePakPathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "RePak.exe|*RePak.exe|All Files|*.*";
            if (openFileDialog.ShowDialog() == true)
                RePakPath_TextBox.Text = openFileDialog.FileName;
        }

        private void SelectOutputPathButton_Click(object sender, RoutedEventArgs e)
        {
            // gotta love installing a package for 1 (one) usage
            var folderDlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (folderDlg.ShowDialog() == true)
                OutputPath_TextBox.Text = folderDlg.SelectedPath;
        }

        private void DescriptionHelpButton_Click(object sender, RoutedEventArgs e)
        {
            // open the help page for description formatting
            DescriptionHelpWindow descriptionHelp = new();
            descriptionHelp.Show();
        }
    }
}
