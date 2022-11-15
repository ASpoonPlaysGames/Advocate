using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advocate.Conversion
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
        ///     Shown in gui. See <see cref="MainWindow.MainWindow_OnConversionMessage"/> for implementation.
        /// </summary>
        Info,
        /// <summary>
        ///     <para>Indicates that the skin conversion is complete.</para>
        ///     Shown in gui. See <see cref="MainWindow.MainWindow_OnConversionComplete"/> for implementation.
        /// </summary>
        Completion,
        /// <summary>
        ///     <para>Indicates an error during skin conversion.</para>
        ///     Shown in gui. See <see cref="MainWindow.MainWindow_OnConversionError"/> for implementation.
        /// </summary>
        Error
    }
    public class ConversionMessageEventArgs : EventArgs
    {
        public string? Message { get; init; }
        public MessageType Type { get; init; }
        public ConversionMessageEventArgs(string? message) { Message = message; }
    }
    public class ConversionProgressEventArgs : EventArgs
    {
        public float ConversionPercent { get; init; }
        public ConversionProgressEventArgs(float conversionPercent) { ConversionPercent = conversionPercent; }
    }
}
