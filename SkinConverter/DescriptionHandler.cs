using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Advocate
{
    /// <summary>
    /// Handles parsing descriptions given by users
    /// </summary>
    internal class DescriptionHandler
    {
        /// <summary>
        /// The Author Name field
        /// </summary>
        public string Author { get; init; }

        /// <summary>
        /// The Version field
        /// </summary>
        public string Version { get; init; }

        /// <summary>
        /// The Skin Name field
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The types of skin that the package contains, eg CAR, Flatline, R101, etc.
        /// </summary>
        public string[] Types { get; init; }

        /// <summary>
        /// Parses a description containing keys in the format {<KEY>} into a properly formatted description
        /// </summary>
        /// <param name="toParse">The string to parse</param>
        /// <returns>The parsed string</returns>
        public string ParseDescription(string toParse)
        {
            // handle null value
            if (toParse == null)
                return "";

            // replace all instances of {<stuff>} with known values using GetValue
            return Regex.Replace(toParse, @"\{\w+?\}",
                match => GetValue(match.Value));
        }

        /// <summary>
        /// Replaces a known key in the format {<KEY>} with a variable
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The replacement string, or the key if it is not found</returns>
        private string GetValue(string key)
        {
            return key switch
            {
                "{AUTHOR}" => Author,
                "{VERSION}" => Version,
                "{SKIN}" => Name,
                "{TYPES}" => string.Join('/', Types),
                // do not replace if it is an unrecognised key
                _ => key,
            };
        }
    }
}
