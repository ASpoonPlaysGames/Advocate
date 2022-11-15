using Pfim;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Advocate.Conversion
{
	/// <summary>
	///     Handles skin conversion.
	/// </summary>
	internal class Converter
	{
		/// <summary>
		///     The path to the .zip file containing the skin.
		/// </summary>
		/// <value>
		///     A relative or fully qualified path that leads to a .zip file.
		/// </value>
		public string ZipPath { get; private set; }

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
		public string SkinName { get; private set; }

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
        public string? ReadMePath { get; private set; }

        /// <summary>
        ///     The Icon for the skin.
        /// </summary>
        /// <value>
        ///     <para>A Bitmap of the skin's icon, must be 256x256.</para>
        ///     <para>Generated from the first _col texture found in the skin
        ///     if null when <see cref="Convert(string, string, string)"/> is called.</para>
        /// </value>
        public string? IconPath { get; private set; }

        /// <summary>
        ///     An event handler for the <see cref="OnConversionMessage(ConversionMessageEventArgs)"/> event.
        /// </summary>
        public event EventHandler<ConversionMessageEventArgs> ConversionMessage;
        /// <summary>
        ///     Helper function that creates a new <see cref="ConversionMessageEventArgs"/>
        ///     from an input string and calls <see cref="OnConversionMessage(ConversionMessageEventArgs)"/>.
        /// </summary>
        /// <param name="message">The message that will be passed to the event listeners.</param>
        /// <param name="type">The message type, used in formatting and showing in gui.</param>
        protected virtual void OnConversionMessage(string? message, MessageType type = MessageType.Info)
        {
            OnConversionMessage(new ConversionMessageEventArgs(message) { Type = type, ConversionPercent = 100 * (curStep / NUM_CONVERT_STEPS) });
        }
        /// <summary>
        ///     Event that is called on a generic message received from the conversion.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConversionMessage(ConversionMessageEventArgs e)
        {
            ConversionMessage?.Invoke(this, e);
        }

        private void OnConversionError(string message)
        {
            OnConversionMessage(message, MessageType.Error);
        }

        private void ConvertTaskComplete(MessageType type = MessageType.Info)
        {
            float percent = 100 * (++curStep / NUM_CONVERT_STEPS);
            ConversionMessageEventArgs args = new($"Conversion Progress: {percent}%") { ConversionPercent = percent, Type = type };
            OnConversionMessage(args);
        }

        private const float NUM_CONVERT_STEPS = 13; // INCREMENT THIS WHEN YOU ADD A NEW MESSAGE IDK
        private float curStep = 0;

        /// <summary>
        ///     Constructor for the Converter class, generates <see cref="ReadMePath"/> and <see cref="IconPath"/> if null.
        /// </summary>
        public Converter(string pZipPath, string pAuthorName, string pSkinName, string pVersion, string pReadMePath = "", string pIconPath = "")
		{
			// validate pZipPath, must exist and be a .zip file
			if (!File.Exists(pZipPath))
			{
				throw new FileNotFoundException("Couldn't find file at Skin Path!");
			}
			if (!pZipPath.EndsWith(".zip"))
            {
				throw new ArgumentException("Skin Path is invalid!");
			}
			ZipPath = pZipPath;

			// validate pAuthorName, see AuthorName for more details
			if (string.IsNullOrEmpty(pAuthorName)) { throw new ArgumentException("Author Name is required!"); }
			if (Regex.Match(pAuthorName, "[^\\da-zA-Z _]").Success || string.IsNullOrWhiteSpace(pAuthorName))
			{
				throw new ArgumentException("Author Name is invalid!");
			}
			AuthorName = pAuthorName;

			// validate pSkinName, same as pAuthorName
			if (string.IsNullOrEmpty(pSkinName)) { throw new ArgumentException("Skin Name is required!"); }
			if (Regex.Match(pSkinName, "[^\\da-zA-Z _]").Success || string.IsNullOrWhiteSpace(pSkinName))
			{
				throw new ArgumentException("Skin Name is invalid!");
			}
			SkinName = pSkinName;

			// validate pVersion, must be in the format MAJOR.MINOR.VERSION
			if (string.IsNullOrEmpty(pVersion)) { throw new ArgumentException("Version is required!"); }
			if (!Regex.Match(pVersion, "^\\d+.\\d+.\\d+$").Success)
			{
				throw new ArgumentException("Version is invalid! (Example: 1.0.0)");
			}
			Version = pVersion;

            // check that ReadMePath is valid and leads to a .md file
            if (!string.IsNullOrWhiteSpace(ReadMePath) && !ReadMePath.EndsWith(".md"))
            {
                throw new ArgumentException("README path doesn't lead to a .md file!");
            }
            ReadMePath = pReadMePath;

            // check that IconPath is valid and leads to a .png file
            if (!string.IsNullOrWhiteSpace(IconPath) && !IconPath.EndsWith(".png"))
            {
                throw new ArgumentException("Icon path doesn't lead to a .png file!");
            }
            IconPath = pIconPath;
        }

        /// <summary>
        ///		Converts the skin. The converted .zip file will be put at <see cref="Properties.Settings.OutputPath"/>
        /// </summary>
        public void Convert()
        {
            Convert(Properties.Settings.Default.OutputPath, Properties.Settings.Default.RePakPath, Properties.Settings.Default.Description);
        }
        /// <summary>
        ///		Converts the skin. The converted .zip file will be put at <see cref="outputPath"/>
        /// </summary>
        public void Convert(string outputPath, string repakPath, string description)
        {

            // initialise various path variables, just because they are useful

            // the temp path is appended with the current date and time to prevent duplicates
            string tempFolderPath = $"{Path.GetTempPath()}/Advocate/{DateTime.Now:yyyyMMdd-THHmmss}";
            string skinTempFolderPath = Path.GetFullPath($"{tempFolderPath}/Skin");
            string modTempFolderPath = Path.GetFullPath($"{tempFolderPath}/Mod");
            string repakTempFolderPath = Path.GetFullPath($"{tempFolderPath}/RePak");

            // try convert stuff, if we get a weird exception, don't crash preferably
            try
            {
                /////////////////////////////
                // create temp directories //
                /////////////////////////////

                //o.Message = "Creating temporary directories...";

                // directory for unzipped file
                Directory.CreateDirectory(skinTempFolderPath);

                // directory for TS-compliant mod
                Directory.CreateDirectory(modTempFolderPath);

                // directory for RePak things
                Directory.CreateDirectory(repakTempFolderPath);

                // move progress bar
                ConvertTaskComplete();

                ///////////////////////////////
                // unzip skin to temp folder //
                ///////////////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Unzipping skin...");

                // try to extract the zip, catch any errors and just exit, sometimes we get bad zips, non-zips, etc. etc.
                try
                {
                    ZipFile.ExtractToDirectory(ZipPath, skinTempFolderPath, true);
                }
                catch (InvalidDataException)
                {
                    OnConversionError("Unable to unzip skin!");
                    return;
                }

                // move progress bar
                ConvertTaskComplete();

                ////////////////////////////////////
                // create temp mod file structure //
                ////////////////////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Creating mod file structure...");

                // create the bare-bones folder structure for the mod
                Directory.CreateDirectory($"{modTempFolderPath}/mods/{AuthorName}.{SkinName}/paks");

                // move progress bar
                ConvertTaskComplete();

                /////////////////////
                // create icon.png //
                /////////////////////

                // if IconPath is an empty string, we try and generate the icon from a _col texture (thunderstore requires an icon)
                if (string.IsNullOrWhiteSpace(IconPath))
                {
                    // set the message for the new conversion step
                    OnConversionMessage("Generating icon.png...");
                    // fuck you, im using the col of the first folder i find, shouldve specified an icon path
                    string[] skinPaths = Directory.GetDirectories(skinTempFolderPath);
                    if (skinPaths.Length == 0)
                    {
                        OnConversionError("Couldn't generate icon.png: No Skins found in zip!");
                        return;
                    }
                    string[] resolutions = Directory.GetDirectories(skinPaths[0]);
                    if (resolutions.Length == 0)
                    {
                        OnConversionError("Couldn't generate icon.png: No Skins found in zip!");
                        return;
                    }
                    // find highest resolution folder
                    int highestRes = 0;
                    foreach (string resolution in resolutions)
                    {
                        string? thing = Path.GetFileName(resolution);
                        // check if higher than highestRes and a power of 2
                        if (int.TryParse(thing, out int res) && res > highestRes && (highestRes & (highestRes - 1)) == 0)
                        {
                            highestRes = res;
                        }
                    }
                    // check that we actually found something
                    if (highestRes == 0)
                    {
                        OnConversionError("Couldn't generate icon.png: No valid image resolutions found in zip!");
                        return;
                    }

                    string[] files = Directory.GetFiles(skinPaths[0] + "\\" + highestRes.ToString());
                    if (files.Length == 0)
                    {
                        OnConversionError("Couldn't generate icon.png: No files in highest resolution folder!");
                        return;
                    }
                    // find _col file
                    string colPath = "";
                    foreach (string file in files)
                    {
                        if (file.EndsWith("_col.dds"))
                        {
                            colPath = file;
                            break;
                        }
                    }
                    if (colPath == "")
                    {
                        OnConversionError("Couldn't generate icon.png: No _col texture found in highest resolution folder!");
                        return;
                    }

                    if (!DdsToPng(colPath, modTempFolderPath + "\\icon.png"))
                    {
                        OnConversionError("Couldn't generate icon.png: Failed to convert dds to png!");
                        return;
                    }
                }
                else
                {
                    // set the message for the new conversion step
                    OnConversionMessage("Copying icon.png...");
                    // check that png is correct size
                    Image img = Image.FromFile(IconPath);
                    if (img.Width != 256 || img.Height != 256)
                    {
                        OnConversionError("Icon must be 256x256!");
                        return;
                    }
                    // copy png over
                    File.Copy(IconPath, $"{modTempFolderPath}/icon.png");
                }

                // move progress bar
                ConvertTaskComplete();

                //////////////////////
                // create README.md //
                //////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Generating README.md...");
                File.WriteAllText($"{modTempFolderPath}/README.md", ReadMePath);

                // move progress bar
                ConvertTaskComplete();

                //////////////////////////////////////////////////////////////////
                // create map.json and move textures to temp folder for packing //
                //////////////////////////////////////////////////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Copying textures...");

                // this holds a string of json which will be appended to as the map file is constructed, TODO - refactor to use json library?
                string map = $"{{\n\"name\":\"{SkinName}\",\n\"assetsDir\":\"{repakTempFolderPath.Replace('\\', '/')}/assets\",\n\"outputDir\":\"{modTempFolderPath.Replace('\\', '/')}/mods/{AuthorName}.{SkinName}/paks\",\n\"version\": 7,\n\"files\":[\n";

                // this tracks the textures that we have already added to the json, so we can avoid duplicates in there
                List<string> textures = new();
                // this tracks the different skin types that we have found, for description parsing later
                List<string> skinTypes = new();

                bool isFirst = true;
                foreach (string skinPath in Directory.GetDirectories(skinTempFolderPath))
                {
                    // some skins have random files and folders in here, like images and stuff, so I have to do sorting in an annoying way
                    List<string> parsedDirs = new();
                    foreach (string dir in Directory.GetDirectories(skinPath))
                    {
                        // only add to the list of dirs if we manage to parse the value
                        if (int.TryParse(Path.GetFileName(dir), out int val))
                        {
                            parsedDirs.Add(dir);
                        }
                    }

                    foreach (string resolution in parsedDirs.OrderBy(path => int.Parse(Path.GetFileName(path))))
                    {
                        if (int.TryParse(Path.GetFileName(resolution), out int res))
                        {
                            foreach (string texture in Directory.GetFiles(resolution))
                            {
                                // move texture to temp folder for packing
                                // convert from skin tool syntax to actual texture path, gotta be hardcoded because pain
                                string texturePath = TextureNameToPath(Path.GetFileNameWithoutExtension(texture));
                                if (string.IsNullOrWhiteSpace(texturePath))
                                {
                                    OnConversionError($"Failed to convert texture '{Path.GetFileNameWithoutExtension(texture)}')");
                                    return;
                                }

                                // avoid duplicate textures in the json
                                if (!textures.Contains(texturePath))
                                {
                                    // dont add a comma on the first one
                                    if (!isFirst)
                                        map += ",\n";
                                    map += $"{{\n\"$type\":\"txtr\",\n\"path\":\"{texturePath}\",\n\"disableStreaming\":true,\n\"saveDebugName\":true\n}}";
                                    // add texturePath to tracked textures
                                    textures.Add(texturePath);
                                    // add texture to skinTypes for tracking which skins are in the package
                                    skinTypes.Add(Path.GetFileNameWithoutExtension(texture).Split("_")[0]);
                                }
                                isFirst = false;

                                // instantiate dds handler, pass it the texture path
                                DdsHandler handler = new(texture);
                                // convert the dds image to one that works with RePak
                                handler.Convert();
                                // save the file where RePak expects it to be
                                handler.Save($"{repakTempFolderPath}/assets/{texturePath}.dds");
                            }
                        }
                    }
                }

                // end the json
                map += "\n]\n}";
                File.WriteAllText($"{repakTempFolderPath}/map.json", map);

                // move progress bar
                ConvertTaskComplete();

                //////////////////////////
                // pack using RePak.exe //
                //////////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Packing using RePak...");


                // create the process for RePak


                //var sb = new StringBuilder();

                Process P = new();

                //P.StartInfo.RedirectStandardOutput = true;
                //P.StartInfo.RedirectStandardError = true;
                //P.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
                //P.ErrorDataReceived += (sender, args) => sb.AppendLine(args.Data);
                //P.StartInfo.UseShellExecute = false;
                P.StartInfo.FileName = repakPath;
                P.StartInfo.Arguments = $"\"{repakTempFolderPath}\\map.json\"";
                P.Start();
                //P.BeginOutputReadLine();
                //P.BeginErrorReadLine();

                // wait for RePak to finish
                P.WaitForExit();

                // currently, RePak always uses exitcode 1 for failure, if we implement more error codes then I'll probably give a more detailed error here
                if (P.ExitCode == 1)
                {
                    OnConversionError("RePak failed to pack the rpak!");
                    return;
                }

                // move progress bar
                ConvertTaskComplete();

                //////////////////////
                // create rpak.json //
                //////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Generating rpak.json...");

                // we can just preload our rpak, since it should only contain textures
                string rpakjson = $"{{\n\"Preload\":\n{{\n\"{SkinName}.rpak\":true\n}}\n}}";

                File.WriteAllText($"{modTempFolderPath}/mods/{AuthorName}.{SkinName}/paks/rpak.json", rpakjson);

                // move progress bar
                ConvertTaskComplete();

                //////////////////////////
                // create manifest.json //
                //////////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Writing manifest.json...");

                // create the DescriptionHandler for parsing the description
                DescriptionHandler desc = new()
                {
                    Author = AuthorName,
                    Version = Version,
                    Name = SkinName,
                    Types = skinTypes.Distinct().ToArray()
                };

                string manifest = $"{{\n\"name\": \"{SkinName.Replace(' ', '_')}\",\n\"version_number\":\"{Version}\",\n\"website_url\":\"https://github.com/ASpoonPlaysGames/Advocate\",\n\"dependencies\":[],\n\"description\":\"{desc.FormatDescription(description)}\"\n}}";
                File.WriteAllText($"{modTempFolderPath}/manifest.json", manifest);

                // move progress bar
                ConvertTaskComplete();

                /////////////////////
                // create mod.json //
                /////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Writing mod.json...");

                string modJson = $"{{\n\"Name\": \"{AuthorName}.{SkinName}\",\n\"Description\": \"{desc.FormatDescription(description)}\",\n\"Version\": \"{Version}\",\n\"LoadPriority\": 1\n}}";
                File.WriteAllText($"{modTempFolderPath}/mods/{AuthorName}.{SkinName}/mod.json", modJson);

                // move progress bar
                ConvertTaskComplete();

                ///////////////////
                // zip up result //
                ///////////////////

                // set the message for the new conversion step
                OnConversionMessage("Zipping mod...");

                // create the zip file from the mod temp path
                ZipFile.CreateFromDirectory(modTempFolderPath, $"{tempFolderPath}/{AuthorName}.{SkinName}.zip");

                // move progress bar
                ConvertTaskComplete();

                ////////////////////////////////////
                // move result out of temp folder //
                ////////////////////////////////////

                // set the message for the new conversion step
                OnConversionMessage("Moving zip to output folder...");

                // move the zip file we created to the output folder
                File.Move($"{tempFolderPath}/{AuthorName}.{SkinName}.zip", $"{outputPath}/{AuthorName}.{SkinName}-{Version}.zip", true);

                // move progress bar
                ConvertTaskComplete();

                /////////////
                // cleanup //
                /////////////

                OnConversionMessage("Cleaning up...");

                // delete temp folders
                if (Directory.Exists(tempFolderPath))
                    Directory.Delete(tempFolderPath, true);
            }
            catch (Exception ex)
            {
                // create message box showing the full error
                MessageBoxButton msgButton = MessageBoxButton.OK;
                MessageBoxImage msgIcon = MessageBoxImage.Error;
                MessageBox.Show($"There was an unhandled error during conversion!\n\n{ex.Message}\n\n{ex.StackTrace}", "Conversion Error", msgButton, msgIcon);

                // exit out of the conversion
                OnConversionError("Unknown Error!");
                return;
            }

            // everything is done and hopefully good
            // move progress bar to the end
            curStep = NUM_CONVERT_STEPS;
            // Log complete conversion
            OnConversionMessage("Conversion Complete!", MessageType.Completion);
        }

        /// <summary>
        ///     Converts a .dds file to a .png file with dimensions of 256x256 (thunderstore compliant)
        /// </summary>
        /// <param name="imagePath">The path of the input image (.dds)</param>
        /// <param name="outputPath">The path of the output image (.png)</param>
        /// <returns>true on success</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool DdsToPng(string imagePath, string outputPath)
        {
            // this code is just yoinked from pfim usage example
            using (var image = Pfimage.FromFile(imagePath))
            {
                var format = image.Format switch
                {
                    Pfim.ImageFormat.Rgba32 => PixelFormat.Format32bppArgb,
                    _ => throw new NotImplementedException(),// see the sample for more details
                };

                // Pin pfim's data array so that it doesn't get reaped by GC, unnecessary
                // in this snippet but useful technique if the data was going to be used in
                // control like a picture box
                var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
                try
                {
                    var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                    var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, data);
                    // resize the bitmap before saving it, we need 256x256
                    var resized = new Bitmap(bitmap, new System.Drawing.Size(256, 256));
                    resized.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                finally
                {
                    handle.Free();
                }
            }
            return true;
        }

        // these dictionaries have to be hardcoded because skin tool just hardcodes in offsets afaik
        // maybe eventually use a .csv for this?

        // weapons
        private readonly Dictionary<string, string> weaponNameToPath = new()
        {
            // pilot weapons
            { "R201_Default", @"texture\\models\\weapons\\r101\\r101" },
            { "R101_Default", @"texture\\models\\Weapons_R2\\r101_sfp\\r101_sfp" },
            { "HemlokBFR_Default", @"texture\\models\\Weapons_R2\\hemlok_bfr_ar\\hemlok_BFR_ar" },
            { "V47Flatline_Default", @"texture\\models\\weapons\\vinson\\vinson_rifle" },
            { "G2A5_Default", @"texture\\models\\Weapons_R2\\g2a4_ar\\g2a4_ar_col" },
            { "Alternator_Default", @"texture\\models\\Weapons_R2\\alternator_smg\\alternator_smg" },
            { "CAR_Default", @"texture\\models\\Weapons_R2\\car_smg\\CAR_smg" },
            { "R97_Default", @"texture\\models\\Weapons_R2\\r97\\R97_CN" },
            { "Volt_Default", @"texture\\models\\weapons\\hemlok_smg\\hemlok_smg" },
            { "Devotion_Default", @"texture\\models\\weapons\\hemlock_br\\hemlock_br" },
            { "Devotion_clip_Default", @"texture\\models\\weapons\\hemlock_br\\hemlock_br_acc" },
            { "LSTAR_Default", @"texture\\models\\weapons\\lstar\\lstar" },
            { "Spitfire_Default", @"texture\\models\\Weapons_R2\\spitfire_lmg\\spitfire_lmg" },
            { "DoubleTake_Default", @"texture\\models\\Weapons_R2\\doubletake_sr\\doubletake" },
            { "Kraber_Default", @"texture\\models\\Weapons_R2\\kraber_sr\\kraber_sr" },
            { "LongbowDMR_Default", @"texture\\models\\Weapons_R2\\Longbow_dmr\\longbow_dmr" },
            { "EVA8_Default", @"texture\\models\\Weapons_R2\\eva8_stgn\\eva8_stgn" },
            { "Mastiff_Default", @"texture\\models\\weapons\\mastiff_stgn\\mastiff_stgn" },
            { "ColdWar_Default", @"texture\\models\\Weapons_R2\\pulse_lmg\\pulse_lmg" },
            { "EPG_Default", @"texture\\models\\Weapons_R2\\epg\\epg" },
            { "SMR_Default", @"texture\\models\\Weapons_R2\\sidewinder_at\\sidewinder_at" },
            { "Softball_Default", @"texture\\models\\weapons\\softball_at\\softball_at_skin01" },
            { "Mozambique_Default", @"texture\\models\\Weapons_R2\\pstl_sa3" },
            { "P2016_Default", @"texture\\models\\Weapons_R2\\p2011_pstl\\p2011_pstl" },
            { "RE45_Default", @"texture\\models\\Weapons_R2\\re45_pstl\\RE45" },
            { "SmartPistol_Default", @"texture\\models\\Weapons_R2\\smart_pistol\\Smart_Pistol_MK6" },
            { "Wingman_Default", @"texture\\models\\weapons\\b3_wingman\\b3_wingman" },
            { "WingmanElite_Default", @"texture\\models\\Weapons_R2\\wingman_elite\\wingman_elite" },
            { "Archer_Default", @"texture\\models\\Weapons_R2\\archer_at\\archer_at" },
            { "ChargeRifle_Default", @"texture\\models\\Weapons_R2\\charge_rifle\\charge_rifle_at" },
            { "MGL_Default", @"texture\\models\\weapons\\mgl_at\\mgl_at" },
            { "Thunderbolt_Default", @"texture\\models\\Weapons_R2\\arc_launcher\\arc_launcher" },
            // titan weapons
            { "BroadSword_Default", @"texture\\models\\weapons\\titan_sword\\titan_sword_01" },
            { "LeadWall_Default", @"texture\\models\\Weapons_R2\\titan_triple_threat\\triple_threat" },
            { "PlasmaRailgun_Default", @"texture\\models\\Weapons_R2\\titan_plasma_railgun\\plasma_railgun" },
            { "SplitterRifle_Default", @"texture\\models\\weapons\\titan_particle_accelerator\\titan_particle_accelerator" },
            { "ThermiteLauncher_Default", @"texture\\models\\Weapons_R2\\titan_thermite_launcher\\thermite_launcher" },
            { "TrackerCannon_Default", @"texture\\models\\Weapons_R2\\titan_40mm\\titan_40mm" },
            { "XO16_Default", @"texture\\models\\Weapons_R2\\xo16_shorty_titan\\xo16_shorty_titan" },
            { "XO16_clip_Default", @"texture\\models\\Weapons_R2\\xo16_a2_titan\\xo16_a2_titan" },
            // melee
            { "Sword_Default", @"texture\\models\\weapons\\bolo_sword\\bolo_sword_01" }, // also idk about this, this is a blank texture in vanilla
            { "Kunai_Default", @"texture\\models\\Weapons_R2\\shuriken_kunai\\kunai_shuriken" }, // again, not entirely sure
        };
        // pilot and titan overrides - used for weird exceptions where things share textures etc.
        private readonly Dictionary<string, string> nameToPathOverrides = new()
        {
            //////////////
            /// PILOTS ///
            //////////////
            // cloak
            // A-wall
            // phase
            { "PhaseShift_fbody_ilm", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_male_b_v1_skn01_ilm" },
            // stim
            // grapple
            // pulse
            // holo
            { "HoloPilot_fbody_col", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_female_b_v1_skn02_col" },
            { "HoloPilot_mbody_col", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_b_v1_skn02_col" },
            { "HoloPilot_mbody_spc", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_b_v1_skn02_spc" },
            { "HoloPilot_gear_col", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_g_v1_skn02_col" },
            { "HoloPilot_gear_spc", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_g_v1_skn02_spc" },
            { "HoloPilot_helmet_col", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_he_v1_skn02_col" },
            { "HoloPilot_helmet_spc", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_he_v1_skn02_spc" },
            { "HoloPilot_jumpkit_col", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_j_v1_skn02_col" },
            { "HoloPilot_viewhand_col", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_vh_v1_skn02_col" },
            { "HoloPilot_viewhand_spc", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_vh_v1_skn02_spc" },
            //////////////
            /// TITANS ///
            //////////////
            // ion
            { "ION_Default_ao", @"texture\\models\\titans_r2\\medium_ion\\warpaint\\warpaint_02\\t_m_ion_warpaint_skin02_ao" },
            { "ION_Default_cav", @"texture\\models\\titans_r2\\medium_ion\\warpaint\\warpaint_02\\t_m_ion_warpaint_skin02_cav" },
            { "ION_Default_ilm", @"texture\\models\\titans_r2\\medium_ion\\warpaint\\warpaint_02\\t_m_ion_warpaint_skin02_ilm" },
            { "ION_Default_nml", @"texture\\models\\titans_r2\\medium_ion\\warpaint\\warpaint_02\\t_m_ion_warpaint_skin02_nml" },
            // ion prime
            { "PrimeION_Default_ao", @"texture\\models\\titans_r2\\medium_ion_prime\\warpaint\\warpaint_00\\t_m_ion_prime_warpaint_skin01_ao" },
            { "PrimeION_Default_cav", @"texture\\models\\titans_r2\\medium_ion_prime\\warpaint\\warpaint_00\\t_m_ion_prime_warpaint_skin01_cav" },
            { "PrimeION_Default_ilm", @"texture\\models\\titans_r2\\medium_ion_prime\\warpaint\\warpaint_00\\t_m_ion_prime_warpaint_skin01_ilm" },
            // tone
            { "Tone_Default_ao", @"texture\\models\\titans_r2\\medium_tone\\warpaint\\warpaint_00\\t_m_tone_warpaint_skin00_ao" },
            { "Tone_Default_cav", @"texture\\models\\titans_r2\\medium_tone\\warpaint\\warpaint_00\\t_m_tone_warpaint_skin00_cav" },
            { "Tone_Default_ilm", @"texture\\models\\titans_r2\\medium_tone\\warpaint\\warpaint_00\\t_m_tone_warpaint_skin00_ilm" },
            // tone prime
            { "PrimeTone_Default_ao", @"texture\\models\\titans_r2\\medium_tone_prime\\warpaint\\warpaint_00\\t_m_tone_prime_warpaint_skin00_ao" },
            { "PrimeTone_Default_cav", @"texture\\models\\titans_r2\\medium_tone_prime\\warpaint\\warpaint_00\\t_m_tone_prime_warpaint_skin00_cav" },
            { "PrimeTone_Default_ilm", @"texture\\models\\titans_r2\\medium_tone_prime\\warpaint\\warpaint_00\\t_m_tone_prime_warpaint_skin00_ilm" },
            { "PrimeTone_Default_nml", @"texture\\models\\titans_r2\\medium_tone_prime\\warpaint\\warpaint_00\\t_m_tone_prime_warpaint_skin00_nml" },
            // northstar
            // northstar prime
            { "PrimeNorthstar_Default_ao", @"texture\\models\\titans_r2\\light_northstar_prime\\warpaint\\warpaint_00\\t_l_northstar_prime_warpaint_skin00_ao" },
            { "PrimeNorthstar_Default_cav", @"texture\\models\\titans_r2\\light_northstar_prime\\warpaint\\warpaint_00\\t_l_northstar_prime_warpaint_skin00_cav" },
            { "PrimeNorthstar_Default_ilm", @"texture\\models\\titans_r2\\light_northstar_prime\\warpaint\\warpaint_00\\t_l_northstar_prime_warpaint_skin00_ilm" },
            { "PrimeNorthstar_Default_nml", @"texture\\models\\titans_r2\\light_northstar_prime\\warpaint\\warpaint_00\\t_l_northstar_prime_warpaint_skin00_nml" },
            // ronin
            // ronin prime
            { "PrimeRonin_Default_ao", @"texture\\models\\titans_r2\\light_ronin_prime\\warpaint\\warpaint_00\\t_l_ronin_prime_warpaint_skin00_ao" },
            { "PrimeRonin_Default_cav", @"texture\\models\\titans_r2\\light_ronin_prime\\warpaint\\warpaint_00\\t_l_ronin_prime_warpaint_skin00_cav" },
            { "PrimeRonin_Default_ilm", @"texture\\models\\titans_r2\\light_ronin_prime\\warpaint\\warpaint_00\\t_l_ronin_prime_warpaint_skin00_ilm" },
            { "PrimeRonin_Default_nml", @"texture\\models\\titans_r2\\light_ronin_prime\\warpaint\\warpaint_00\\t_l_ronin_prime_warpaint_skin00_nml" },
            // scorch
            { "Scorch_Default_ao", @"texture\\models\\titans_r2\\heavy_scorch\\warpaint\\warpaint_00\\t_h_scorch_warpaint_skin00_ao" },
            { "Scorch_Default_cav", @"texture\\models\\titans_r2\\heavy_scorch\\warpaint\\warpaint_00\\t_h_scorch_warpaint_skin00_cav" },
            { "Scorch_Default_ilm", @"texture\\models\\titans_r2\\heavy_scorch\\warpaint\\warpaint_00\\t_h_scorch_warpaint_skin00_ilm" },
            // scorch prime
            { "PrimeScorch_Default_ao", @"texture\\models\\titans_r2\\heavy_scorch_prime\\warpaint\\warpaint_00\\t_h_scorch_prime_warpaint_skin00_ao" },
            { "PrimeScorch_Default_cav", @"texture\\models\\titans_r2\\heavy_scorch_prime\\warpaint\\warpaint_00\\t_h_scorch_prime_warpaint_skin00_cav" },
            { "PrimeScorch_Default_ilm", @"texture\\models\\titans_r2\\heavy_scorch_prime\\warpaint\\warpaint_00\\t_h_scorch_prime_warpaint_skin00_ilm" },
            // legion
            { "Legion_Default_ao", @"texture\\models\\titans_r2\\heavy_legion\\warpaint\\warpaint_00\\t_h_legion_warpaint_skin00_ao" },
            { "Legion_Default_cav", @"texture\\models\\titans_r2\\heavy_legion\\warpaint\\warpaint_00\\t_h_legion_warpaint_skin00_cav" },
            { "Legion_Default_ilm", @"texture\\models\\titans_r2\\heavy_legion\\warpaint\\warpaint_00\\t_h_legion_warpaint_skin00_ilm" },
            // legion prime
            { "PrimeLegion_Default_ao", @"texture\\models\\titans_r2\\heavy_legion_prime\\warpaint\\warpaint_00\\t_h_legion_prime_warpaint_skin00_ao" },
            { "PrimeLegion_Default_cav", @"texture\\models\\titans_r2\\heavy_legion_prime\\warpaint\\warpaint_00\\t_h_legion_prime_warpaint_skin00_cav" },
            { "PrimeLegion_Default_ilm", @"texture\\models\\titans_r2\\heavy_legion_prime\\warpaint\\warpaint_00\\t_h_legion_prime_warpaint_skin00_ilm" },
            { "PrimeLegion_Default_nml", @"texture\\models\\titans_r2\\heavy_legion_prime\\warpaint\\warpaint_00\\t_h_legion_prime_warpaint_skin00_nml" },
            // monarch
            { "Monarch_Default_ao", @"texture\\models\\titans_r2\\medium_vanguard\\warpaint\\warpaint_00\\t_m_vanguard_prime_warpaint_skin00_ao" },
            { "Monarch_Default_cav", @"texture\\models\\titans_r2\\medium_vanguard\\warpaint\\warpaint_00\\t_m_vanguard_prime_warpaint_skin00_cav" },
            { "Monarch_Default_ilm", @"texture\\models\\titans_r2\\medium_vanguard\\warpaint\\warpaint_00\\t_m_vanguard_prime_warpaint_skin00_ilm" },
            { "Monarch_Default_nml", @"texture\\models\\titans_r2\\medium_vanguard\\warpaint\\warpaint_00\\t_m_vanguard_prime_warpaint_skin00_nml" },
        };
        // pilots and titans
        private readonly Dictionary<string, string> nameToPath = new()
        {
            //////////////
            /// PILOTS ///
            //////////////
            // cloak
            { "Cloak_fbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_drex\\pilot_heavy_drex_f_body_skin_01" },
            { "Cloak_mbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_drex\\pilot_heavy_drex_m_body_skin_01" },
            { "Cloak_gauntlet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_drex\\pilot_heavy_drex_gauntlet_skin_01" },
            { "Cloak_gear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_drex\\pilot_heavy_drex_gear_skin_01" },
            { "Cloak_jumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_drex\\pilot_heavy_drex_jumpkit_skin_01" },
            { "Cloak_ghillie", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_drex\\pilot_heavy_drex_ghullie_skin_01" },// ghullie lol
            { "Cloak_helmet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_drex\\pilot_heavy_drex_helmet_skin_01" },
            // A-wall
            { "AWall_fbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_hevy_roog\\pilot_hev_roog_f_body_skn_01" },
            { "AWall_mbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_hevy_roog\\pilot_hev_roog_m_body_skn_01" },
            { "AWall_gauntlet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_hevy_roog\\pilot_hev_roog_m_gauntlet_skn_01" },
            { "AWall_gear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_hevy_roog\\pilot_hev_roog_m_gear_skn_01" },
            { "AWall_jumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_hevy_roog\\pilot_hev_roog_m_jumpkit_skn_01" },
            { "AWall_helmet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_heavy_helmets\\pilot_hev_helmet_v1_skn" },
            // phase
            { "PhaseShift_fbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_female_body_v1_skn01" },
            { "PhaseShift_mbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_male_b_v1_skn01" },
            { "PhaseShift_gear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_male_g_v1_skn01" },
            { "PhaseShift_viewhand", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_male_vh_v1_skn01" },
            { "PhaseShift_jumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_male_j_v1_skn01" },
            { "PhaseShift_helmet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_male_g_v1_skn01" },
            { "PhaseShift_hair", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_ged\\p_l_ged_male_alpha_v1_skn01" },
            // stim
            { "Stim_fbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_f_body" },
            { "Stim_mbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_m_body" },
            { "Stim_fgear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_f_gear" },
            { "Stim_gear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_gear" },
            { "Stim_gauntlet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_gauntlet" },
            { "Stim_fjumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_f_jumpkit" },
            { "Stim_jumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_jumpkit" },
            { "Stim_head", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_light_jester\\pilot_lit_jester_head" },
            // grapple
            { "Grapple_fbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_geist\\pilot_med_geist_f_body_skn_01" },
            { "Grapple_mbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_geist\\pilot_med_geist_m_body_skn_02" },
            { "Grapple_gear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_geist\\pilot_med_geist_gear_skn_02" },
            { "Grapple_gauntlet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_geist\\pilot_med_geist_gauntlet_skn_02" },
            { "Grapple_jumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_geist\\pilot_med_geist_jumpkit_skn_01" },
            { "Grapple_helmet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_geist\\pilot_med_geist_helmet_v2_skn_01" },
            // pulse
            { "PulseBlade_fbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_reaper\\pilot_med_reaper_f_body_skin_01" },
            { "PulseBlade_mbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_reaper\\pilot_med_reaper_m_body_skin_01" },
            { "PulseBlade_gear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_reaper\\pilot_med_reaper_m_gear_skin_01" },
            { "PulseBlade_gauntlet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_reaper\\pilot_med_reaper_m_gauntlet1_skin_01" },
            { "PulseBlade_jumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_reaper\\pilot_med_reaper_jumpkit_skin_02" },
            { "PulseBlade_helmet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_v_helmets\\pilot_med_helmet_v2_skn_02" },
            // holo
            { "HoloPilot_fbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_female_b_v1_skn01" },
            { "HoloPilot_mbody", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_b_v1_skn01" },
            { "HoloPilot_gear", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_g_v1_skn01" },
            { "HoloPilot_viewhand", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_vh_v1_skn01" },
            { "HoloPilot_jumpkit", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_j_v1_skn01" },
            { "HoloPilot_helmet", @"texture\\models\\humans\\titanpilot_gsuits\\pilot_medium_stalker\\p_m_stalker_male_he_v1_skn01" },
            // shared
            { "Cloak_head", @"texture\\models\\humans\\titanpilot_heads\\pilot_v3_head" },
            { "AWall_head", @"texture\\models\\humans\\titanpilot_heads\\pilot_v3_head" },
            { "Grapple_head", @"texture\\models\\humans\\titanpilot_heads\\pilot_v3_head" },
            { "PulseBlade_head", @"texture\\models\\humans\\titanpilot_heads\\pilot_v3_head" },
            { "HoloPilot_head", @"texture\\models\\humans\\titanpilot_heads\\pilot_v3_head" },

            //////////////
            /// TITANS ///
            //////////////
            // ion
            { "ION_Default", @"texture\\models\\titans_r2\\medium_ion\\warpaint\\warpaint_01\\t_m_ion_warpaint_skin01" },
            // ion prime
            { "PrimeION_Default", @"texture\\models\\titans_r2\\medium_ion_prime\\warpaint\\warpaint_01\\t_m_ion_prime_warpaint_skin01" },
            // tone
            { "Tone_Default", @"texture\\models\\titans_r2\\medium_tone\\warpaint\\warpaint_02\\t_m_tone_warpaint_skin02" },
            // tone prime
            { "PrimeTone_Default", @"texture\\models\\titans_r2\\medium_tone_prime\\warpaint\\warpaint_01\\t_m_tone_prime_warpaint_skin01" },
            // northstar
            { "Northstar_Default", @"texture\\models\\titans_r2\\light_northstar\\warpaint\\warpaint_01\\t_l_northstar_warpaint_skin01" },
            // northstar prime
            { "PrimeNorthstar_Default", @"texture\\models\\titans_r2\\light_northstar_prime\\warpaint\\warpaint_01\\t_l_northstar_prime_warpaint_skin01" },
            // ronin
            { "Ronin_Default", @"texture\\models\\titans_r2\\light_ronin\\warpaint\\warpaint_02\\t_l_ronin_warpaint_skin02" },
            // ronin prime
            { "PrimeRonin_Default", @"texture\\models\\titans_r2\\light_ronin_prime\\warpaint\\warpaint_01\\t_l_ronin_prime_warpaint_skin01" },
            // scorch
            { "Scorch_Default", @"texture\\models\\titans_r2\\heavy_scorch\\warpaint\\warpaint_01\\t_h_scorch_warpaint_skin01" },
            // scorch prime
            { "PrimeScorch_Default", @"texture\\models\\titans_r2\\heavy_scorch_prime\\warpaint\\warpaint_01\\t_h_scorch_prime_warpaint_skin01" },
            // legion
            { "Legion_Default", @"texture\\models\\titans_r2\\heavy_legion\\warpaint\\warpaint_01\\t_h_legion_warpaint_skin01" },
            // legion prime
            { "PrimeLegion_Default", @"texture\\models\\titans_r2\\heavy_legion_prime\\warpaint\\warpaint_01\\t_h_legion_prime_warpaint_skin01" },
            // monarch
            { "Monarch_Default", @"texture\\models\\titans_r2\\medium_vanguard\\warpaint\\warpaint_01\\t_m_vanguard_prime_warpaint_skin01" },

        };

        /// <summary>
        ///     Gets the texture path from the skintool name for the texture
        /// </summary>
        /// <param name="textureName">The skintool name for the texture</param>
        /// <returns>The texture path for RePak and the game, or an empty string if it couldn't be found</returns>
        private string TextureNameToPath(string textureName)
        {
            // find the last '_', the chars after this determine the texture type
            int lastIndex = textureName.LastIndexOf('_');

            // split textureName into the texture type and the texture name
            string txtrType = textureName[lastIndex..];
            textureName = textureName[..lastIndex];

            // check if the texture is a weapon
            if (weaponNameToPath.ContainsKey(textureName))
                return weaponNameToPath[textureName] + txtrType;

            // check if the texture is an overwritten texture
            if (nameToPathOverrides.ContainsKey(textureName + txtrType))
                return nameToPathOverrides[textureName + txtrType];
            // check if the texture is a pilot/titan/misc
            if (nameToPath.ContainsKey(textureName))
                return nameToPath[textureName] + txtrType;

            // return empty string, signifying failure
            return "";
        }
    }
}
