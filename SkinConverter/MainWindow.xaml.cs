using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HandyControl.Themes;
using Microsoft.Win32;

namespace SkinConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // this is the class that actually does the converting of the skin(s)
        private Converter conv = new();
        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Current.ApplicationTheme = ThemeManager.GetSystemTheme();
            ThemeManager.Current.AccentColor = ThemeManager.Current.GetAccentColorFromSystem();

            DataContext = conv;

            conv.CheckConvertStatus();
        }


        ///////////////////////////////////////
        // BUTTON PRESSES AND OTHER UI STUFF //
        ///////////////////////////////////////
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {            
            conv.Convert(ConvertButton, StyleProperty);
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new();
            settings.Closing += (object sender, CancelEventArgs e) => { Properties.Settings.Default.Save(); };
            settings.ShowDialog();
            conv.CheckConvertStatus();
        }

        private void SelectReadMeFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "README.md|*README.md|All Markdown Files|*.md|All Files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                ReadMePath_TextBox.Text = openFileDialog.FileName;
                conv.ReadMePath = openFileDialog.FileName;
            }
        }

        private void SelectSkinFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "ZIP Archives|*.zip|All Files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                SkinPath_TextBox.Text = openFileDialog.FileName;
                conv.SkinPath = openFileDialog.FileName;
            }
        }

        
        private void SelectIconFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "PNG Files|*.png|All Files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                IconPath_TextBox.Text = openFileDialog.FileName;
                conv.IconPath = openFileDialog.FileName;
            }
        }
        private void ReadMePath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            conv.ReadMePath = ReadMePath_TextBox.Text;
        }

        private void SkinPath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            conv.SkinPath = SkinPath_TextBox.Text;
        }

        private void IconPath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            conv.IconPath = IconPath_TextBox.Text;
        }

        private void Author_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            conv.AuthorName = Author_TextBox.Text;
        }

        private void SkinName_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            conv.ModName = SkinName_TextBox.Text;
        }

        private void Version_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            conv.Version = Version_TextBox.Text;
        }
    }
}
