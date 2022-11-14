using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advocate.Conversion
{
    public class ConversionMessageEventArgs : EventArgs
    {
        public string? Message { get; set; }
        public ConversionMessageEventArgs(string? message) { Message = message; }
    }
    public class ConversionProgressEventArgs : EventArgs
    {
        public float ConversionPercent { get; set; }
        public ConversionProgressEventArgs(float conversionPercent) { ConversionPercent = conversionPercent; }
    }
}
