using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Advocate.Models.JSON
{
	internal class NoseArt
	{
		[JsonInclude]
		[JsonPropertyName("chassis")]
		public string Chassis;
		[JsonInclude]
		[JsonPropertyName("assetPathPrefix")]
		public string AssetPathPrefix;
		[JsonInclude]
		[JsonPropertyName("previewPathPrefix")]
		public string PreviewPathPrefix;
		[JsonInclude]
		[JsonPropertyName("name")]
		public string Name;

		[JsonInclude]
		[JsonPropertyName("width")]
		public int Width;
		[JsonInclude]
		[JsonPropertyName("height")]
		public int Height;

		[JsonInclude]
		[JsonPropertyName("textures")]
		public string[] Textures;
	}
}
