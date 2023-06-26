using System;
using System.IO;
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

		[JsonInclude]
		[JsonPropertyName("assetPathOverrides")]
		public string[] assetPathOverrides;

		public string GetFullAssetPath(string textureType)
		{
			for (int i = 0; i < Textures.Length; i++)
			{
				if (Textures[i] == textureType)
					return GetFullAssetPath(i);
			}
			throw new Exception($"textureType {textureType} is not present");
		}

		public string GetFullAssetPath(int textureIndex)
		{
			if (assetPathOverrides != null && assetPathOverrides.Length > textureIndex && assetPathOverrides[textureIndex] != "")
				return $"{assetPathOverrides[textureIndex]}";
			return $"{AssetPathPrefix}_{Textures[textureIndex]}";
		}
	}
}
