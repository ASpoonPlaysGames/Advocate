using System.Text.Json.Serialization;

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
