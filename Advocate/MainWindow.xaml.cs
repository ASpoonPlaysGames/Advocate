﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Advocate
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        ///     Handles property changes, updating the UI.
        ///     To update a property that is bound to UI, use <see cref="OnPropertyChanged(string)"/>
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        ///     Updates the UI that has a data binding for the string
        /// </summary>
        /// <param name="propertyName">The name of the data binding to update in UI</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // backing fields, prefer to not use these in code
        private string status = "Loading..."; // this is generally never seen, but exists just in case?
        private string message = "";
        private string skinPath = "";
        private string readMePath = "";
        private string iconPath = "";
        private string authorName = "";
        private string modName = "";
        private string version = "";
        private float convertProgress = 0;

        /// <summary>
        ///     A short status message to the user, indicating the readiness of the converter.
        /// </summary>
        public string Status
        {
            get { return status; }
            private set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        /// <summary>
        ///     A message to the user indicating problems, progress, or readiness of the converter.
        /// </summary>
        public string Message
        {
            get { return message; }
            private set { message = value; OnPropertyChanged(nameof(Message)); }
        }

        /// <summary>
        ///     The file path of the skin.
        /// </summary>
        /// <value>
        ///     A fully qualified file path, leading to a .zip file.
        /// </value>
        public string SkinPath
        {
            get { return skinPath; }
            set { skinPath = value; OnPropertyChanged(nameof(SkinPath)); CheckConvertStatus(); }
        }

        /// <summary>
        ///     The file path of the skin's README.md file.
        /// </summary>
        /// <value>
        ///     A fully qualified file path, leading to a .md file.
        /// </value>
        public string ReadMePath
        {
            get { return readMePath; }
            set { readMePath = value; OnPropertyChanged(nameof(ReadMePath)); CheckConvertStatus(); }
        }

        /// <summary>
        ///     The file path of the skin's icon.png file.
        ///     This field is optional, and a .png file will be generated during <see cref="Conversion.Converter.Convert"/>
        /// </summary>
        /// <value>
        ///     A fully qualified file path, leading to a .png file, or 
        ///     an empty or whitespace string
        /// </value>
        public string IconPath
        {
            get { return iconPath; }
            set { iconPath = value; OnPropertyChanged(nameof(IconPath)); CheckConvertStatus(); }
        }

        /// <summary>
        ///     The name of the Author of the skin.
        /// </summary>
        /// <value>
        ///     A string containing only alphanumeric characters, ' ', and '_'.
        /// </value>
        public string AuthorName
        {
            get { return authorName; }
            set { authorName = value; OnPropertyChanged(nameof(AuthorName)); CheckConvertStatus(); }
        }

        /// <summary>
        ///     The name of the skin/mod.
        /// </summary>
        /// <value>
        ///     A string containing only alphanumeric characters, ' ', and '_'.
        /// </value>
        public string ModName
        {
            get { return modName; }
            set { modName = value; OnPropertyChanged(nameof(ModName)); CheckConvertStatus(); }
        }

        /// <summary>
        ///     The Version of the skin.
        /// </summary>
        /// <value>
        ///     A version string in the format "MAJOR.MINOR.PATCH", where MAJOR, MINOR, and PATCH are numbers.
        /// </value>
        public string Version
        {
            get { return version; }
            set { version = value; OnPropertyChanged(nameof(Version)); CheckConvertStatus(); }
        }

        /// <summary>
        ///     The current progress of the conversion.
        /// </summary>
        /// <value>
        ///     A float between 0 and 100, where 0 represents no progress, and 100 represents completion.
        /// </value>
        public float ConvertProgress
        {
            get { return convertProgress; }
            set { convertProgress = value; OnPropertyChanged(nameof(ConvertProgress)); }
        }
        

        /// <summary>
        ///     An event handler for the <see cref="OnConversionComplete(Conversion.ConversionMessageEventArgs)"/> event.
        /// </summary>
        public event EventHandler<Conversion.ConversionMessageEventArgs> ConversionComplete;
        /// <summary>
        ///     Helper function that creates a new <see cref="Conversion.ConversionMessageEventArgs"/>
        ///     from an input string and calls <see cref="OnConversionComplete(Conversion.ConversionMessageEventArgs)"/>.
        /// </summary>
        /// <param name="message">The message that will be passed to the event listeners.</param>
        protected virtual void OnConversionComplete(string? message) { OnConversionComplete(new Conversion.ConversionMessageEventArgs(message)); }
        /// <summary>
        ///     Event that is called when the conversion is complete.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConversionComplete(Conversion.ConversionMessageEventArgs e)
        {
            ConversionComplete?.Invoke(this, e);
        }

        /// <summary>
        ///     An event handler for the <see cref="OnConversionError(Conversion.ConversionMessageEventArgs)"/> event.
        /// </summary>
        public event EventHandler<Conversion.ConversionMessageEventArgs> ConversionError;
        /// <summary>
        ///     Helper function that creates a new <see cref="Conversion.ConversionMessageEventArgs"/>
        ///     from an input string and calls <see cref="OnConversionError(Conversion.ConversionMessageEventArgs)"/>.
        /// </summary>
        /// <param name="message">The message that will be passed to the event listeners.</param>
        protected virtual void OnConversionError(string? message) { OnConversionError(new Conversion.ConversionMessageEventArgs(message)); }
        /// <summary>
        ///     Event that is called when the conversion fails.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConversionError(Conversion.ConversionMessageEventArgs e)
        {
            ConversionError?.Invoke(this, e);
        }

        /// <summary>
        ///     An event handler for the <see cref="OnConversionMessage(Conversion.ConversionMessageEventArgs)"/> event.
        /// </summary>
        public event EventHandler<Conversion.ConversionMessageEventArgs> ConversionMessage;
        /// <summary>
        ///     Helper function that creates a new <see cref="Conversion.ConversionMessageEventArgs"/>
        ///     from an input string and calls <see cref="OnConversionMessage(Conversion.ConversionMessageEventArgs)"/>.
        /// </summary>
        /// <param name="message">The message that will be passed to the event listeners.</param>
        protected virtual void OnConversionMessage(string? message) { OnConversionMessage(new Conversion.ConversionMessageEventArgs(message)); }
        /// <summary>
        ///     Event that is called on a generic message received from the conversion.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConversionMessage(Conversion.ConversionMessageEventArgs e)
        {
            ConversionMessage?.Invoke(this, e);
        }

        /// <summary>
        ///     An event handler for the <see cref="OnConversionProgressChanged(Conversion.ConversionProgressEventArgs)"/> event.
        /// </summary>
        public event EventHandler<Conversion.ConversionProgressEventArgs> ConversionProgress;
        /// <summary>
        ///     Helper function that creates a new <see cref="Conversion.ConversionProgressEventArgs"/>
        ///     from an input string and calls <see cref="OnConversionProgressChanged(Conversion.ConversionProgressEventArgs)"/>.
        /// </summary>
        /// <param name="message">The message that will be passed to the event listeners.</param>
        protected virtual void OnConversionProgressChanged(float conversionPercent) { OnConversionProgressChanged(new Conversion.ConversionProgressEventArgs(conversionPercent)); }
        /// <summary>
        ///     Event that is called when the progress of the conversion changes.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConversionProgressChanged(Conversion.ConversionProgressEventArgs e)
        {
            ConversionProgress?.Invoke(this, e);
        }

        /// <summary>
        ///     Checks the status of the various settings for the conversion.
        /// </summary>
        /// <returns>true if no issues are found.</returns>
        public bool CheckConvertStatus()
        {
            try
            {
                Status = "Convert Skin(s)";
                // check that RePak path is valid
                if (!File.Exists(Properties.Settings.Default.RePakPath))
                {
                    Message = "Error: RePak path is invalid! (Change in Settings)";
                    return false;
                }
                // i swear to god if people rename RePak.exe and break shit im going to commit war crimes
                if (!Properties.Settings.Default.RePakPath.EndsWith("RePak.exe"))
                {
                    Message = "Error: RePak path does not lead to RePak.exe! (Change in Settings)";
                    return false;
                }
                // check that Output path is valid
                if (!Directory.Exists(Properties.Settings.Default.OutputPath))
                {
                    Message = "Error: Output path is invalid! (Change in Settings)";
                    return false;
                }
                // check that SkinPath is valid and leads to a .zip file
                if (!File.Exists(SkinPath))
                {
                    Message = "Error: Skin path is invalid!";
                    return false;
                }
                if (!SkinPath.EndsWith(".zip"))
                {
                    Message = "Error: Skin path doesn't lead to a .zip file!";
                    return false;
                }
                // check that ReadMePath is valid and leads to a .md file
                if (!string.IsNullOrWhiteSpace(ReadMePath) && !ReadMePath.EndsWith(".md"))
                {
                    Message = "Error: README path doesn't lead to a .md file!";
                    return false;
                }
                // check that IconPath is valid and leads to a .png file
                if (!string.IsNullOrWhiteSpace(IconPath) && !IconPath.EndsWith(".png"))
                {
                    Message = "Error: Icon path doesn't lead to a .png file!";
                    return false;
                }
                // check that AuthorName is valid
                if (AuthorName.Length == 0)
                {
                    Message = "Error: Author Name is required!";
                    return false;
                }
                if (Regex.Match(AuthorName, "[^\\da-zA-Z _]").Success)
                {
                    Message = "Error: Author Name is invalid!";
                    return false;
                }
                // check that ModName is valid
                if (ModName.Length == 0)
                {
                    Message = "Error: Skin Name is required!";
                    return false;
                }
                if (Regex.Match(ModName, "[^\\da-zA-Z _]").Success)
                {
                    Message = "Error: Skin Name is invalid!";
                    return false;
                }
                // check that Version is valid
                if (Version.Length == 0)
                {
                    Message = "Error: Version is required!";
                    return false;
                }
                if (!Regex.Match(Version, "^\\d+.\\d+.\\d+$").Success)
                {
                    Message = "Error: Version is invalid! (Example: 1.0.0)";
                    return false;
                }

                // everything looks good
                Message = "Ready!";
                return true;
            }
            catch (Exception ex)
            {
                // create message box showing the full error
                MessageBoxButton msgButton = MessageBoxButton.OK;
                MessageBoxImage msgIcon = MessageBoxImage.Error;
                MessageBox.Show($"There was an unhandled error during checking!\n\n{ex.Message}\n\n{ex.StackTrace}", "Conversion Checking Error", msgButton, msgIcon);

                // exit out of the conversion
                Message = "Unknown Checking Error!";
                return false;
            }
        }


        /// <summary>
        ///     Constructor for the MainWindow class
        /// </summary>
        /// <param name="path">The path to immediately set the SkinPath to on page load</param>
        public MainWindow(string? path = null)
        {
            InitializeComponent();
            ThemeManager.Current.ApplicationTheme = ThemeManager.GetSystemTheme();
            ThemeManager.Current.AccentColor = ThemeManager.Current.GetAccentColorFromSystem();

            // set the data context for data bindings
            DataContext = this;

            // register event listeners for conversion 
            ConversionProgress += MainWindow_OnConversionProgress;
            ConversionComplete += MainWindow_OnConversionComplete;
            ConversionError += MainWindow_OnConversionError;
            ConversionMessage += MainWindow_OnConversionMessage;

            // if we are given a path, set the SkinPath
            if (path != null)
            {
                SkinPath = path;
            }

            // check the conversion status immediately to initialise the message
            CheckConvertStatus();
        }

        /// <summary>
        ///     Event listener for the <see cref="OnConversionMessage(Conversion.ConversionMessageEventArgs)"/> event.
        ///     Sets <see cref="Message"/> to the <see cref="Conversion.ConversionMessageEventArgs.Message"/> or an empty string if null.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnConversionMessage(object? sender, Conversion.ConversionMessageEventArgs e)
        {
            Message = e.Message ?? "";
        }

        /// <summary>
        ///     Event listener for the <see cref="OnConversionError(Conversion.ConversionMessageEventArgs)"/> event.
        ///     Sets <see cref="Message"/> to the <see cref="Conversion.ConversionMessageEventArgs.Message"/> or an empty string if null.
        ///     Sets <see cref="Status"/> to a failure message, and changes the colour of the conversion button to red.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnConversionError(object? sender, Conversion.ConversionMessageEventArgs e)
        {
            Message = e.Message ?? "";
            Status = "Error!";
            ConvertButton.Dispatcher.Invoke(() => { ConvertButton.SetResourceReference(StyleProperty, "ProgressButtonDanger"); });
        }

        /// <summary>
        ///     Event listener for the <see cref="OnConversionComplete(Conversion.ConversionMessageEventArgs)"/> event.
        ///     Sets <see cref="Message"/> to the <see cref="Conversion.ConversionMessageEventArgs.Message"/> or an empty string if null.
        ///     Sets <see cref="Status"/> to a success message, and changes the colour of the conversion button to green.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnConversionComplete(object? sender, Conversion.ConversionMessageEventArgs e)
        {
            Message = e.Message ?? "";
            Status = "Complete!";
            ConvertButton.Dispatcher.Invoke(() => { ConvertButton.SetResourceReference(StyleProperty, "ProgressButtonPrimary"); });
            ConvertProgress = 0;
        }

        /// <summary>
        ///     Event listener for the <see cref="OnConversionProgress(Conversion.ConversionProgressEventArgs)"/> event.
        ///     Sets <see cref="ConvertProgress"/> to <see cref="Conversion.ConversionProgressEventArgs.ConversionPercent"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnConversionProgress(object? sender, Conversion.ConversionProgressEventArgs e)
        {
            ConvertProgress = e.ConversionPercent;
        }


        ///////////////////////////////////////
        // BUTTON PRESSES AND OTHER UI STUFF //
        ///////////////////////////////////////
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            // instantiate converter
            try
            {
                Conversion.Converter conv = new(SkinPath, AuthorName, ModName, Version, ReadMePath, IconPath);
                Status = "Converting...";
                // event handling, for bubbling up events and stuff
                conv.ConversionMessage += ConversionMessage;
                conv.ConversionError += ConversionError;
                conv.ConversionProgress += ConversionProgress;
                conv.ConversionComplete += ConversionComplete;
                // run conversion in separate thread from the UI
                Task.Run(() => { conv.Convert(); });
            }
            catch (Exception ex)
            {
                OnConversionError(new Conversion.ConversionMessageEventArgs(ex.Message)); 
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new();
            settings.Closing += (object? sender, CancelEventArgs e) => { Properties.Settings.Default.Save(); };
            settings.ShowDialog();
            CheckConvertStatus();
        }

        private void SelectReadMeFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "README.md|*README.md|All Markdown Files|*.md|All Files|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ReadMePath_TextBox.Text = openFileDialog.FileName;
                ReadMePath = openFileDialog.FileName;
            }
        }

        private void SelectSkinFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "ZIP Archives|*.zip|All Files|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                SkinPath_TextBox.Text = openFileDialog.FileName;
                SkinPath = openFileDialog.FileName;
            }
        }

        
        private void SelectIconFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "PNG Files|*.png|All Files|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                IconPath_TextBox.Text = openFileDialog.FileName;
                IconPath = openFileDialog.FileName;
            }
        }

        // these should probably be TwoWay data bindings
        private void ReadMePath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            ReadMePath = ReadMePath_TextBox.Text;
        }

        private void SkinPath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            SkinPath = SkinPath_TextBox.Text;
        }

        private void IconPath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            IconPath = IconPath_TextBox.Text;
        }

        private void Author_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            AuthorName = Author_TextBox.Text;
        }

        private void SkinName_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            ModName = SkinName_TextBox.Text;
        }

        private void Version_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            Version = Version_TextBox.Text;
        }
    }
}