using System.Text.RegularExpressions;

namespace Advocate.Scripts.NoseArts
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
		///     <para>The name of the Author of the mod.</para>
		///     <para>Defaults to <see cref="string.Empty"/> if not set at init.</para>
		/// </value>
		public string Author { get; init; } = string.Empty;

		/// <summary>
		///     The Version field
		/// </summary>
		/// <value>
		///     <para>The Version number of the mod.</para>
		///     <para>Defaults to <see cref="string.Empty"/> if not set at init.</para>
		/// </value>
		public string Version { get; init; } = string.Empty;

		/// <summary>
		///     The Name field
		/// </summary>
		/// <value>
		///     <para>The name of the mod.</para>
		///     <para>Defaults to <see cref="string.Empty"/> if not set at init.</para>
		/// </value>
		public string Name { get; init; } = string.Empty;

		/// <summary>
		///     The Chassis field
		/// </summary>
		/// <value>
		///     <para>The titan chassis the nose art is for</para>
		///     <para>Defaults to <see cref="string.Empty"/> if not set at init.</para>
		/// </value>
		public string Chassis { get; init; } = string.Empty;

		/// <summary>
		///     The NoseArt field
		/// </summary>
		/// <value>
		///     <para>The vanilla nose art's name</para>
		///     <para>Defaults to <see cref="string.Empty"/> if not set at init.</para>
		/// </value>
		public string NoseArt { get; init; } = string.Empty;

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
				"{CHASSIS}" => Chassis,
				"{NOSEART}" => NoseArt,
				// do not replace if it is an unrecognised key
				_ => key,
			};
		}
	}
}
