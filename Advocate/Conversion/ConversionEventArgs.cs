using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advocate.Conversion
{
    public enum MessageType
    {
        Debug,
        Info,
        Completion,
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
