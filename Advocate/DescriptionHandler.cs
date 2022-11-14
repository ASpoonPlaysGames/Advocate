using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Advocate
{
    /// <summary>
    ///     Handles parsing descriptions given by users, using information about the skin.
    ///     To format a string, use <see cref="FormatDescription(string)"/>
    /// </summary>
    internal class DescriptionHandler
    {
        /// <summary>
        ///     The Author Name field.
        /// </summary>
        /// <value>
        ///     The name of the Author of the skin.
        ///     Defaults to <see cref="string.Empty"/> if not set at init.
        /// </value>
        public string Author { get; init; } = string.Empty;

        /// <summary>
        ///     The Version field
        /// </summary>
        /// <value>
        ///     The Version number of the skin.
        ///     Defaults to <see cref="string.Empty"/> if not set at init.
        /// </value>
        public string Version { get; init; } = string.Empty;

        /// <summary>
        ///     The Skin Name field
        /// </summary>
        /// <value>
        ///     The name of the skin.
        ///     Defaults to <see cref="string.Empty"/> if not set at init.
        /// </value>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        ///     The types of skin that the package contains, eg CAR, Flatline, R101, etc.
        /// </summary>
        /// <value>
        ///     The different types of skin contained in the package.
        ///     Defaults to <see cref="Array.Empty{string}"/> if not set at init.
        /// </value>
        public string[] Types { get; init; } = Array.Empty<string>();

        /// <summary>
        ///     Formats a description containing keys in the format "{KEY}", replacing each key with information about the skin
        /// </summary>
        /// <param name="toFormat">The string that will be formatted</param>
        /// <returns>The parsed string</returns>
        public string FormatDescription(string toFormat)
        {
            // handle null value
            if (toFormat == null)
                return "";

            // replace all instances of {<stuff>} with known values using GetValue
            return Regex.Replace(toFormat, @"\{\w+?\}",
                match => GetValue(match.Value));
        }

        /// <summary>
        ///     Replaces a known key in the format "{KEY}" with a variable
        /// </summary>
        /// <param name="key">The key to replace if found</param>
        /// <returns>The replacement string, or the key if it is not found</returns>
        private string GetValue(string key)
        {
            return key switch
            {
                // ADD NEW KEYS HERE WHEN THEY ARE CREATED
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
