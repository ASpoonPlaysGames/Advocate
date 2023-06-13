using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Resources;
using System.Threading;
using Advocate.Models.JSON;
using System.Text.Json;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Drawing;
using HandyControl.Controls;

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

		private Assembly assembly;

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
			foreach(NoseArt art in arts)
			{
				if (noseArts.ContainsKey(art.chassis))
				{
					noseArts[art.chassis].Add(art);
				}
				else
				{
					noseArts.Add(art.chassis, new() { art });
					chassisTypes.Add(art.chassis);
				}
			}

			if (chassisTypes.Count == 0)
			{
				throw new Exception("No nose art chassis found?");
			}

			ChassisList.SelectedIndex = 0;
			NamesList.SelectedIndex = 0;

			
		}

		private void NamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (NamesList.SelectedIndex == -1)
				return;

			selectedNoseArt = noseArts[chassisTypes[ChassisList.SelectedIndex]][NamesList.SelectedIndex];
			// update the preview
			Task.Run(UpdatePreviewImage);

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
			foreach(NoseArt art in noseArts[chassisTypes[ChassisList.SelectedIndex]])
			{
				noseArtNames.Add(art.name);
			}

			NamesList.SelectedIndex = 0;
		}

		private void UpdatePreviewImage()
		{
			if (!selectedNoseArt.textures.Contains("col"))
			{
				throw new Exception("Cannot make a preview without a col map");
			}

			ImagePreview.Dispatcher.Invoke(() =>
			{
				ImagePreview.Visibility = Visibility.Hidden;
			});

			Uri colUri = ImageSelector_col.Dispatcher.Invoke(() => { return ImageSelector_col.Uri; });
			
			if (!selectedNoseArt.textures.Contains("opa"))
			{
				BitmapImage image = new(colUri);

				if (!IsValidNoseArtDimensions(image.PixelWidth, image.PixelHeight))
				{
					return;
				}

				ImagePreview.Dispatcher.Invoke(() =>
				{
					ImagePreview.Source = image;
				});
				
				return;
			}

			Bitmap imageBmp = new(System.Windows.Application.GetResourceStream(colUri).Stream);

			if (!IsValidNoseArtDimensions(imageBmp.Width, imageBmp.Height))
			{
				return;
			}

			Uri maskUri = ImageSelector_opa.Dispatcher.Invoke(() => { return ImageSelector_opa.Uri; });
			Bitmap maskBmp = new(System.Windows.Application.GetResourceStream(maskUri).Stream);

			if (!IsValidNoseArtDimensions(maskBmp.Width, maskBmp.Height))
			{
				return;
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

			if (!selectedNoseArt.textures.Contains(imageType))
			{
				imageSelector.Visibility = Visibility.Hidden;
				button.Visibility = Visibility.Hidden;
			}
			else
			{
				imageSelector.Visibility = Visibility.Visible;
				button.Visibility = Visibility.Visible;
			}

			Uri uri = new($"pack://application:,,,/{assembly.GetName().Name};component/Resource/{selectedNoseArt.previewPathPrefix}_{imageType}.png");

			// do this manually, so that the + icon doesn't change, but we still get a preview
			imageSelector.SetValue(ImageSelector.UriPropertyKey, uri);
			imageSelector.SetValue(ImageSelector.PreviewBrushPropertyKey, new ImageBrush(BitmapFrame.Create(uri, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None))
			{
				Stretch = imageSelector.Stretch
			});
		}

		private bool IsValidNoseArtDimensions(int w, int h)
		{
			return w == selectedNoseArt.width && h == selectedNoseArt.height;
		}

		//////////////////////////////////////////////////////////////////////////////////////////
		// BELOW HERE IS JUST EVENT HANDLERS FOR BUTTONS AND STUFF, NOTHING PARTICULARLY USEFUL //
		//////////////////////////////////////////////////////////////////////////////////////////
		private void ImageSelector_col_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("col"); }
		private void ImageSelector_opa_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("opa"); }
		private void ImageSelector_spc_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("spc"); }
		private void ImageSelector_gls_ImageUnselected(object sender, RoutedEventArgs e) { ResetImageSelection("gls"); }
	}
}
