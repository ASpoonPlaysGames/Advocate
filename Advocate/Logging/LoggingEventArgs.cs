using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advocate.Logging
{
    /// <summary>
    ///     Holds the type of message,
    ///     used for determining behaviour when logging
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///     <para>Indicates that the message should only
        ///     be shown when running in Debug.</para>
        ///     Not shown in gui.
        /// </summary>
        Debug,
        /// <summary>
        ///     <para>Default message type.</para>
        ///     <para>For general purpose messages to the user in gui and console.</para>
        ///     Shown in gui. See <see cref="MainWindow.HandleConversionMessage"/> for implementation.
        /// </summary>
        Info,
        /// <summary>
        ///     <para>Indicates that the skin conversion is complete.</para>
        ///     Shown in gui. See <see cref="MainWindow.HandleConversionMessage"/> for implementation.
        /// </summary>
        Completion,
        /// <summary>
        ///     <para>Indicates an error during skin conversion.</para>
        ///     Shown in gui. See <see cref="MainWindow.HandleConversionMessage"/> for implementation.
        /// </summary>
        Error
    }

    /// <summary>
    ///     Holds information about a conversion message,
    ///     used for logging to the console and updating the gui.
    /// </summary>
    public class LoggingEventArgs : EventArgs
    {
        /// <summary>
        ///     The message to the user. May be null.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     The type of message that this is.
        ///     <para><see cref="MessageType.Debug"/> messages will not be shown in the gui, and will only be logged to the console if compiled in debug</para>
        /// </summary>
        public MessageType Type { get; }

        /// <summary>
        ///     How complete the conversion is, represented by a percentage.
        /// </summary>
        /// <value>
        ///     A float value between 0 and 100 (inclusive).
        /// </value>
        public float? ConversionPercent { get; init; }

        /// <summary>
        ///     Basic constructor for <see cref="LoggingEventArgs"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public LoggingEventArgs(string message, MessageType type) { Message = message; Type = type; }
    }
}
