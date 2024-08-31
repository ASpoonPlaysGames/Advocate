using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Advocate.Logging;
using Advocate.Models.JSON;
using HandyControl.Controls;
using Microsoft.Win32;

namespace Advocate.Pages.NoseArtCreator
{
	/// <summary>
	/// Interaction logic for NoseArtCreatorPage.xaml
	/// </summary>
	public partial class NoseArtCreatorPage : Page, INotifyPropertyChanged
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


		private Dictionary<string, ObservableCollection<NoseArt>> noseArts = new();

		public ObservableCollection<string> noseArtNames { get; private set; } = new();
		public ObservableCollection<string> chassisTypes { get; private set; } = new();

		private NoseArt selectedNoseArt;

		private readonly Assembly assembly;

		// variables for actual nose art creation
		private string iconPath = "";
		public string IconPath
		{
			get { return iconPath; }
			set { iconPath = value; OnPropertyChanged(nameof(IconPath)); CheckStatus(); }
		}

		private string readMePath = "";
		public string ReadMePath
		{
			get { return readMePath; }
			set { readMePath = value; OnPropertyChanged(nameof(ReadMePath)); CheckStatus(); }
		}

		private string authorName = "";
		public string AuthorName
		{
			get { return authorName; }
			set { authorName = value; OnPropertyChanged(nameof(AuthorName)); CheckStatus(); }
		}

		private string modName = "";
		public string ModName
		{
			get { return modName; }
			set { modName = value; OnPropertyChanged(nameof(ModName)); CheckStatus(); }
		}

		private string version = "1.0.0";
		public string Version
		{
			get { return version; }
			set { version = value; OnPropertyChanged(nameof(Version)); CheckStatus(); }
		}

		/// <summary>
		///		Constructor for NoseArtCreatorPage.
		/// </summary>
		/// <param name="openedFilePath">The file path that Advocate was opened with</param>
		public NoseArtCreatorPage(string? openedFilePath)
		{
			InitializeComponent();

			DataContext = this;

			// Add event listener for the nose art selection combo boxes
			ChassisList.SelectionChanged += ChassisList_SelectionChanged;
			NamesList.SelectionChanged += NamesList_SelectionChanged;

			// add event listener for diplaying messages to the user
			Logger.LogReceived += OnLogMessageReceived;

			// read the json that controls nose arts
			assembly = Assembly.GetExecutingAssembly();
			string str;
			using (Stream stream = assembly.GetManifestResourceStream("Advocate.Resource.NoseArts.nose-arts.json") ?? throw new Exception("Failed to find nose art json! This is probably a bug"))
			using (StreamReader reader = new(stream))
			{
				str = reader.ReadToEnd();
			}
			// parse the json
			NoseArt[] arts = JsonSerializer.Deserialize<NoseArt[]>(str) ?? throw new Exception("Failed to parse nose art json! This is probably a bug");

			if (arts.Length == 0)
			{
				throw new Exception("No nose arts found?");
			}

			// add the nose arts to the dictionary
			foreach (NoseArt art in arts)
			{
				if (noseArts.ContainsKey(art.Chassis))
				{
					noseArts[art.Chassis].Add(art);
				}
				else
				{
					noseArts.Add(art.Chassis, new() { art });
					chassisTypes.Add(art.Chassis);
				}
			}

			if (chassisTypes.Count == 0)
			{
				throw new Exception("No nose art chassis found?");
			}

			ChassisList.SelectedIndex = 0;
			NamesList.SelectedIndex = 0;

			CheckStatus();
		}

