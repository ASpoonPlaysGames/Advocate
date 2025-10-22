using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advocate.Models.JSON;

internal class Map
{
	[JsonPropertyName("name")]
	public string Name { get; init; }

	[JsonPropertyName("streamFileMandatory")]
	public string StarpakPath { get; init; }

	[JsonPropertyName("assetsDir")]
	public string AssetsDir { get; init; }

	[JsonPropertyName("outputDir")]
	public string OutputDir { get; init; }

	[JsonPropertyName("version")]
	public int Version { get; init; } = 7; // defaults to 7 (titanfall 2)

	[JsonPropertyName("showDebugInfo")]
	public bool ShowDebugInfo { get; init; } = true;

	[JsonPropertyName("keepDevOnly")]
	public bool KeepDevOnly { get; init; } = true;

	// this could in theory have non-texture assets, but this will do for now
	[JsonPropertyName("files")]
	public List<TextureAsset> Files { get; private set; } = new();

	public Map(string name, string assetsDir, string outputDir)
	{
		Name = name;
		AssetsDir = assetsDir;
		OutputDir = outputDir;
		StarpakPath = $"{name}.starpak";
	}

	public void AddTextureAsset(string path, bool disableStreaming = false)
	{
		// trim texture/ from all txtr paths since repak prepends with it now, just to be safe
		const string texturePrepend = "texture/";
		path = path.StartsWith(texturePrepend) ? path[texturePrepend.Length..] : path;

		TextureAsset asset = new() { Path = path, DisableStreaming = disableStreaming };
		Files.Add(asset);
	}
}

internal class TextureAsset
{
	[JsonPropertyName("_type")]
	public string Type { get; } = "txtr";

	[JsonPropertyName("_path")]
	public string Path { get; init; } = "";

	[JsonPropertyName("$disableStreaming")]
	public bool DisableStreaming { get; init; } = false;
}
