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

namespace Advocate.Pages
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		private ConverterPage converterPage;

		/// <summary>
		///		Constructor for the MainWindow class
		/// </summary>
		/// <param name="openedFilePath">The file path that Advocate was opened with</param>
		public MainWindow(string? openedFilePath)
		{
			InitializeComponent();

			// initialise our pages
			converterPage = new(openedFilePath);

			// start open to the converter page
			PageFrame.Navigate(converterPage);
		}


	}

}
