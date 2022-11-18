using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Advocate.Logging
{
    /// <summary>
    ///     Handles Logging, use CreateLogFile to 
    /// </summary>
    public static class Logger
    {
        /// <summary>
        ///     The path which the <see cref="Logger"/> is writing to.
        /// </summary>
        public static string LogFilePath { get; private set; } = "";

        private static StreamWriter? logWriter;
        
        /// <summary>
        ///     Creates a log file at <paramref name="outputPath"/>.
        ///     <para>The <see cref="Logger"/> can only be writing to a single log file at once</para>
        /// </summary>
        /// <param name="outputPath"></param>
        public static void CreateLogFile(string outputPath)
        {
            if (logWriter != null)
                logWriter.Close();

            LogFilePath = $"{outputPath}/advocate-log{DateTime.Now:yyyyMMdd-THHmmss}.txt";

            logWriter = File.AppendText(LogFilePath);
            logWriter.AutoFlush = true;
        }

        /// <summary>
        ///     Event handler for receiving logs
        /// </summary>
        public static EventHandler<LoggingEventArgs>? LogReceived = OnLogReceived;

        private static void OnLogReceived(object? sender, LoggingEventArgs e)
        {
            // bonus check for null to prevent compiler warnings
            if (logWriter == null)
                // this is no longer fatal, because console logging exists
                return; //throw new Exception("Tried to log to file without calling CreateLogFile first!");

            string level = e.Type switch
            {
                MessageType.Debug => "DEBUG",
                MessageType.Info => "INFO",
                MessageType.Completion => "INFO", // just use INFO for now, maybe implement something special later?
                MessageType.Error => "ERROR",
                // throw an error if a value is not supported
                _ => throw new NotImplementedException($"MessageType value '{e.Type}' is unsupported in LogFile_ConversionMessage.")
            };

            // async write to the file
            logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {e.Message}");

        }

        /// <summary>
        ///     Logs a message with a <see cref="MessageType"/> of <see cref="MessageType.Debug"/>.
        ///     <para>If <paramref name="completionPercent"/> is not null, it sets the conversion progress.</para>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="completionPercent"></param>
        public static void Debug(string message, float? completionPercent = null)
        {
            LogReceived?.Invoke(null, new LoggingEventArgs(message, MessageType.Debug) { ConversionPercent = completionPercent });
        }

        /// <summary>
        ///     Logs a message with a <see cref="MessageType"/> of <see cref="MessageType.Info"/>.
        ///     <para>If <paramref name="completionPercent"/> is not null, it sets the conversion progress.</para>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="completionPercent"></param>
        public static void Info(string message, float? completionPercent = null)
        {
            LogReceived?.Invoke(null, new LoggingEventArgs(message, MessageType.Info) { ConversionPercent = completionPercent });
        }

        /// <summary>
        ///     Logs a message with a <see cref="MessageType"/> of <see cref="MessageType.Completion"/>.
        ///     <para>If <paramref name="completionPercent"/> is not null, it sets the conversion progress.</para>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="completionPercent"></param>
        public static void Completion(string message, float? completionPercent = null)
        {
            LogReceived?.Invoke(null, new LoggingEventArgs(message, MessageType.Completion) { ConversionPercent = completionPercent });
        }

        /// <summary>
        ///     Logs a message with a <see cref="MessageType"/> of <see cref="MessageType.Error"/>.
        ///     <para>If <paramref name="completionPercent"/> is not null, it sets the conversion progress.</para>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="completionPercent"></param>
        public static void Error(string message, float? completionPercent = null)
        {
            LogReceived?.Invoke(null, new LoggingEventArgs(message, MessageType.Error) { ConversionPercent = completionPercent });
        }
    }
}
