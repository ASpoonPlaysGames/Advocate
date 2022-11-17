using System;
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
        ///     This field is optional, and a .png file will be generated during <see cref="Conversion.Converter.Convert(string, string, string, bool)"/>
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

            // register event listener for conversion messages
            Logging.Logger.LogReceived += HandleConversionMessage;

            // if we are given a path, set the SkinPath
            if (path != null)
            {
                SkinPath = path;
            }

            // check the conversion status immediately to initialise the message
            CheckConvertStatus();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            // register event listener for conversion messages
            Logging.Logger.LogReceived -= HandleConversionMessage;
        }

        /// <summary>
        ///     <para>Event listener for the <see cref="Logging.Logger.LogReceived"/> event.</para>
        ///     <para>Sets <see cref="Message"/> to the <see cref="Conversion.ConversionMessageEventArgs.Message"/> or an empty string if null.</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleConversionMessage(object? sender, Logging.LoggingEventArgs e)
        {
            // ignore messages that are below MessageType.Info in gui
            if (e.Type < Logging.MessageType.Info)
                return;

            // update the ConvertProgress
            ConvertProgress = e.Type switch
            {
                // error is a special case where we want to set the conversion progress to 0 no matter what
                Logging.MessageType.Error => 0,
                // default to just using the percentage we were given, or not changing it at all if we are given null
                _ => e.ConversionPercent ?? ConvertProgress
            };

            // Update the conversion message shown to the user, if null then just use an empty string
            Message = e.Message ?? "";

            // Update the status message inside the button
            Status = e.Type switch
            {
                Logging.MessageType.Completion => "Complete!",
                Logging.MessageType.Error => "Error!",
                // default to just not changing it
                _ => Status
            };

            // determine which style we should set the ConvertButton to
            string style = e.Type switch
            {
                // this is like a light green
                Logging.MessageType.Completion => "ProgressButtonSuccess",
                // this is a red
                Logging.MessageType.Error => "ProgressButtonDanger",
                // this is the user's system accent colour
                _ => "ProgressButtonPrimary"
            };

            // update the ConvertButton's style
            ConvertButton.Dispatcher.Invoke(() =>
            {
                ConvertButton.SetResourceReference(StyleProperty, style);
            });
        }

        ///////////////////////////////////////
        // BUTTON PRESSES AND OTHER UI STUFF //
        ///////////////////////////////////////
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // instantiate converter
                Conversion.Converter conv = new(SkinPath, AuthorName, ModName, Version, ReadMePath, IconPath);

                // set the status
                Status = "Converting...";
                // reset the conversion progress
                ConvertProgress = 0;
                // reset the button style
                ConvertButton.SetResourceReference(StyleProperty, "ProgressButtonPrimary");
                // lock the button to try prevent multiple conversion threads running at the same time
                ConvertButton.IsEnabled = false;

                // run conversion in separate thread from the UI
                Task.Run(() =>
                {
                    conv.Convert(false);
                    ConvertButton.Dispatcher.Invoke(() =>
                    {
                        // allow the button to be pressed again once conversion is complete
                        ConvertButton.IsChecked = false;
                        ConvertButton.IsEnabled = true;
                    });
                });
            }
            catch (Exception ex)
            {
                Logging.Logger.Error(ex.Message);
                // allow the button to be pressed again if conversion fails
                ConvertButton.IsChecked = false;
                ConvertButton.IsEnabled = true;
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
            Logging.Logger.Debug($"ReadMePath changed to '{ReadMePath}'");
        }

        private void SkinPath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            SkinPath = SkinPath_TextBox.Text;
            Logging.Logger.Debug($"SkinPath changed to '{SkinPath}'");
        }

        private void IconPath_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            IconPath = IconPath_TextBox.Text;
            Logging.Logger.Debug($"IconPath changed to '{IconPath}'");
        }

        private void Author_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            AuthorName = Author_TextBox.Text;
            Logging.Logger.Debug($"AuthorName changed to '{AuthorName}'");
        }

        private void SkinName_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            ModName = SkinName_TextBox.Text;
            Logging.Logger.Debug($"ModName changed to '{ModName}'");
        }

        private void Version_TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            Version = Version_TextBox.Text;
            Logging.Logger.Debug($"Version changed to '{Version}'");
        }
    }
}
