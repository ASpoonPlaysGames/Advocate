using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Advocate.Conversion.JSON
{
	internal class Map
	{
		[JsonPropertyName("name")]
		public string Name;

		[JsonPropertyName("assetsDir")]
		public string AssetsDir;

		[JsonPropertyName("outputDir")]
		public string OutputDir;

		[JsonPropertyName("version")]
		public int Version { get; init; } = 7; // defaults to 7 (titanfall 2)

		// this could in theory have non-texture assets, but this will do for now
		[JsonPropertyName("files")]
		public List<TextureAsset> Files { get; private set; } = new();

		public Map(string name, string assetsDir, string outputDir)
		{
			Name = name;
			AssetsDir = assetsDir;
			OutputDir = outputDir;
		}

		public void AddTextureAsset(string path, string? starpakPath = null)
		{
			TextureAsset asset = new TextureAsset() { Path = path, DisableStreaming = starpakPath == null };
			Files.Add(asset);
		}
	}

	internal class TextureAsset
	{
		[JsonPropertyName("$type")]
		public const string Type = "txtr";

		[JsonPropertyName("path")]
		public string Path { get; init; } = "";

		[JsonPropertyName("disableStreaming")]
		public bool DisableStreaming { get; init; } = true;
	}
}