		private bool CheckStatus()
		{
			try
			{
				StatusButton.Content = "Create Nose Art";
				// check that RePak path is valid
				if (!File.Exists(Properties.Settings.Default.RePakPath))
				{
					StatusMessage.Content = "Error: RePak path is invalid! (Change in Settings)";
					return false;
				}
				// i swear to god if people rename RePak.exe and break shit im going to commit war crimes
				if (!Properties.Settings.Default.RePakPath.EndsWith("RePak.exe"))
				{
					StatusMessage.Content = "Error: RePak path does not lead to RePak.exe! (Change in Settings)";
					return false;
				}
				// check that texconv path is valid
				if (!File.Exists(Properties.Settings.Default.TexconvPath))
				{
					StatusMessage.Content = "Error: texconv path is invalid! (Change in Settings)";
					return false;
				}
				// i swear to god if people rename texconv.exe and break shit im going to commit war crimes
				if (!Properties.Settings.Default.TexconvPath.EndsWith("texconv.exe"))
				{
					StatusMessage.Content = "Error: texconv path does not lead to texconv.exe! (Change in Settings)";
					return false;
				}
				// check that Output path is valid
				if (!Directory.Exists(Properties.Settings.Default.OutputPath))
				{
					StatusMessage.Content = "Error: Output path is invalid! (Change in Settings)";
					return false;
				}
				// check that ReadMePath is valid and leads to a .md file
				if (!string.IsNullOrWhiteSpace(ReadMePath) && !ReadMePath.EndsWith(".md"))
				{
					StatusMessage.Content = "Error: README path doesn't lead to a .md file!";
					return false;
				}
				// check that IconPath is valid and leads to a .png file
				if (!string.IsNullOrWhiteSpace(IconPath) && !IconPath.EndsWith(".png"))
				{
					StatusMessage.Content = "Error: Icon path doesn't lead to a .png file!";
					return false;
				}
				// check that AuthorName is valid
				if (AuthorName.Length == 0)
				{
					StatusMessage.Content = "Error: Author Name is required!";
					return false;
				}
				if (Regex.Match(AuthorName, "[^\\da-zA-Z _]").Success)
				{
					StatusMessage.Content = "Error: Author Name is invalid!";
					return false;
				}
				// check that ModName is valid
				if (ModName.Length == 0)
				{
					StatusMessage.Content = "Error: Name is required!";
					return false;
				}
				if (Regex.Match(ModName, "[^\\da-zA-Z _]").Success)
				{
					StatusMessage.Content = "Error: Name is invalid!";
					return false;
				}
				// check that Version is valid
				if (Version.Length == 0)
				{
					StatusMessage.Content = "Error: Version is required!";
					return false;
				}
				if (!Regex.Match(Version, "^\\d+\\.\\d+\\.\\d+$").Success)
				{
					StatusMessage.Content = "Error: Version is invalid! (Example: 1.0.0)";
					return false;
				}

				// everything looks good
				StatusMessage.Content = "Ready!";
				return true;
			}
			catch (Exception ex)
			{
				// create message box showing the full error
				MessageBoxButton msgButton = MessageBoxButton.OK;
				MessageBoxImage msgIcon = MessageBoxImage.Error;
				System.Windows.MessageBox.Show($"There was an unhandled error during checking!\n\n{ex.Message}\n\n{ex.StackTrace}", "Nose Art Creation Checking Error", msgButton, msgIcon);

				// exit out of the conversion
				StatusMessage.Content = "Unknown Checking Error!";
				return false;
			}
		}

		private void OnLogMessageReceived(object? sender, LogMessageEventArgs e)
		{
			// ignore messages that are below MessageType.Info in gui
			if (e.Type < MessageType.Info)
				return;

			StatusButton.Dispatcher.Invoke(() =>
			{
				StatusMessage.Content = e.Message;
				if (e.Progress != null)
				{
					StatusButton.Progress = (double)e.Progress;
				}

				StatusButton.Content = e.Type switch
				{
					MessageType.Completion => "Complete!",
					MessageType.Error => "Error!",
					// default to just not changing it
					_ => StatusButton.Content
				};

				string style = e.Type switch
				{
					// this is like a light green
					MessageType.Completion => "ProgressButtonSuccess",
					// this is a red
					MessageType.Error => "ProgressButtonDanger",
					// this is the user's system accent colour
					_ => "ProgressButtonPrimary"
				};

				// update the StatusButton's style
				StatusButton.SetResourceReference(StyleProperty, style);
			});
		}

