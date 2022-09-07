using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using Pfim;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Advocate
{
    internal class Converter : INotifyPropertyChanged 
    {
        // property change stuff that i don't really understand tbh
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged( string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // variable instantiation
        private string status = "Loading...";
        private string message = "";
        private string skinPath = "";
        private string readMePath = "";
        private string iconPath = "";
        private string authorName = "";
        private string modName = "";
        private string version = "";
        public string Status
        {
            get { return status; }
            private set { status = value; OnPropertyChanged(nameof(Status)); }
        }
        public string Message
        {
            get { return message; }
            private set { message = value; OnPropertyChanged(nameof(Message)); }
        }
        public string SkinPath
        {
            get { return skinPath; }
            set { skinPath = value; OnPropertyChanged(nameof(SkinPath)); CheckConvertStatus(); }
        }
        public string ReadMePath
        {
            get { return readMePath; }
            set { readMePath = value; OnPropertyChanged(nameof(ReadMePath)); CheckConvertStatus(); }
        }
        public string IconPath
        {
            get { return iconPath; }
            set { iconPath = value; OnPropertyChanged(nameof(IconPath)); CheckConvertStatus(); }
        }
        public string AuthorName
        {
            get { return authorName; }
            set { authorName = value; OnPropertyChanged(nameof(AuthorName)); CheckConvertStatus(); }
        }
        public string ModName
        {
            get { return modName; }
            set { modName = value; OnPropertyChanged(nameof(ModName)); CheckConvertStatus(); }
        }
        public string Version
        {
            get { return version; }
            set { version = value; OnPropertyChanged(nameof(Version)); CheckConvertStatus(); }
        }


        // conversion progress tracking variables
        private int currentConvertStep = 0;
        private int numConvertSteps = 5;
        // checking
        // clearing old file structure if exists
        // unzipping zip to new folder
        // creating file structure
        // creating icon.png
        // create README.md
        // create manifest.json
        // create mod.json
        // create map.json
        // move textures to temp folder for packing
        // pack using RePak.exe
        // zip up result
        // move result out of temp folder
 
        public float ConvertProgress
        {
            get { return 100 * ((float)currentConvertStep / (float)numConvertSteps); }
        }

        private void ConvertTaskComplete()
        {
            currentConvertStep++;
            OnPropertyChanged(nameof(ConvertProgress));
        }

        private async void ConversionComplete(HandyControl.Controls.ProgressButton button, System.Windows.DependencyProperty styleProperty)
        {
            currentConvertStep = 0;
            OnPropertyChanged(nameof(ConvertProgress));
            Status = "Complete!";
            Message = "Converted Successfully!";

            button.SetResourceReference(styleProperty, "ProgressButtonSuccess");
            await ChangeStyle_Delayed(button, styleProperty, 3000, "ProgressButtonPrimary");
            Status = "Convert Skin(s)";
            CheckConvertStatus();
        }

        private async void ConversionFailed(HandyControl.Controls.ProgressButton button, System.Windows.DependencyProperty styleProperty, string reason, bool reset = false)
        {
            currentConvertStep = 0;
            OnPropertyChanged(nameof(ConvertProgress));
            Status = "Failed!";
            Message = reason;

            button.SetResourceReference(styleProperty, "ProgressButtonDanger");
            await ChangeStyle_Delayed(button, styleProperty, 3000, "ProgressButtonPrimary");
            Status = "Convert Skin(s)";
            if (reset)
            {
                CheckConvertStatus();
            }
        }

        /// <summary>
        /// Checks the status of the various settings for the conversion
        /// </summary>
        /// <returns>true if no issues are found</returns>
        public bool CheckConvertStatus()
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
            if (!Regex.Match(Version, "\\d+.\\d+.\\d+").Success)
            {
                Message = "Error: Version is invalid! (Example: 1.0.0)";
                return false;
            }

            // everything looks good
            Message = "Ready!";
            return true;
        }

        public async Task ChangeStyle_Delayed(HandyControl.Controls.ProgressButton button, System.Windows.DependencyProperty styleProperty, int time, string style)
        {
            await Task.Delay(time);
            button.SetResourceReference(styleProperty, style);
        }

        public async void Convert(HandyControl.Controls.ProgressButton button, System.Windows.DependencyProperty styleProperty)
        {
            if (!CheckConvertStatus())
            {
                // something is wrong with the setup, just exit
                ConversionFailed(button, styleProperty, Message);
                return;
            }
            ConvertTaskComplete();

            // make some variables that are useful at various points
            string tempFolderPath = Path.GetFullPath("./Temp");
            string skinTempFolderPath = Path.GetFullPath("./Temp/Skin");
            string modTempFolderPath = Path.GetFullPath("./Temp/Mod");
            string repakTempFolderPath = Path.GetFullPath("./Temp/RePak");

            // try convert stuff, if we get a weird exception, don't crash preferably
            try
            {
                /////////////////////////////
                // create temp directories //
                /////////////////////////////

                // directory for unzipped file
                Directory.CreateDirectory(skinTempFolderPath);

                // directory for TS-compliant mod
                Directory.CreateDirectory(modTempFolderPath);

                // directory for RePak things
                Directory.CreateDirectory(repakTempFolderPath);

                ConvertTaskComplete();

                ///////////////////////////////
                // unzip skin to temp folder //
                ///////////////////////////////

                ZipFile.ExtractToDirectory(SkinPath, skinTempFolderPath, true);

                ConvertTaskComplete();

                ////////////////////////////////////
                // create temp mod file structure //
                ////////////////////////////////////

                Directory.CreateDirectory(modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\paks");

                ConvertTaskComplete();

                /////////////////////
                // create icon.png //
                /////////////////////

                if (IconPath == "")
                {
                    // fuck you, im using the col of the first folder i find, shouldve specified an icon path
                    string[] skinPaths = Directory.GetDirectories(skinTempFolderPath);
                    if (skinPaths.Length == 0)
                    {
                        ConversionFailed(button, styleProperty, "No Skins found in zip!");
                        return;
                    }
                    string[] resolutions = Directory.GetDirectories(skinPaths[0]);
                    if (resolutions.Length == 0)
                    {
                        ConversionFailed(button, styleProperty, "No Skins found in zip!");
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
                        ConversionFailed(button, styleProperty, "No valid image resolutions found in zip!");
                        return;
                    }

                    string[] files = Directory.GetFiles(skinPaths[0] + "\\" + highestRes.ToString());
                    if (files.Length == 0)
                    {
                        ConversionFailed(button, styleProperty, "No files in highest resolution folder!");
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
                        ConversionFailed(button, styleProperty, "No _col texture found in highest resolution folder");
                        return;
                    }

                    if(!DdsToPng(colPath, modTempFolderPath + "\\icon.png"))
                    {
                        ConversionFailed(button, styleProperty, "Failed to convert dds to png for icon!");
                        return;
                    }
                }
                else
                {
                    // check that png is correct size
                    Image img = Image.FromFile(IconPath);
                    if (img.Width != 256 || img.Height != 256)
                    {
                        ConversionFailed(button, styleProperty, "Icon must be 256x256!");
                        return;
                    }
                    // copy png over
                    File.Copy(IconPath, modTempFolderPath + "\\icon.png");
                }

                ConvertTaskComplete();

                //////////////////////
                // create README.md //
                //////////////////////

                if (ReadMePath == "")
                {
                    // todo, maybe add some basic default text here idk
                    File.WriteAllText(modTempFolderPath + "\\README.md", "");
                }
                else
                {
                    File.Copy(ReadMePath, modTempFolderPath + "\\README.md");
                }

                ConvertTaskComplete();

                //////////////////////////
                // create manifest.json //
                //////////////////////////
                string manifest = string.Format("{{\n\"name\":\"{0}\",\n\"version_number\":\"{1}\",\n\"website_url\":\"\",\n\"dependencies\":[],\n\"description\":\"{2}\"\n}}", ModName, Version, string.Format("Skin made by {0}", AuthorName));
                File.WriteAllText(modTempFolderPath + "\\manifest.json", manifest);

                ConvertTaskComplete();

                /////////////////////
                // create mod.json //
                /////////////////////

                string modJson = string.Format("{{\n\"Name\": \"{0}\",\n\"Description\": \"\",\n\"Version\": \"{1}\",\n\"LoadPriority\": 1,\n\"ConVars\":[],\n\"Scripts\":[],\n\"Localisation\":[]\n}}", AuthorName + "." + ModName, Version);
                File.WriteAllText(modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\mod.json", modJson);

                ConvertTaskComplete();

                //////////////////////////////////////////////////////////////////
                // create map.json and move textures to temp folder for packing //
                //////////////////////////////////////////////////////////////////

                string map = string.Format("{{\n\"name\":\"{0}\",\n\"assetsDir\":\"{1}\",\n\"outputDir\":\"{2}\",\n\"version\": 7,\n\"files\":[\n", ModName, (repakTempFolderPath + "\\assets").Replace('\\', '/'), (modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\paks").Replace('\\', '/'));
                // add texture entries
                List<string> textures = new();
                bool isFirst = true;
                foreach(string skinPath in Directory.GetDirectories(skinTempFolderPath))
                {
                    foreach (string resolution in Directory.GetDirectories(skinPath).OrderBy(path => int.Parse(Path.GetFileName(path))))
                    {
                        if (int.TryParse(Path.GetFileName(resolution), out int res))
                        {
                            foreach (string texture in Directory.GetFiles(resolution))
                            {
                                // move texture to temp folder for packing
                                // convert from skin tool syntax to actual texture path, gotta be hardcoded because pain
                                string texturePath = TextureNameToPath(Path.GetFileNameWithoutExtension(texture));
                                if (texturePath == "")
                                {
                                    ConversionFailed(button, styleProperty, "Failed to convert texture '" + Path.GetFileNameWithoutExtension(texture) + "')");
                                    return;
                                }

                                // avoid duplicate textures in the json
                                if (!textures.Contains(texturePath))
                                {
                                    if (!isFirst)
                                        map += ",\n";
                                    map += "{\n\"$type\":\"txtr\",\n\"path\":\"" + texturePath + "\",\n\"disableStreaming\":true\n}";
                                    textures.Append(texturePath);
                                }
                                isFirst = false;
                                // copy file
                                Directory.CreateDirectory(Directory.GetParent(repakTempFolderPath + "\\assets\\" + texturePath + ".dds").FullName);
                                File.Copy(texture, repakTempFolderPath + "\\assets\\" + texturePath + ".dds", true);
                            }
                        }
                    }
                }

                // end the json
                map += "\n]\n}";
                File.WriteAllText(repakTempFolderPath + "\\map.json", map);


                //////////////////////////
                // pack using RePak.exe //
                //////////////////////////
                //var sb = new StringBuilder();

                Process P = new Process();

                //P.StartInfo.RedirectStandardOutput = true;
                //P.StartInfo.RedirectStandardError = true;
                //P.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
                //P.ErrorDataReceived += (sender, args) => sb.AppendLine(args.Data);
                //P.StartInfo.UseShellExecute = false;
                P.StartInfo.FileName = Properties.Settings.Default.RePakPath;
                P.StartInfo.Arguments = "\"" + repakTempFolderPath + "\\map.json\"";
                P.Start();
                //P.BeginOutputReadLine();
                //P.BeginErrorReadLine();
                P.WaitForExit();

                if (P.ExitCode == 1)
                {
                    ConversionFailed(button, styleProperty, "RePak failed to pack the rpak!");
                    return;
                }

                //////////////////////
                // create rpak.json //
                //////////////////////

                string rpakjson = string.Format("{{\n\"Preload\":\n{{\n\"{0}\":true\n}}\n}}", ModName + ".rpak");

                File.WriteAllText(modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\paks\\rpak.json", rpakjson);

                ///////////////////
                // zip up result //
                ///////////////////

                ZipFile.CreateFromDirectory(modTempFolderPath, tempFolderPath + "\\" + AuthorName + "." + ModName + ".zip");

                ////////////////////////////////////
                // move result out of temp folder //
                ////////////////////////////////////

                File.Move(tempFolderPath + "\\" + AuthorName + "." + ModName + ".zip", Properties.Settings.Default.OutputPath + "\\" + AuthorName + "." + ModName + ".zip");
            }
            catch (Exception ex)
            {
                // create message box showing the full error
                MessageBoxButton msgButton = MessageBoxButton.OK;
                MessageBoxImage msgIcon = MessageBoxImage.Error;
                MessageBox.Show("There was an unhandled error during conversion!\n\n" + ex.Message + "\n\n" + ex.StackTrace, "Conversion Error", msgButton, msgIcon);

                ConversionFailed(button, styleProperty, "Unknown Error!", true);
                return;
            }
            finally
            {
                /////////////
                // cleanup //
                /////////////

                Directory.Delete(tempFolderPath, true);
            }


            // everything is done and good
            ConversionComplete(button, styleProperty);
        }

        private bool DdsToPng(string imagePath, string outputPath)
        {
            // yoinked from pfim usage example
            using (var image = Pfimage.FromFile(imagePath))
            {
                PixelFormat format;

                // Convert from Pfim's backend agnostic image format into GDI+'s image format
                switch (image.Format)
                {
                    case Pfim.ImageFormat.Rgba32:
                        format = PixelFormat.Format32bppArgb;
                        break;
                    default:
                        // see the sample for more details
                        throw new NotImplementedException();
                }

                // Pin pfim's data array so that it doesn't get reaped by GC, unnecessary
                // in this snippet but useful technique if the data was going to be used in
                // control like a picture box
                var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
                try
                {
                    var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                    var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, data);
                    // resize the bitmap before saving it
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

        // these have to be hardcoded because skin tool just hardcodes in offsets afaik
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
        // pilot overrides
        private readonly Dictionary<string, string> pilotNameOverridesToPath = new()
        {
            // cloak
            // A-wall
            // phase
            { "PhaseShift_fbody_ilm", "" },
            // stim
            // grapple
            // pulse
            // holo
        };
        private readonly Dictionary<string, string> pilotNameToPath = new()
        {
            // cloak
            { "Cloak_fbody", "" },
            // A-wall
            // phase
            // stim
            // grapple
            // pulse
            // holo
        };
        private string TextureNameToPath(string textureName)
        {
            int lastIndex = textureName.LastIndexOf('_');

            string txtrType = textureName.Substring(lastIndex, textureName.Length - lastIndex);
            textureName = textureName.Substring(0, lastIndex);

            if (weaponNameToPath.ContainsKey(textureName))
                return weaponNameToPath[textureName] + txtrType;

            if (pilotNameOverridesToPath.ContainsKey(textureName + txtrType))
                return pilotNameOverridesToPath[textureName + txtrType];
            if (pilotNameToPath.ContainsKey(textureName))
                return pilotNameToPath[textureName];

            return "";
        }
    }
}
