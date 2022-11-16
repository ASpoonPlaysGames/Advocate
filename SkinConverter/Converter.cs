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
using System.Runtime.InteropServices;
using System.Diagnostics;
using Pfim;

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
        private const int numConvertSteps = 13;
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
 
        /// <summary>
        /// The current conversion progress as a percentage
        /// </summary>
        public float ConvertProgress
        {
            get { return 100 * ((float)currentConvertStep / (float)numConvertSteps); }
        }

        /// <summary>
        /// Increments the current conversion step, for tracking progress
        /// </summary>
        private void ConvertTaskComplete()
        {
            // increment the currentConvertStep
            currentConvertStep++;
            // update progress bar
            OnPropertyChanged(nameof(ConvertProgress));
        }

        /// <summary>
        /// Changes a button to the success style and message, for when conversion is complete
        /// </summary>
        /// <param name="button">The button which has it's style changed</param>
        /// <param name="styleProperty">The style property to change</param>
        private async void ConversionComplete(HandyControl.Controls.ProgressButton button, DependencyProperty styleProperty)
        {
            // reset the currentConvertStep to 0, so that the progress bar is empty
            currentConvertStep = 0;
            // update progress bar
            OnPropertyChanged(nameof(ConvertProgress));
            // set message and status to show user that conversion is complete
            Status = "Complete!";
            Message = "Converted Successfully!";
            // change button style, using Invoke due to async
            button.Dispatcher.Invoke(() =>
            {
                button.SetResourceReference(styleProperty, "ProgressButtonSuccess");
            });
            // wait 10 seconds before resetting back to normal
            await ChangeStyle_Delayed(button, styleProperty, 10000, "ProgressButtonPrimary");
            Status = "Convert Skin(s)";
            // re-check the conversion status
            CheckConvertStatus();
        }

        /// <summary>
        /// Changes a button to a failure style, and show the reason for doing so for 3 seconds
        /// </summary>
        /// <param name="button">The button which has it's style changed</param>
        /// <param name="styleProperty">The style property to change</param>
        /// <param name="reason">The reason for failure</param>
        /// <param name="reset">Whether or not to check the conversion status after the style has reset</param>
        private async void ConversionFailed(HandyControl.Controls.ProgressButton button, DependencyProperty styleProperty, string reason, bool reset = false)
        {
            // reset the currentConvertStep to 0, so that the progress bar is empty
            currentConvertStep = 0;
            // update progress bar
            OnPropertyChanged(nameof(ConvertProgress));
            // set message and status to show user the error
            Status = "Failed!";
            Message = reason;
            // change button style, using Invoke due to async
            button.Dispatcher.Invoke(() =>
            {
                button.SetResourceReference(styleProperty, "ProgressButtonDanger");
            });
            // wait 3 seconds before resetting back to normal
            await ChangeStyle_Delayed(button, styleProperty, 3000, "ProgressButtonPrimary");
            Status = "Convert Skin(s)";
            // if needed, check the conversion status again
            if (reset)
            {
                CheckConvertStatus();
            }
        }

        /// <summary>
        /// Waits a specified amount of time, before changing the style of the button
        /// </summary>
        /// <param name="button">The button which has it's style changed</param>
        /// <param name="styleProperty">The style property to change</param>
        /// <param name="time">How long to wait before changing style, in ms</param>
        /// <param name="style">The style name</param>
        /// <returns>A Task to be used in async stuff</returns>
        public static async Task ChangeStyle_Delayed(HandyControl.Controls.ProgressButton button, DependencyProperty styleProperty, int time, string style)
        {
            await Task.Delay(time);
            button.Dispatcher.Invoke( () =>
            {
               button.SetResourceReference(styleProperty, style);
            });
        }

        /// <summary>
        /// Checks the status of the various settings for the conversion
        /// </summary>
        /// <returns>true if no issues are found</returns>
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
                MessageBox.Show("There was an unhandled error during checking!\n\n" + ex.Message + "\n\n" + ex.StackTrace, "Conversion Checking Error", msgButton, msgIcon);

                // exit out of the conversion
                Message = "Unknown Error!";
                return false;
            }
        }

        public void Convert(HandyControl.Controls.ProgressButton button, DependencyProperty styleProperty)
        {
            // check the conversion status before converting
            if (!CheckConvertStatus())
            {
                // something is wrong with the setup, just exit
                ConversionFailed(button, styleProperty, Message);
                return;
            }
            // move progress bar
            ConvertTaskComplete();

            // initialise various path variables, just because they are useful

            // the temp path is appended with the current date and time to prevent duplicates
            string tempFolderPath = Path.GetTempPath() + "/Advocate/" + DateTime.Now.ToString("yyyyMMdd-THHmmss");
            string skinTempFolderPath = Path.GetFullPath(tempFolderPath + "/Skin");
            string modTempFolderPath = Path.GetFullPath(tempFolderPath + "/Mod");
            string repakTempFolderPath = Path.GetFullPath(tempFolderPath + "/RePak");

            // try convert stuff, if we get a weird exception, don't crash preferably
            try
            {
                /////////////////////////////
                // create temp directories //
                /////////////////////////////

                Message = "Creating temporary directories...";

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
                Message = "Unzipping skin...";

                // try to extract the zip, catch any errors and just exit, sometimes we get bad zips, non-zips, etc. etc.
                try
                {
                    ZipFile.ExtractToDirectory(SkinPath, skinTempFolderPath, true);
                }
                catch (InvalidDataException)
                {
                    ConversionFailed(button, styleProperty, "Unable to unzip skin!");
                    return;
                }

                // move progress bar
                ConvertTaskComplete();

                ////////////////////////////////////
                // create temp mod file structure //
                ////////////////////////////////////

                // set the message for the new conversion step
                Message = "Creating mod file structure...";

                // create the bare-bones folder structure for the mod
                Directory.CreateDirectory(modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\paks");

                // move progress bar
                ConvertTaskComplete();

                /////////////////////
                // create icon.png //
                /////////////////////

                // if IconPath is an empty string, we try and generate the icon from a _col texture (thunderstore requires an icon)
                if (IconPath == "")
                {
                    // set the message for the new conversion step
                    Message = "Generating icon.png...";
                    // fuck you, im using the col of the first folder i find, shouldve specified an icon path
                    string[] skinPaths = Directory.GetDirectories(skinTempFolderPath);
                    if (skinPaths.Length == 0)
                    {
                        ConversionFailed(button, styleProperty, "Couldn't generate icon.png: No Skins found in zip!");
                        return;
                    }
                    string[] resolutions = Directory.GetDirectories(skinPaths[0]);
                    if (resolutions.Length == 0)
                    {
                        ConversionFailed(button, styleProperty, "Couldn't generate icon.png: No Skins found in zip!");
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
                        ConversionFailed(button, styleProperty, "Couldn't generate icon.png: No valid image resolutions found in zip!");
                        return;
                    }

                    string[] files = Directory.GetFiles(skinPaths[0] + "\\" + highestRes.ToString());
                    if (files.Length == 0)
                    {
                        ConversionFailed(button, styleProperty, "Couldn't generate icon.png: No files in highest resolution folder!");
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
                        ConversionFailed(button, styleProperty, "Couldn't generate icon.png: No _col texture found in highest resolution folder!");
                        return;
                    }

                    if(!DdsToPng(colPath, modTempFolderPath + "\\icon.png"))
                    {
                        ConversionFailed(button, styleProperty, "Couldn't generate icon.png: Failed to convert dds to png!");
                        return;
                    }
                }
                else
                {
                    // set the message for the new conversion step
                    Message = "Copying icon.png...";
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

                // move progress bar
                ConvertTaskComplete();

                //////////////////////
                // create README.md //
                //////////////////////

                // if the path is an empty string, we should be generating a README.md (for now it's just blank)
                if (ReadMePath == "")
                {
                    // set the message for the new conversion step
                    Message = "Generating README.md...";
                    // todo, maybe add some basic default text here idk
                    File.WriteAllText(modTempFolderPath + "\\README.md", "");
                }
                else
                {
                    // set the message for the new conversion step
                    Message = "Copying README.md...";
                    // copy the file over to the temp folder
                    File.Copy(ReadMePath, modTempFolderPath + "\\README.md");
                }

                // move progress bar
                ConvertTaskComplete();

                //////////////////////////
                // create manifest.json //
                //////////////////////////

                // set the message for the new conversion step
                Message = "Writing manifest.json...";

                string manifest = string.Format("{{\n\"name\":\"{0}\",\n\"version_number\":\"{1}\",\n\"website_url\":\"\",\n\"dependencies\":[],\n\"description\":\"{2}\"\n}}", ModName.Replace(' ', '_'), Version, string.Format("Skin made by {0}", AuthorName));
                File.WriteAllText(modTempFolderPath + "\\manifest.json", manifest);

                // move progress bar
                ConvertTaskComplete();

                /////////////////////
                // create mod.json //
                /////////////////////

                // set the message for the new conversion step
                Message = "Writing mod.json...";

                string modJson = string.Format("{{\n\"Name\": \"{0}\",\n\"Description\": \"\",\n\"Version\": \"{1}\",\n\"LoadPriority\": 1,\n\"ConVars\":[],\n\"Scripts\":[],\n\"Localisation\":[]\n}}", AuthorName + "." + ModName, Version);
                File.WriteAllText(modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\mod.json", modJson);

                // move progress bar
                ConvertTaskComplete();

                //////////////////////////////////////////////////////////////////
                // create map.json and move textures to temp folder for packing //
                //////////////////////////////////////////////////////////////////

                // set the message for the new conversion step
                Message = "Copying textures...";

                string map = string.Format("{{\n\"name\":\"{0}\",\n\"assetsDir\":\"{1}\",\n\"outputDir\":\"{2}\",\n\"version\": 7,\n\"files\":[\n", ModName, (repakTempFolderPath + "\\assets").Replace('\\', '/'), (modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\paks").Replace('\\', '/'));
                // this tracks the textures that we have already added to the json, so we can avoid duplicates in there
                List<string> textures = new();
                bool isFirst = true;
                foreach(string skinPath in Directory.GetDirectories(skinTempFolderPath))
                {
                    // some skins have random files and folders in here, like images and stuff, so I have to do sorting in an annoying way
                    List<string> parsedDirs = new();
                    foreach(string dir in Directory.GetDirectories(skinPath))
                    {
                        // only add to the list of dirs
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
                                if (texturePath == "")
                                {
                                    ConversionFailed(button, styleProperty, "Failed to convert texture '" + Path.GetFileNameWithoutExtension(texture) + "')");
                                    return;
                                }

                                // avoid duplicate textures in the json
                                if (!textures.Contains(texturePath))
                                {
                                    // dont add a comma on the first one
                                    if (!isFirst)
                                        map += ",\n";
                                    map += $"{{\n\"$type\":\"txtr\",\n\"path\":\"{texturePath}\",\n\"disableStreaming\":true,\n\"saveDebugName\":true\n}}";
                                    // add texture to tracked textures
                                    textures.Add(texturePath);
                                }
                                isFirst = false;
                                // copy file
                                Directory.CreateDirectory(Directory.GetParent($"{repakTempFolderPath}\\assets\\{texturePath}.dds").FullName);
                                
                                // instantiate dds handler, pass it the texture path
                                DdsHandler handler = new(texture);
                                // convert the dds image to one that works with RePak
                                handler.Convert();
                                // save the file where RePak expects it to be
                                handler.Save($"{repakTempFolderPath}\\assets\\{texturePath}.dds");
                            }
                        }
                    }
                }

                // end the json
                map += "\n]\n}";
                File.WriteAllText(repakTempFolderPath + "\\map.json", map);

                // move progress bar
                ConvertTaskComplete();

                //////////////////////////
                // pack using RePak.exe //
                //////////////////////////

                // set the message for the new conversion step
                Message = "Packing using RePak...";


                // create the process for RePak


                //var sb = new StringBuilder();

                Process P = new();

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

                // wait for RePak to finish
                P.WaitForExit();

                // currently, RePak always uses exitcode 1 for failure, if we implement more error codes then I'll probably give a more detailed error here
                if (P.ExitCode == 1)
                {
                    ConversionFailed(button, styleProperty, "RePak failed to pack the rpak!");
                    return;
                }

                // move progress bar
                ConvertTaskComplete();

                //////////////////////
                // create rpak.json //
                //////////////////////

                // set the message for the new conversion step
                Message = "Generating rpak.json...";

                // we can just preload our rpak, since it should only contain textures
                string rpakjson = string.Format("{{\n\"Preload\":\n{{\n\"{0}\":true\n}}\n}}", ModName + ".rpak");

                File.WriteAllText(modTempFolderPath + "\\mods\\" + AuthorName + "." + ModName + "\\paks\\rpak.json", rpakjson);

                // move progress bar
                ConvertTaskComplete();

                ///////////////////
                // zip up result //
                ///////////////////

                // set the message for the new conversion step
                Message = "Zipping mod...";

                // create the zip file from the mod temp path
                ZipFile.CreateFromDirectory(modTempFolderPath, tempFolderPath + "\\" + AuthorName + "." + ModName + ".zip");

                // move progress bar
                ConvertTaskComplete();

                ////////////////////////////////////
                // move result out of temp folder //
                ////////////////////////////////////

                // set the message for the new conversion step
                Message = "Moving zip to output folder...";

                // move the zip file we created to the output folder
                File.Move(tempFolderPath + "\\" + AuthorName + "." + ModName + ".zip", Properties.Settings.Default.OutputPath + "\\" + AuthorName + "." + ModName + ".zip", true);

                // move progress bar
                ConvertTaskComplete();

                /////////////
                // cleanup //
                /////////////

                Message = "Cleaning up...";

                // delete temp folders
                if (Directory.Exists(tempFolderPath))
                    Directory.Delete(tempFolderPath, true);
            }
            catch (Exception ex)
            {
                // create message box showing the full error
                MessageBoxButton msgButton = MessageBoxButton.OK;
                MessageBoxImage msgIcon = MessageBoxImage.Error;
                MessageBox.Show("There was an unhandled error during conversion!\n\n" + ex.Message + "\n\n" + ex.StackTrace, "Conversion Error", msgButton, msgIcon);

                // exit out of the conversion
                ConversionFailed(button, styleProperty, "Unknown Error!", true);
                return;
            }

            // everything is done and good
            // move progress bar
            ConversionComplete(button, styleProperty);
        }

        /// <summary>
        /// Converts a .dds file to a .png file with dimensions of 256x256 (thunderstore compliant)
        /// </summary>
        /// <param name="imagePath">The path of the input image (.dds)</param>
        /// <param name="outputPath">The path of the output image (.png)</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool DdsToPng(string imagePath, string outputPath)
        {
            // this code is just yoinked from pfim usage example
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
            { "PrimeSword_Default", @"texture\\models\\weapons_r2\\titan_prime_sword\\titan_prime_sword_01" },
            { "SwordPuls_Default", @"texture\\models\\weapons_r2\\titan_prime_sword\\titan_prime_sword_01" }, // copied from PrimeSword since an old skin named it this way before skin tool support
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
        /// Gets the texture path from the skintool name for the texture
        /// </summary>
        /// <param name="textureName">The skintool name for the texture</param>
        /// <returns>The texture path for RePak and the game, or an empty string if it couldn't be found</returns>
        private string TextureNameToPath(string textureName)
        {
            // find the last '_', the chars after this determine the texture type
            int lastIndex = textureName.LastIndexOf('_');

            // split textureName into the texture type and the texture name
            string txtrType = textureName.Substring(lastIndex, textureName.Length - lastIndex);
            textureName = textureName.Substring(0, lastIndex);

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
