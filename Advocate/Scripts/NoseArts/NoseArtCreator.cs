using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using Advocate.Logging;
using Advocate.Models.JSON;
using static System.Windows.Forms.DataFormats;

namespace Advocate.Scripts.NoseArts
{
	/// <summary>
	///		Class responsible for creating a Nose Art mod.
	/// </summary>
	internal class NoseArtCreator
	{
		/// <summary>
		///     The name of the Author of the skin.
		/// </summary>
		/// <value>
		///     A non-zero length string of the Author's name, containing
		///     only alphanumeric characters, ' ', and '_'.
		/// </value>
		public string AuthorName { get; private set; }

		/// <summary>
		///     The name of the skin.
		/// </summary>
		/// <value>
		///     A non-zero length string of the skin's name, containing
		///     only alphanumeric characters, ' ', and '_'.
		/// </value>
		public string ModName { get; private set; }

		/// <summary>
		///     The version number of the skin.
		/// </summary>
		/// <value>
		///     A string of the version number of the skin,
		///     in the format MAJOR.MINOR.PATCH. <code>Example: 1.3.0</code>
		/// </value>
		public string Version { get; private set; }


		/// <summary>
		///     The skin's README.md file, as a string.
		/// </summary>
		/// <value>
		///     <para>A string of the skin's README.md file, set at initialisation.</para>
		///     <para>Defaults to an empty string ("")</para>
		/// </value>
		public string ReadMePath { get; private set; }

		/// <summary>
		///     The Icon for the Nose Art.
		/// </summary>
		/// <value>
		///     <para>A Bitmap of the nose art's icon, must be 256x256.</para>
		///     <para>Generated from the _col texture
		///     if null when <see cref="Convert(string, string, string, string, bool)"/> is called.</para>
		/// </value>
		public string? IconPath { get; private set; }

		public NoseArt NoseArt { get; private set; }

		private readonly Dictionary<string, Uri> Images;

		public float ConvertProgress { get { return 100 * curStep / NUM_CONVERT_STEPS; } }

		private const float NUM_CONVERT_STEPS = 10; // INCREMENT THIS WHEN YOU ADD A NEW MESSAGE IDK
		private float curStep = 0;

		private Bitmap? icon;

		// provides the serialisation options we use for writing json files
		private static readonly JsonSerializerOptions jsonOptions = new()
		{
			WriteIndented = true
		};

		// just here for better readability
		private void ConvertTaskComplete() { curStep++; }

		// helper functions for some nicer logging
		private void Debug(string message) { Logger.Debug(message, ConvertProgress); }
		private void Info(string message) { Logger.Info(message, ConvertProgress); }
		private void Completion(string message) { Logger.Completion(message, ConvertProgress); }
		private void Error(string message) { Logger.Error(message, ConvertProgress); }


		// the temp path is appended with the current date and time to prevent duplicates
		public static string tempFolderPath = Path.GetFullPath($"{Path.GetTempPath()}/Advocate/{DateTime.Now:yyyyMMdd-THHmmss}");

		/// <summary>
		///		Constructor for NoseArtCreator, validates input
		/// </summary>
		/// <param name="pNoseArt"></param>
		/// <param name="pImages"></param>
		/// <param name="pAuthorName"></param>
		/// <param name="pSkinName"></param>
		/// <param name="pVersion"></param>
		/// <param name="pReadMePath"></param>
		/// <param name="pIconPath"></param>
		public NoseArtCreator(NoseArt pNoseArt, Dictionary<string, Uri> pImages, string pAuthorName, string pSkinName, string pVersion, string pReadMePath = "", string pIconPath = "")
		{
			// validate pAuthorName, see AuthorName for more details
			if (string.IsNullOrEmpty(pAuthorName)) { throw new ArgumentException("Author Name is required!"); }
			if (Regex.Match(pAuthorName, "[^\\da-zA-Z _]").Success || string.IsNullOrWhiteSpace(pAuthorName)) { throw new ArgumentException("Author Name is invalid!"); }
			AuthorName = pAuthorName;

			// validate pSkinName, same as pAuthorName
			if (string.IsNullOrEmpty(pSkinName)) { throw new ArgumentException("Skin Name is required!"); }
			if (Regex.Match(pSkinName, "[^\\da-zA-Z _]").Success || string.IsNullOrWhiteSpace(pSkinName)) { throw new ArgumentException("Skin Name is invalid!"); }
			ModName = pSkinName;

			// validate pVersion, must be in the format MAJOR.MINOR.VERSION
			if (string.IsNullOrEmpty(pVersion)) { throw new ArgumentException("Version is required!"); }
			if (!Regex.Match(pVersion, "^\\d+.\\d+.\\d+$").Success) { throw new ArgumentException("Version is invalid! (Example: 1.0.0)"); }
			Version = pVersion;

			// check that pReadMePath is valid and leads to a .md file
			if (!string.IsNullOrWhiteSpace(pReadMePath) && !pReadMePath.EndsWith(".md")) { throw new ArgumentException("README path doesn't lead to a .md file!"); }
			ReadMePath = pReadMePath;

			// check that pIconPath is valid and leads to a .png file
			if (!string.IsNullOrWhiteSpace(pIconPath) && !pIconPath.EndsWith(".png")) { throw new ArgumentException("Icon path doesn't lead to a .png file!"); }
			IconPath = pIconPath;

			if (pNoseArt.Textures.Length == 0) { throw new ArgumentException("nose art has no textures! this is probably a bug"); }
			NoseArt = pNoseArt;

			foreach (string texture in NoseArt.Textures)
			{
				if (!pImages.ContainsKey(texture)) { throw new ArgumentException($"pImages doesn't contain image for key '{texture}'!"); }
			}
			Images = pImages;

		}