		private void UpdatePreviewImage()
		{
			if (!selectedNoseArt.Textures.Contains("col"))
			{
				throw new Exception("Cannot make a preview without a col map");
			}

			ImagePreview.Dispatcher.Invoke(() =>
			{
				ImagePreview.Visibility = Visibility.Hidden;
			});

			Uri colUri = ImageSelector_col.Dispatcher.Invoke(() => { return ImageSelector_col.Uri; });

			if (!selectedNoseArt.Textures.Contains("opa"))
			{
				BitmapSource image = new BitmapImage(colUri);

				if (!IsValidNoseArtDimensions(image.PixelWidth, image.PixelHeight))
				{
					// scale the image
					image = new TransformedBitmap(image, new ScaleTransform(selectedNoseArt.Width / image.PixelWidth, selectedNoseArt.Height / image.PixelHeight));
				}

				ImagePreview.Dispatcher.Invoke(() =>
				{
					ImagePreview.Source = image;
				});

				return;
			}

			Bitmap imageBmp;
			if (colUri.IsFile)
			{
				if (colUri.LocalPath.EndsWith(".png"))
				{
					FileStream stream = new(colUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					imageBmp = new(stream);
					stream.Close();
				}
				else if (colUri.LocalPath.EndsWith(".dds"))
				{
					FileStream stream = new(colUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					MemoryStream mem = new MemoryStream();
					Scripts.DDS.Manager.DdsToPng(stream, mem, selectedNoseArt.Width, selectedNoseArt.Height);
					imageBmp = new(mem);
					stream.Close();
				}
				else
				{
					throw new NotImplementedException("Failed to load texture, invalid extension");
				}
			}
			else
			{
				imageBmp = new(Application.GetResourceStream(colUri).Stream);
			}

			if (!IsValidNoseArtDimensions(imageBmp.Width, imageBmp.Height))
			{
				imageBmp = new(imageBmp, new(selectedNoseArt.Width, selectedNoseArt.Height));
			}

			Uri maskUri = ImageSelector_opa.Dispatcher.Invoke(() => { return ImageSelector_opa.Uri; });
			Bitmap maskBmp;
			if (maskUri.IsFile)
			{
				if (maskUri.LocalPath.EndsWith(".png"))
				{
					FileStream stream = new(maskUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					maskBmp = new(stream);
					stream.Close();
				}
				else if (maskUri.LocalPath.EndsWith(".dds"))
				{
					FileStream stream = new(maskUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					MemoryStream mem = new MemoryStream();
					Scripts.DDS.Manager.DdsToPng(stream, mem, selectedNoseArt.Width, selectedNoseArt.Height);
					maskBmp = new(mem);
					stream.Close();
				}
				else
				{
					throw new NotImplementedException("Failed to load texture, invalid extension");
				}
			}
			else
			{
				maskBmp = new(Application.GetResourceStream(maskUri).Stream);
			}

			if (!IsValidNoseArtDimensions(maskBmp.Width, maskBmp.Height))
			{
				maskBmp = new(maskBmp, new(selectedNoseArt.Width, selectedNoseArt.Height));
			}

			imageBmp = ApplyMask(imageBmp, maskBmp);


			ImagePreview.Dispatcher.Invoke(() =>
			{
				ImagePreview.Source = ToBitmapImage(imageBmp);
				ImagePreview.Visibility = Visibility.Visible;
			});
		}


		// this is rather slow, too bad!
		private static Bitmap ApplyMask(Bitmap image, Bitmap mask)
		{
			if (image.Width != mask.Width)
				throw new Exception("Image width and mask width does not match");

			if (image.Height != mask.Height)
				throw new Exception("Image width and mask height does not match");

			Bitmap ret = new(image.Width, image.Height);

			for (int x = 0; x < image.Width; x++)
			{
				for (int y = 0; y < image.Height; y++)
				{
					System.Drawing.Color col = image.GetPixel(x, y);
					System.Drawing.Color maskCol = mask.GetPixel(x, y);

					// literally just average the R, G, and B
					int maskAlpha = ((maskCol.R + maskCol.G + maskCol.B) / 3);

					ret.SetPixel(x, y, System.Drawing.Color.FromArgb(maskAlpha, col));
				}
			}

			return ret;
		}

		// disgusting, i hate it here
		private static BitmapImage ToBitmapImage(Bitmap bitmap)
		{
			using (var memory = new MemoryStream())
			{
				bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
				memory.Position = 0;

				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memory;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();

				return bitmapImage;
			}
		}

		private void ResetImageSelection(string imageType)
		{
			ImageSelector imageSelector = imageType switch
			{
				"col" => ImageSelector_col,
				"spc" => ImageSelector_spc,
				"gls" => ImageSelector_gls,
				"opa" => ImageSelector_opa,
				_ => throw new NotImplementedException("Invalid imageType"),
			};

			Button button = imageType switch
			{
				"col" => DownloadButton_col,
				"spc" => DownloadButton_spc,
				"gls" => DownloadButton_gls,
				"opa" => DownloadButton_opa,
				_ => throw new NotImplementedException("Invalid imageType"),
			};

			if (!selectedNoseArt.Textures.Contains(imageType))
			{
				imageSelector.Visibility = Visibility.Hidden;
				button.Visibility = Visibility.Hidden;
				return;
			}
			else
			{
				imageSelector.Visibility = Visibility.Visible;
				button.Visibility = Visibility.Visible;
			}

			Uri uri = new($"pack://application:,,,/{assembly.GetName().Name};component/Resource/{selectedNoseArt.PreviewPathPrefix}_{imageType}.png");

			// do this manually, so that the + icon doesn't change, but we still get a preview
			// the + icon is handled by the HasValue property
			imageSelector.SetValue(ImageSelector.UriPropertyKey, uri);
			imageSelector.SetValue(ImageSelector.HasValuePropertyKey, false);
			imageSelector.SetValue(ImageSelector.PreviewBrushPropertyKey, new ImageBrush(BitmapFrame.Create(uri, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad))
			{
				Stretch = imageSelector.Stretch
			});
		}

		private bool IsValidNoseArtDimensions(int w, int h)
		{
			return w == selectedNoseArt.Width && h == selectedNoseArt.Height;
		}

		private void SaveDefaultTexture(string type)
		{
			if (!selectedNoseArt.Textures.Contains(type))
				return;

			SaveFileDialog dialog = new()
			{
				Filter = "PNG Image|*.png",
				Title = "Save Vanilla Texture",
				FileName = $"{selectedNoseArt.Name}_{type}.png"
			};

			bool? res = dialog.ShowDialog();

			if (res != true)
				return;

			Uri uri = new($"pack://application:,,,/{assembly.GetName().Name};component/Resource/{selectedNoseArt.PreviewPathPrefix}_{type}.png");
			Bitmap bmp = new(Application.GetResourceStream(uri).Stream);

			FileStream fs = (FileStream)dialog.OpenFile();
			bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);

			fs.Close();
		}

		//////////////////////////////////////////////////////////////////////////////////////////
		// BELOW HERE IS JUST EVENT HANDLERS FOR BUTTONS AND STUFF, NOTHING PARTICULARLY USEFUL //
		//////////////////////////////////////////////////////////////////////////////////////////
		private void ImageSelector_col_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("col"); Task.Run(UpdatePreviewImage); }
		private void ImageSelector_opa_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("opa"); Task.Run(UpdatePreviewImage); }
		private void ImageSelector_spc_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("spc"); Task.Run(UpdatePreviewImage); }
		private void ImageSelector_gls_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("gls"); Task.Run(UpdatePreviewImage); }

		private void ImageSelector_col_ImageSelected(object sender, RoutedEventArgs e)
		{
			Task.Run(UpdatePreviewImage);
		}

		private void ImageSelector_opa_ImageSelected(object sender, RoutedEventArgs e)
		{
			Task.Run(UpdatePreviewImage);
		}

		private void ImageSelector_spc_ImageSelected(object sender, RoutedEventArgs e)
		{
			Task.Run(UpdatePreviewImage);
		}

		private void ImageSelector_gls_ImageSelected(object sender, RoutedEventArgs e)
		{
			Task.Run(UpdatePreviewImage);
		}

		private void ReadMePath_Button_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new()
			{
				Filter = "README.md|*README.md|All Markdown Files|*.md|All Files|*.*"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				ReadMePath = openFileDialog.FileName;
			}
		}

		private void IconPath_Button_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new()
			{
				Filter = "PNG Files|*.png|All Files|*.*"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				IconPath = openFileDialog.FileName;
			}
		}

		private void NamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (NamesList.SelectedIndex == -1)
				return;

			selectedNoseArt = noseArts[chassisTypes[ChassisList.SelectedIndex]][NamesList.SelectedIndex];
			// update the preview
			Task.Run(UpdatePreviewImage);

			// reset all images
			ResetImageSelection("col");
			ResetImageSelection("opa");
			ResetImageSelection("spc");
			ResetImageSelection("gls");
		}

		private void ChassisList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ChassisList.SelectedIndex == -1)
				return;

			noseArtNames.Clear();
			foreach (NoseArt art in noseArts[chassisTypes[ChassisList.SelectedIndex]])
			{
				noseArtNames.Add(art.Name);
			}

			NamesList.SelectedIndex = 0;
		}

		private void StatusButton_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				Dictionary<string, Uri> uris = new();
				// create dictionary of uris to pass to the creator
				foreach (string textureType in selectedNoseArt.Textures)
				{
					ImageSelector imageSelector = textureType switch
					{
						"col" => ImageSelector_col,
						"spc" => ImageSelector_spc,
						"gls" => ImageSelector_gls,
						"opa" => ImageSelector_opa,
						_ => throw new NotImplementedException("Invalid imageType"),
					};

					uris.Add(textureType, imageSelector.Uri);
				}

				Scripts.NoseArts.NoseArtCreator creator = new(
					selectedNoseArt,
					uris,
					AuthorName,
					ModName,
					Version,
					ReadMePath,
					IconPath
					);

				// set the status
				StatusButton.Content = "Creating Nose Art...";
				// reset the conversion progress
				StatusButton.Progress = 0;
				// reset the button style
				StatusButton.SetResourceReference(StyleProperty, "ProgressButtonPrimary");
				// lock the button to try prevent multiple conversion threads running at the same time
				StatusButton.IsEnabled = false;

				// run conversion in separate thread from the UI
				Task.Run(() =>
				{
					creator.CreateNoseArt();
					StatusButton.Dispatcher.Invoke(() =>
					{
						// allow the button to be pressed again once conversion is complete
						StatusButton.IsChecked = false;
						StatusButton.IsEnabled = true;
					});
				});
			}
			catch (Exception ex)
			{
				Logger.Error(ex.Message);
				// allow the button to be pressed again if conversion fails
				StatusButton.IsChecked = false;
				StatusButton.IsEnabled = true;
			}
		}

		private void DownloadButton_col_Click(object sender, RoutedEventArgs e) { SaveDefaultTexture("col"); }

		private void DownloadButton_opa_Click(object sender, RoutedEventArgs e) { SaveDefaultTexture("opa"); }

		private void DownloadButton_spc_Click(object sender, RoutedEventArgs e) { SaveDefaultTexture("spc"); }

		private void DownloadButton_gls_Click(object sender, RoutedEventArgs e) { SaveDefaultTexture("gls"); }

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settings = new();
			settings.Closing += (object? sender, CancelEventArgs e) => { Properties.Settings.Default.Save(); };
			settings.ShowDialog();
			CheckStatus();
		}
	}
}
