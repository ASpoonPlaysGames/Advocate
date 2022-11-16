using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advocate.Conversion.JSON
{
    internal class Manifest
    {
#pragma warning disable IDE1006 // Naming Styles
        public string name { get; set; }
        public string version_number { get; set; }
        public string website_url { get; set; }
        public string[] dependencies { get; set; } = Array.Empty<string>();
        public string description { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
