using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Advocate.Logging;
using Advocate.Models.JSON;

namespace Advocate.Scripts.NoseArts
{
    class NoseArtCreator
    {
		/// <summary>
		///     The name of the Author of the skin.
		/// </summary>
		/// <value>
		///     A non-zero length string of the Author's name, containing
		///     only alphanumeric characters, ' ', and '_'.
		/// </value>
		public string AuthorName { get; private set; }

		/// <summary>
		///     The name of the skin.
		/// </summary>
		/// <value>
		///     A non-zero length string of the skin's name, containing
		///     only alphanumeric characters, ' ', and '_'.
		/// </value>
		public string SkinName { get; private set; }

		/// <summary>
		///     The version number of the skin.
		/// </summary>
		/// <value>
		///     A string of the version number of the skin,
		///     in the format MAJOR.MINOR.PATCH. <code>Example: 1.3.0</code>
		/// </value>
		public string Version { get; private set; }


		/// <summary>
		///     The skin's README.md file, as a string.
		/// </summary>
		/// <value>
		///     <para>A string of the skin's README.md file, set at initialisation.</para>
		///     <para>Defaults to an empty string ("")</para>
		/// </value>
		public string ReadMePath { get; private set; }

		/// <summary>
		///     The Icon for the Nose Art.
		/// </summary>
		/// <value>
		///     <para>A Bitmap of the nose art's icon, must be 256x256.</para>
		///     <para>Generated from the _col texture
		///     if null when <see cref="Convert(string, string, string, string, bool)"/> is called.</para>
		/// </value>
		public string? IconPath { get; private set; }

		public NoseArt noseArt { get; private set; }

		public float ConvertProgress { get { return 100 * curStep / NUM_CONVERT_STEPS; } }

		private const float NUM_CONVERT_STEPS = 14; // INCREMENT THIS WHEN YOU ADD A NEW MESSAGE IDK
		private float curStep = 0;

		// just here for better readability
		private void ConvertTaskComplete() { curStep++; }

		// helper functions for some nicer logging
		private void Debug(string message) { Logger.Debug(message, ConvertProgress); }
		private void Info(string message) { Logger.Info(message, ConvertProgress); }
		private void Completion(string message) { Logger.Completion(message, ConvertProgress); }
		private void Error(string message) { Logger.Error(message, ConvertProgress); }


		/// <summary>
		///		Constructor for NoseArtCreator, validates input
		/// </summary>
		/// <param name="pNoseArt"></param>
		/// <param name="images"></param>
		/// <param name="pAuthorName"></param>
		/// <param name="pSkinName"></param>
		/// <param name="pVersion"></param>
		/// <param name="pReadMePath"></param>
		/// <param name="pIconPath"></param>
		public NoseArtCreator(NoseArt pNoseArt, Uri[] pImages, string pAuthorName, string pSkinName, string pVersion, string pReadMePath = "", string pIconPath = "")
		{
			// validate pAuthorName, see AuthorName for more details
			if (string.IsNullOrEmpty(pAuthorName)) { throw new ArgumentException("Author Name is required!"); }
			if (Regex.Match(pAuthorName, "[^\\da-zA-Z _]").Success || string.IsNullOrWhiteSpace(pAuthorName)) { throw new ArgumentException("Author Name is invalid!"); }
			AuthorName = pAuthorName;

			// validate pSkinName, same as pAuthorName
			if (string.IsNullOrEmpty(pSkinName)) { throw new ArgumentException("Skin Name is required!"); }
			if (Regex.Match(pSkinName, "[^\\da-zA-Z _]").Success || string.IsNullOrWhiteSpace(pSkinName)) { throw new ArgumentException("Skin Name is invalid!"); }
			SkinName = pSkinName;

			// validate pVersion, must be in the format MAJOR.MINOR.VERSION
			if (string.IsNullOrEmpty(pVersion)) { throw new ArgumentException("Version is required!"); }
			if (!Regex.Match(pVersion, "^\\d+.\\d+.\\d+$").Success) { throw new ArgumentException("Version is invalid! (Example: 1.0.0)"); }
			Version = pVersion;

			// check that pReadMePath is valid and leads to a .md file
			if (!string.IsNullOrWhiteSpace(pReadMePath) && !pReadMePath.EndsWith(".md")) { throw new ArgumentException("README path doesn't lead to a .md file!"); }
			ReadMePath = pReadMePath;

			// check that pIconPath is valid and leads to a .png file
			if (!string.IsNullOrWhiteSpace(pIconPath) && !pIconPath.EndsWith(".png")) { throw new ArgumentException("Icon path doesn't lead to a .png file!"); }
			IconPath = pIconPath;
		}

	}
}
