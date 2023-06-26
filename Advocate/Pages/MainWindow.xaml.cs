using System.Windows;

namespace Advocate.Pages
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		private readonly Converter.ConverterPage converterPage;
		private readonly NoseArtCreator.NoseArtCreatorPage noseArtCreatorPage;

		/// <summary>
		///		Constructor for the MainWindow class
		/// </summary>
		/// <param name="openedFilePath">The file path that Advocate was opened with</param>
		public MainWindow(string? openedFilePath)
		{
			InitializeComponent();

			// initialise our pages
			// note: this is done here instead of in-editor so that i can pass arguments to the page constructors
			converterPage = new(openedFilePath);
			SkinConverterFrame.Navigate(converterPage);

			noseArtCreatorPage = new(openedFilePath);
			NoseArtCreatorFrame.Navigate(noseArtCreatorPage);
		}
	}
}
