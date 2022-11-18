using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Advocate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private bool nogui = false;

        /// <summary>
        ///     Called on App startup, the "entry point" of the program.
        ///     <para>Creates the <see cref="MainWindow"/> if not running with -nogui</para>
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="Exception"></exception>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            nogui = e.Args.Contains("-nogui");
            if (nogui)
            {
                Logging.Logger.Debug($"Found 'nogui' in commandline arguments, running without gui");
            }
            bool forceConsole = e.Args.Contains("-forceconsole");

#if DEBUG
            // always have a console if debug
            forceConsole = true;
#endif

            // get the file opened with Advocate if applicable
            string? openedFilePath = e.Args.Length != 0 ? e.Args[0] : null;
            if (openedFilePath != null)
            {
                Logging.Logger.Debug($"Found target file in commandline arguments: '{openedFilePath}'");
            }

            // try to attach to a console from the parent program if it exists
            AttachConsole(-1);

            // create a console window if we must
            if (forceConsole && GetConsoleWindow() == IntPtr.Zero)
            {
                AllocConsole();
            }

            // if we have a console, use it
            if (GetConsoleWindow() != IntPtr.Zero)
                Logging.Logger.LogReceived += Console_OnConversionMessage;

            if (!nogui)
            {
                // create our window
                MainWindow window = new(openedFilePath);

                // set SkinPath as soon as we make the window if it is in the command line args
                if (openedFilePath != null)
                    window.SkinPath = openedFilePath;

                // show the window
                window.ShowDialog();
            }
            else
            {
                // get command line arguments and set as args for converter
                if (openedFilePath == null)
                    throw new Exception("Invalid Usage: No arguments were passed");

                // disctionary containing all known command line args
                Dictionary<string, string> argDict = new()
                {
                    { "-author", "" },
                    { "-name", "" },
                    { "-version", "" },
                    { "-readme", "" },
                    { "-icon", "" },
                    { "-outputpath", "" },
                    { "-repakpath", "" },
                    { "-desc", "" },
                };


                bool isFirst = true;
                foreach (string s in e.Args)
                {
                    // skip first arg and -nogui
                    if (isFirst || s == "-nogui")
                    {
                        isFirst = false;
                        continue;
                    }

                    int i = s.IndexOf('=');
                    if (i == -1)
                    {
                        throw new Exception($"Invalid Usage: Argument '{s}' does not have an associated value! (expected '{s}=value')");
                    }
                    string key = s[..i];
                    string val = s[++i..];

                    if (argDict.ContainsKey(key))
                    {
                        Logging.Logger.Debug($"Found key '{key}' in commandline arguments with value {val}");
                        argDict[key] = val;
                    }
                    else
                    {
                        throw new Exception($"Invalid Usage: Unknown argument '{s}'");
                    }
                }

                // check for required args
                foreach (string s in new string[] { "-author", "-name", "-version", "-repakpath", "-desc", "-outputpath" })
                {
                    if (argDict[s] != "")
                        continue;
                    throw new Exception($"Invalid Usage: Missing required argument '{s}'");
                }

                // create converter
                Conversion.Converter conv = new(openedFilePath, argDict["-author"], argDict["-name"], argDict["-version"], argDict["-readme"], argDict["-icon"]);

                // convert
                bool success = conv.Convert(argDict["-outputpath"], argDict["-repakpath"], argDict["-desc"], nogui);

                // exit
                Environment.Exit(success ? 0 : 1);
            }
        }

        private void Console_OnConversionMessage(object? sender, Logging.LoggingEventArgs e)
        {
#if !DEBUG
            // break early if message is a debug message and we arent in debug
            if (e.Type <= Logging.MessageType.Debug)
                return;
#endif

            string level = e.Type switch
            {
                Logging.MessageType.Debug => "DEBUG",
                Logging.MessageType.Info => "INFO",
                Logging.MessageType.Completion => "INFO", // just use INFO for now, maybe implement something special later?
                Logging.MessageType.Error => "ERROR",
                // throw an error if a value is not supported
                _ => throw new NotImplementedException($"MessageType value '{e.Type}' is unsupported in Console_ConversionMessage.")
            };

            Console.WriteLine($"[{level}]{(e.ConversionPercent == null ? "" : $" [{(int)e.ConversionPercent,3}%]")} {e.Message}");
        }

        [DllImport("Kernel32.dll")]
        static extern void AllocConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
    }
}
