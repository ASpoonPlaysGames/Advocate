using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Advocate
{
	/// <summary>
	///     Interaction logic for DescriptionHelp.xaml
	/// </summary>
	public partial class DescriptionHelpWindow : Window
	{
		/// <summary>
		///     Struct used for generating the main content of the window
		/// </summary>
		public struct HelpHint
		{
			/// <summary>
			///     The Key that is to be used when writing a description
			///     <example><code>"{AUTHOR}"</code></example>
			/// </summary>
			public string Key { get; set; }

			/// <summary>
			///     A short sentence describing how the <see cref="Key"/> is to be used when writing a description
			///     <example><code>"The Author Name field"</code></example>
			/// </summary>
			public string Hint { get; set; }
		}

		/// <summary>
		///     A collection of <see cref="HelpHint"/>s, used to generate the content of the <see cref="DescriptionHelpWindow"/>
		/// </summary>
		public ObservableCollection<HelpHint> DescriptionHelpHints { get; private set; }

		/// <summary>
		///     Instantiates a new <see cref="HelpHint"/> and adds it to <see cref="DescriptionHelpHints"/>.
		///     Used to register a new hint for the DescriptionHelpWindow for displaying in UI.
		/// </summary>
		/// <param name="key">The Key that is used when creating the new <see cref="HelpHint"/></param>
		/// <param name="hint">The Hint that is used when creating the new <see cref="HelpHint"/></param>
		public void AddHelpHint(string key, string hint)
		{
			// instantiate a new HelpHint
			HelpHint newHint = new()
			{
				Key = key,
				Hint = hint
			};
			// add the new HelpHint
			AddHelpHint(newHint);
		}

		/// <summary>
		///     Adds an instance of <see cref="HelpHint"/> to <see cref="DescriptionHelpHints"/>
		///     Used to register a new hint for the DescriptionHelpWindow for displaying in UI.
		/// </summary>
		/// <param name="helpHint">The HelpHint to be added</param>
		public void AddHelpHint(HelpHint helpHint)
		{
			// Add the helpHint to the ObservableCollection
			DescriptionHelpHints.Add(helpHint);
		}

		/// <summary>
		///     Constructor for DescriptionHelpWindow, initialises the window and populates help hints
		/// </summary>
		public DescriptionHelpWindow()
		{
			InitializeComponent();

			// instantiate DescriptionHelpHints
			DescriptionHelpHints = new ObservableCollection<HelpHint>();
			
			AddHelpHint("Key:", "Description:"); // todo, replace with a proper header

			// add help hints here - changes made here should be mirrored in DescriptionHandler.cs
			// todo - make this into one struct or something?
			AddHelpHint("{AUTHOR}", "The Author Name field");
			AddHelpHint("{VERSION}", "The Version field");
			AddHelpHint("{SKIN}", "The Skin Name field");
			AddHelpHint("{TYPES}", "The types of skin, separated by '/'  e.g \"CAR/Flatline\"");

			// set the DataContext so that we can get data for bindings
			DataContext = this;
		}
	}
}