		/// <summary>
		///		Creates the Nose Art. The .zip file will be put at <see cref="Properties.Settings.OutputPath"/>
		/// </summary>
		public bool CreateNoseArt(bool nogui = false)
		{
			return CreateNoseArt(Properties.Settings.Default.OutputPath, Properties.Settings.Default.RePakPath, Properties.Settings.Default.TexconvPath, Properties.Settings.Default.Description, nogui);
		}

		/// <summary>
		///		Creates the Nose Art. The .zip file will be put at outputPath/>
		/// </summary>
		public bool CreateNoseArt(string outputPath, string repakPath, string texconvPath, string description, bool nogui = false)
		{
			try
			{
				Logger.CreateLogFile($"{outputPath}/advlog-{AuthorName}.{ModName}-{Version}");
			}
			catch (Exception ex) when (!nogui)
			{
				// create message box showing the full error
				MessageBoxButton msgButton = MessageBoxButton.OK;
				MessageBoxImage msgIcon = MessageBoxImage.Error;
				MessageBox.Show($"Failed to create log file at path: '{Logger.LogFilePath}'\nMake sure that the Output Path directory is writable!\n\nDebugging information:\n\n {ex.Message}\n{ex.StackTrace}", "Logging Error", msgButton, msgIcon);
				return false;
			}

			try
			{
				/////////////
				// cleanup //
				/////////////

				Info("Cleaning up...");

				// delete temp folders from previous conversions maybe
				if (Directory.Exists($"{Path.GetTempPath()}/Advocate"))
					Directory.Delete($"{Path.GetTempPath()}/Advocate", true);
			}
			catch (Exception e)
			{
				Debug($"Cleanup failed! there is now some random files in your temp folder i guess?\n Exception: {e.Message}");
			}

			try
			{
				Debug("Starting Nose Art creation...");
				Debug($"Nose Art: {NoseArt.Name}");
				Debug($"Asset path prefix: {NoseArt.AssetPathPrefix}");
				Debug($"Width: {NoseArt.Width}");
				Debug($"Height: {NoseArt.Height}");
				Debug($"Textures:");
				foreach (string txtr in NoseArt.Textures) { Debug($"  {txtr}"); }


				///////////////////////////
				// create temporary dirs //
				///////////////////////////

				// main temp directory
				Directory.CreateDirectory(tempFolderPath);
				// temp directory for the output zip to be constructed in
				string tempModPath = $"{tempFolderPath}\\Mod";
				Directory.CreateDirectory(tempModPath);
				// temp directory for repak setup and usage
				string tempRePakPath = $"{tempFolderPath}\\RePak";
				Directory.CreateDirectory(tempRePakPath);
				// temp directory for texconv setup and usage
				string tempTexConvPath = $"{tempFolderPath}\\TexConv";
				Directory.CreateDirectory(tempTexConvPath);

				// move progress bar
				ConvertTaskComplete();

				//////////////////////////////////////////////////////
				// convert all textures to DDS and generate mipmaps //
				//////////////////////////////////////////////////////

				Dictionary<string, DDS.Manager> managers = new();

				foreach (string textureType in NoseArt.Textures)
				{
					Info($"Reading {textureType} texture");

					// get uri
					Uri uri = Images[textureType];

					// load image into bitmap, everything is getting turned into a png, and then back into a dds
					Bitmap imageBmp;
					if (uri.IsFile)
					{
						if (uri.LocalPath.EndsWith(".png"))
						{
							Debug("URI is png file");
							FileStream stream = new(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
							imageBmp = new(stream);
							stream.Close();
						}
						else if (uri.LocalPath.EndsWith(".dds"))
						{
							Debug("URI is dds file");
							FileStream stream = new(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
							MemoryStream mem = new();
							Debug("Converting dds file to png");
							DDS.Manager.DdsToPng(stream, mem, NoseArt.Width, NoseArt.Height);
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
						Debug("URI is resource stream (default)");
						imageBmp = new(Application.GetResourceStream(uri).Stream);
					}

					// unfortunately bitmaps can't save directly to dds, so I have to save to png and then convert.
					// however, I get the benefit of people being able to input any size image because i can resize pngs easily
					// If people input dds files this means it gets converted to png and then back to dds, not ideal.
					// todo: look into figuring out if an image is a dds image, (with the right size) and skip this step if so
					imageBmp.Save($"{tempTexConvPath}\\{textureType}.png", System.Drawing.Imaging.ImageFormat.Png);

					// use the col texture as the mod's icon (unless an iconpath is specified but this is checked later)
					if (textureType == "col")
					{
						icon = new Bitmap(imageBmp, new(256, 256));
					}
					//////////////////////////////////
					// save textures to temp folder //
					//////////////////////////////////

					if (managers.ContainsKey(textureType))
					{
						throw new Exception("DDS manager already exists? probably a bug");
					}

					Debug("starting texconv");

					Process proc = new();

					proc.StartInfo.RedirectStandardOutput = true;
					proc.StartInfo.RedirectStandardError = true;
					proc.OutputDataReceived += (sender, args) => Logger.Debug(args.Data ?? "");
					proc.ErrorDataReceived += (sender, args) => Logger.Debug(args.Data ?? "");
					proc.StartInfo.UseShellExecute = false;
					proc.StartInfo.CreateNoWindow = true;
					proc.StartInfo.FileName = texconvPath;
					proc.StartInfo.Arguments = $"-ft dds -f BC1_UNORM -nologo -y -m 0 -bc d -o \"{tempTexConvPath}\" \"{tempTexConvPath}\\{textureType}.png\" ";
					proc.Start();
					proc.BeginOutputReadLine();
					proc.BeginErrorReadLine();
					proc.WaitForExit();

					managers.Add(textureType, new());
					managers[textureType].LoadImage(new(new FileStream($"{tempTexConvPath}/{textureType}.dds", FileMode.Open)));
					managers[textureType].Convert();

					Directory.CreateDirectory(Path.GetDirectoryName($"{tempRePakPath}/{NoseArt.GetFullAssetPath(textureType)}.dds") ?? tempRePakPath);
					BinaryWriter writer = new(new FileStream($"{tempRePakPath}/{NoseArt.GetFullAssetPath(textureType)}.dds", FileMode.OpenOrCreate));
					managers[textureType].SaveImage(writer);
					writer.Close();
				}

				// move progress bar
				ConvertTaskComplete();

				//////////////////////
				// create repak map //
				//////////////////////

				Info("Writing Map file");

				Map map = new(ModName, tempRePakPath, $"{tempModPath}/mods/{AuthorName}.{ModName}/paks");

				Directory.CreateDirectory($"{tempModPath}/mods/{AuthorName}.{ModName}/paks");

				/////////////////////////
				// add textures to map //
				/////////////////////////

				foreach (string textureType in NoseArt.Textures)
				{
					map.AddTextureAsset($"{NoseArt.GetFullAssetPath(textureType)}");
				}

				File.WriteAllText($"{tempRePakPath}/map.json", JsonSerializer.Serialize(map, jsonOptions));

				// move progress bar
				ConvertTaskComplete();

				///////////////
				// run repak //
				///////////////

				Info("Running RePak");

				Process P = new();

				P.StartInfo.RedirectStandardOutput = true;
				P.StartInfo.RedirectStandardError = true;
				P.OutputDataReceived += (sender, args) => Debug(args.Data ?? "");
				P.ErrorDataReceived += (sender, args) => Debug(args.Data ?? "");
				P.StartInfo.UseShellExecute = false;
				P.StartInfo.CreateNoWindow = true;
				P.StartInfo.FileName = repakPath;
				P.StartInfo.Arguments = $"\"{tempRePakPath}/map.json\"";
				P.Start();
				P.BeginOutputReadLine();
				P.BeginErrorReadLine();
				P.WaitForExit();

				// currently, RePak always uses exitcode 1 for failure, if we implement more error codes then I'll probably give a more detailed error here
				if (P.ExitCode == 1)
				{
					Error("RePak failed to pack the rpak!");
					return false;
				}

				// move progress bar
				ConvertTaskComplete();

				/////////////////////
				// write rpak.json //
				/////////////////////

				Info("Writing rpak.json");

				// we can just preload our rpak, since it should only contain textures
				RPak rpak = new()
				{
					Preload = new() { { $"{ModName}.rpak", true } }
				};

				File.WriteAllText($"{tempModPath}/mods/{AuthorName}.{ModName}/paks/rpak.json", JsonSerializer.Serialize(rpak, jsonOptions));

				// move progress bar
				ConvertTaskComplete();

				////////////////////
				// write mod.json //
				////////////////////

				Info("Writing mod.json");

				Mod mod = new()
				{
					Name = $"{AuthorName}.{ModName}",
					LoadPriority = 1,
					Version = Version,
					Description = "" // TODO
				};

				File.WriteAllText($"{tempModPath}/mods/{AuthorName}.{ModName}/mod.json", JsonSerializer.Serialize(mod, jsonOptions));

				// move progress bar
				ConvertTaskComplete();

				////////////////////
				// write manifest //
				////////////////////

				Info("Writing manifest.json");

				Manifest manifest = new()
				{
					name = ModName,
					description = "", // TODO
					version_number = Version,
					website_url = "https://github.com/ASpoonPlaysGames/Advocate"
				};

				File.WriteAllText($"{tempModPath}/manifest.json", JsonSerializer.Serialize(manifest, jsonOptions));

				// move progress bar
				ConvertTaskComplete();

				//////////////////
				// write readme //
				//////////////////

				Info("Writing README.md");

				if (string.IsNullOrWhiteSpace(ReadMePath))
				{
					Debug("No ReadMePath, creating README.md");
					File.WriteAllText($"{tempModPath}/README.md", "Created using Advocate.");
				}
				else
				{
					Debug($"ReadMePath = \"{ReadMePath}\", copying file");
					File.Copy(ReadMePath, $"{tempModPath}/README.md");
				}

				// move progress bar
				ConvertTaskComplete();

				////////////////
				// write icon //
				////////////////

				if (string.IsNullOrWhiteSpace(IconPath))
				{
					Info("Generating icon.png");

					if (icon == null)
					{
						throw new Exception("Had to generate an Icon, but no col texture was found");
					}

					icon.Save($"{tempModPath}/icon.png");
				}
				else
				{
					Info("Loading icon.png");
					icon = new(new FileStream(IconPath, FileMode.Open));
					if (icon.Width != 256 || icon.Height != 256)
					{
						Debug($"Icon is not 256x256, resizing. ({icon.Width}x{icon.Height})");
						icon = new(icon, new(256, 256));
					}

					icon.Save($"{tempModPath}/icon.png");
				}

				// move progress bar
				ConvertTaskComplete();

				////////////////////////////////////
				// zip result to output directory //
				////////////////////////////////////

				Info("Creating zip file");

				// create the zip file from the mod temp path
				ZipFile.CreateFromDirectory(tempModPath, $"{tempFolderPath}/{AuthorName}.{ModName}.zip");

				// move progress bar
				ConvertTaskComplete();

				////////////////////////////////////
				// move result out of temp folder //
				////////////////////////////////////

				// set the message for the new conversion step
				Info("Moving zip to output folder...");

				// move the zip file we created to the output folder
				File.Move($"{tempFolderPath}/{AuthorName}.{ModName}.zip", $"{outputPath}/{AuthorName}.{ModName}-{Version}.zip", true);
			}
			catch (Exception ex) when (!nogui)
			{
				// create message box showing the full error
				MessageBoxButton msgButton = MessageBoxButton.OK;
				MessageBoxImage msgIcon = MessageBoxImage.Error;
				MessageBox.Show($"There was an unhandled error during conversion!\n\n{ex.Message}\n\n{ex.StackTrace}", "Conversion Error", msgButton, msgIcon);

				// exit out of the conversion
				Error("Unknown Error!");
				return false;
			}

			// everything is done and hopefully good
			// move progress bar to the end
			curStep = NUM_CONVERT_STEPS;
			Completion("Complete!");

			return true;
		}
	}
}
