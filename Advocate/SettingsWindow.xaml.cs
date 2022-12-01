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
	///     Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		/// <summary>
		///     Constructor for SettingsWindow, initialises the window and loads settings.
		/// </summary>
		public SettingsWindow()
		{
			InitializeComponent();
			LoadSettings();
		}

		/// <summary>
		///     Holds the path to RePak.exe. (hopefully)
		/// </summary>
		public static string RePakPath
		{
			get { return Properties.Settings.Default.RePakPath; }
			set { Properties.Settings.Default.RePakPath = value; Logging.Logger.Debug($"RePakPath changed to {value}"); }
		}
		/// <summary>
		///     Holds the path to the user's output folder, where we put converted mods.
		/// </summary>
		public static string OutputPath
		{
			get { return Properties.Settings.Default.OutputPath; }
			set { Properties.Settings.Default.OutputPath = value; Logging.Logger.Debug($"OutputPath changed to {value}"); }
		}
		/// <summary>
		///     Holds a non-formatted version of the user's description, used as a template to generate descriptions.
		/// </summary>
		public static string Description
		{
			get { return Properties.Settings.Default.Description; }
			set { Properties.Settings.Default.Description = value; Logging.Logger.Debug($"Description changed to {value}"); }
		}
		/// <summary>
		///     Holds the path to texconv.exe. (hopefully)
		/// </summary>
		public static string TexconvPath
		{
			get { return Properties.Settings.Default.TexconvPath; }
			set { Properties.Settings.Default.TexconvPath = value; Logging.Logger.Debug($"TexconvPath changed to {value}"); }
		}


		/// <summary>
		///     Updates <see cref="RePakPath"/>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void RePakPath_TextBox_TextChanged(object sender, EventArgs e)
		{
			RePakPath = RePakPath_TextBox.Text;
		}

		/// <summary>
		///     Updates <see cref="OutputPath"/>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OutputPath_TextBox_TextChanged(object sender, EventArgs e)
		{
			OutputPath = OutputPath_TextBox.Text;
		}

		/// <summary>
		///     Updates <see cref="TexconvPath"/>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void TexconvPath_TextBox_TextChanged(object sender, EventArgs e)
		{
			TexconvPath = TexconvPath_TextBox.Text;
		}

		/// <summary>
		///     Updates <see cref="Description"/>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Description_TextBox_TextChanged(object sender, EventArgs e)
		{
			Description = Description_TextBox.Text;
		}

		/// <summary>
		///     Loads and initialises settings from user settings.
		/// </summary>
		public void LoadSettings()
		{
			RePakPath_TextBox.Text = RePakPath;
			OutputPath_TextBox.Text = OutputPath;
			Description_TextBox.Text = Description;
			TexconvPath_TextBox.Text = TexconvPath;
		}

		private void SelectRePakPathButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new();
			openFileDialog.Filter = "RePak.exe|*RePak.exe|All Files|*.*";
			if (openFileDialog.ShowDialog() == true)
				RePakPath_TextBox.Text = openFileDialog.FileName;
		}

		private void SelectTexconvPathButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new();
			openFileDialog.Filter = "texconv.exe|*texconv.exe|All Files|*.*";
			if (openFileDialog.ShowDialog() == true)
				TexconvPath_TextBox.Text = openFileDialog.FileName;
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
