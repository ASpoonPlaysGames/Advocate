﻿using System;
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
        bool nogui = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            nogui = e.Args.Contains("-nogui");
            bool forceConsole = e.Args.Contains("-forceconsole");

#if DEBUG
            // always create a console if debug
            forceConsole = true;
#endif

            // get the file opened with Advocate if applicable
            string? openedFilePath = e.Args.Length != 0 ? e.Args[0] : null;

            // create the console if needed
            if (nogui || forceConsole)
            {
                AllocConsole();
            }

            if (!nogui)
            {
                // create our window
                MainWindow window = new(openedFilePath);

                // set SkinPath as soon as we make the window if it is in the command line args
                if (openedFilePath != null)
                    window.SkinPath = openedFilePath;

                // add event handling if we have a console
                if (forceConsole)
                {
                    window.MessageReceived += Console_ConversionMessage;
                }

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

                // event handling
                conv.ConversionMessage += Console_ConversionMessage;

                // convert
                conv.Convert(argDict["-outputpath"], argDict["-repakpath"], argDict["-desc"]);

                // exit with success exit code
                Environment.Exit(0);
            }
        }

        private void Console_ConversionMessage(object? sender, Conversion.ConversionMessageEventArgs e)
        {
#if !DEBUG
            // break early if message is a debug message and we arent in debug
            if (e.Type <= Conversion.MessageType.Debug)
                break;
#endif

            string level = e.Type switch
            {
                Conversion.MessageType.Debug => "DEBUG",
                Conversion.MessageType.Info => "INFO",
                Conversion.MessageType.Completion => "INFO", // just use INFO for now, maybe implement something special later?
                Conversion.MessageType.Error => "ERROR",
                // throw an error if a value is not supported
                _ => throw new NotImplementedException($"MessageType value '{e.Type}' is unsupported in Console_ConversionMessage.")
            };

            Console.WriteLine($"[{level}] {e.Message}");
        }

        [DllImport("Kernel32.dll")]
        static extern void AllocConsole();
    }
}
