using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Advocate
{
    public static class Logger
    {

        public static string LogFilePath { get; private set; }
        private static StreamWriter? logWriter;


        public static void CreateLogFile(string outputPath)
        {
            if (logWriter != null)
                logWriter.Close();

            LogFilePath = $"{outputPath}/advocate-log{DateTime.Now:yyyyMMdd-THHmmss}.txt";

            logWriter = File.AppendText(LogFilePath);
            logWriter.AutoFlush = true;
        }

        public static EventHandler<Conversion.ConversionMessageEventArgs> ConversionMessage = OnMessage;

        private static void OnMessage(object? sender, Conversion.ConversionMessageEventArgs e)
        {
            // bonus check for null to prevent compiler warnings
            if (logWriter == null)
                throw new Exception("Tried to log to file without calling CreateLogFile first!");

            string level = e.Type switch
            {
                Conversion.MessageType.Debug => "DEBUG",
                Conversion.MessageType.Info => "INFO",
                Conversion.MessageType.Completion => "INFO", // just use INFO for now, maybe implement something special later?
                Conversion.MessageType.Error => "ERROR",
                // throw an error if a value is not supported
                _ => throw new NotImplementedException($"MessageType value '{e.Type}' is unsupported in LogFile_ConversionMessage.")
            };

            // async write to the file
            logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {e.Message}");

        }
    }
}
