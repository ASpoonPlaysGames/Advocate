using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Advocate
{
	internal class DdsHandler
	{
		// dds file structure
		char[] magic = new char[4];
		// header
		UInt32 size;
		UInt32 flags;
		UInt32 height;
		UInt32 width;
		UInt32 pitchOrLinearSize;
		UInt32 depth;
		UInt32 mipMapCount;
		char[] reserved = new char[44];
		// header -> pixel format
		UInt32 pixel_Size;
		UInt32 pixel_Flags;
		char[] pixel_FourCC = new char[4];
		UInt32 pixel_RGBBitCount;
		UInt32 pixel_rBitMask;
		UInt32 pixel_gBitMask;
		UInt32 pixel_bBitMask;
		UInt32 pixel_aBitMask;
		// header again
		UInt32 caps;
		UInt32 caps2;
		UInt32 caps3;
		UInt32 caps4;
		UInt32 reserved1;
		// dx10 header
		DXGI_FORMAT dxgiFormat;
		DX10ResourceDimension resourceDimension;
		UInt32 miscFlags;
		UInt32 arraySize;
		DX10AlphaMode alphaMode;

		// other variables
		bool isDX10;
		byte[] data; // just everything else thats not in the header


		public DdsHandler(string path)
		{
			Logging.Logger.Debug($"Handling DDS file at path '{path}'");
			BinaryReader reader = new(new FileStream(path, FileMode.Open));
			try
			{
				// read magic
				magic = reader.ReadChars(4);
				if (new string(magic) != "DDS ")
				{
					throw new Exception("File is not a valid DDS file at path " + path);
				}
				// read header
				size = reader.ReadUInt32();
				flags = reader.ReadUInt32();
				height = reader.ReadUInt32();
				width = reader.ReadUInt32();
				pitchOrLinearSize = reader.ReadUInt32();
				depth = reader.ReadUInt32();
				mipMapCount = reader.ReadUInt32();
				reserved = reader.ReadChars(44);
				pixel_Size = reader.ReadUInt32();
				pixel_Flags = reader.ReadUInt32();
				pixel_FourCC = reader.ReadChars(4);
				pixel_RGBBitCount = reader.ReadUInt32();
				pixel_rBitMask = reader.ReadUInt32();
				pixel_gBitMask = reader.ReadUInt32();
				pixel_bBitMask = reader.ReadUInt32();
				pixel_aBitMask = reader.ReadUInt32();
				caps = reader.ReadUInt32();
				caps2 = reader.ReadUInt32();
				caps3 = reader.ReadUInt32();
				caps4 = reader.ReadUInt32();
				reserved1 = reader.ReadUInt32();
				// read extra header if needed
				isDX10 = new string(pixel_FourCC) == "DX10";
				if (isDX10)
				{
					Logging.Logger.Debug($"DDS file at path '{path}' is using DX10");
					dxgiFormat = (DXGI_FORMAT)reader.ReadUInt32();
					resourceDimension = (DX10ResourceDimension)reader.ReadUInt32();
					miscFlags = reader.ReadUInt32();
					arraySize = reader.ReadUInt32();
					alphaMode = (DX10AlphaMode)reader.ReadUInt32();
				}
				// read rest of data
				// potentially losing data here if length > max int value?
				data = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
			}
			finally
			{
				reader.Close();
			}
		}
		public void Convert()
		{
			string str_fourCC = new(pixel_FourCC);
			// this is required by the game (and legion) to read things properly, but sometimes it isn't set
			if (pitchOrLinearSize == 0)
			{
				Logging.Logger.Debug($"DDS file did not have pitchOrLinearSize set, setting to {data.Length}");
				pitchOrLinearSize = (uint)data.Length;
			}

			switch (str_fourCC)
			{
				case "DXT1":
					ToDX10(DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB);
					break;
				case "ATI2":
					Logging.Logger.Debug($"DDS file is using ATI2, changing to BC5U");
					pixel_FourCC = new char[4] { 'B', 'C', '5', 'U' };
					goto case "BC5U";
				case "BC5U":
					if ((flags & 0x000A0000) != 0x000A0000)
						flags |= 0x000A0000;
					break;
				case "BC4U":
					//ToDX10(DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM);
					if ((flags & 0x000A0000) != 0x000A0000)
						flags |= 0x000A0000;
					if ((caps & 0x00400000) != 0x00400000)
						caps |= 0x00400000;
					if ((caps & 0x00000008) != 0x00000008)
						caps |= 0x00000008;
					break;
				case "DX10":
					// do nothing, but do support the fourCC
					break;

				default:
					Logging.Logger.Debug($"DDS file is using {str_fourCC}, which is unsupported.");
					throw new NotImplementedException("DDS fourCC not supported: " + str_fourCC);
			}
		}

		public void ToDX10(DXGI_FORMAT format)
		{
			dxgiFormat = format;
			arraySize = 1;
			resourceDimension = DX10ResourceDimension.Texture2D;
			alphaMode = DX10AlphaMode.Unknown;
			miscFlags = 0;
			pixel_FourCC = new char[4] { 'D', 'X', '1', '0' };
			isDX10 = true;
		}

		public void Save(string path)
		{
			// create the directory if it exists and path has a parent directory (it always should, but this is just in case)
			if (!string.IsNullOrEmpty(Path.GetDirectoryName(path)))
				Directory.CreateDirectory(Path.GetDirectoryName(path));

			BinaryWriter writer = new(new FileStream(path, FileMode.Create));
			try
			{
				// write magic
				writer.Write(magic);
				// write header
				writer.Write(size);
				writer.Write(flags);
				writer.Write(height);
				writer.Write(width);
				writer.Write(pitchOrLinearSize);
				writer.Write(depth);
				writer.Write(mipMapCount);
				writer.Write(reserved);
				writer.Write(pixel_Size);
				writer.Write(pixel_Flags);
				writer.Write(pixel_FourCC);
				writer.Write(pixel_RGBBitCount);
				writer.Write(pixel_rBitMask);
				writer.Write(pixel_gBitMask);
				writer.Write(pixel_bBitMask);
				writer.Write(pixel_aBitMask);
				writer.Write(caps);
				writer.Write(caps2);
				writer.Write(caps3);
				writer.Write(caps4);
				writer.Write(reserved1);
				// write dx10 header if needed
				if (isDX10)
				{
					writer.Write((UInt32)dxgiFormat);
					writer.Write((UInt32)resourceDimension);
					writer.Write(miscFlags);
					writer.Write(arraySize);
					writer.Write((UInt32)alphaMode);
				}
				// write raw data
				writer.Write(data);
			}
			finally
			{
				writer.Close();
			}
		}

	}
	public enum DXGI_FORMAT : UInt32
	{
		DXGI_FORMAT_UNKNOWN,
		DXGI_FORMAT_R32G32B32A32_TYPELESS,
		DXGI_FORMAT_R32G32B32A32_FLOAT,
		DXGI_FORMAT_R32G32B32A32_UINT,
		DXGI_FORMAT_R32G32B32A32_SINT,
		DXGI_FORMAT_R32G32B32_TYPELESS,
		DXGI_FORMAT_R32G32B32_FLOAT,
		DXGI_FORMAT_R32G32B32_UINT,
		DXGI_FORMAT_R32G32B32_SINT,
		DXGI_FORMAT_R16G16B16A16_TYPELESS,
		DXGI_FORMAT_R16G16B16A16_FLOAT,
		DXGI_FORMAT_R16G16B16A16_UNORM,
		DXGI_FORMAT_R16G16B16A16_UINT,
		DXGI_FORMAT_R16G16B16A16_SNORM,
		DXGI_FORMAT_R16G16B16A16_SINT,
		DXGI_FORMAT_R32G32_TYPELESS,
		DXGI_FORMAT_R32G32_FLOAT,
		DXGI_FORMAT_R32G32_UINT,
		DXGI_FORMAT_R32G32_SINT,
		DXGI_FORMAT_R32G8X24_TYPELESS,
		DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
		DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
		DXGI_FORMAT_X32_TYPELESS_G8X24_UINT,
		DXGI_FORMAT_R10G10B10A2_TYPELESS,
		DXGI_FORMAT_R10G10B10A2_UNORM,
		DXGI_FORMAT_R10G10B10A2_UINT,
		DXGI_FORMAT_R11G11B10_FLOAT,
		DXGI_FORMAT_R8G8B8A8_TYPELESS,
		DXGI_FORMAT_R8G8B8A8_UNORM,
		DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
		DXGI_FORMAT_R8G8B8A8_UINT,
		DXGI_FORMAT_R8G8B8A8_SNORM,
		DXGI_FORMAT_R8G8B8A8_SINT,
		DXGI_FORMAT_R16G16_TYPELESS,
		DXGI_FORMAT_R16G16_FLOAT,
		DXGI_FORMAT_R16G16_UNORM,
		DXGI_FORMAT_R16G16_UINT,
		DXGI_FORMAT_R16G16_SNORM,
		DXGI_FORMAT_R16G16_SINT,
		DXGI_FORMAT_R32_TYPELESS,
		DXGI_FORMAT_D32_FLOAT,
		DXGI_FORMAT_R32_FLOAT,
		DXGI_FORMAT_R32_UINT,
		DXGI_FORMAT_R32_SINT,
		DXGI_FORMAT_R24G8_TYPELESS,
		DXGI_FORMAT_D24_UNORM_S8_UINT,
		DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
		DXGI_FORMAT_X24_TYPELESS_G8_UINT,
		DXGI_FORMAT_R8G8_TYPELESS,
		DXGI_FORMAT_R8G8_UNORM,
		DXGI_FORMAT_R8G8_UINT,
		DXGI_FORMAT_R8G8_SNORM,
		DXGI_FORMAT_R8G8_SINT,
		DXGI_FORMAT_R16_TYPELESS,
		DXGI_FORMAT_R16_FLOAT,
		DXGI_FORMAT_D16_UNORM,
		DXGI_FORMAT_R16_UNORM,
		DXGI_FORMAT_R16_UINT,
		DXGI_FORMAT_R16_SNORM,
		DXGI_FORMAT_R16_SINT,
		DXGI_FORMAT_R8_TYPELESS,
		DXGI_FORMAT_R8_UNORM,
		DXGI_FORMAT_R8_UINT,
		DXGI_FORMAT_R8_SNORM,
		DXGI_FORMAT_R8_SINT,
		DXGI_FORMAT_A8_UNORM,
		DXGI_FORMAT_R1_UNORM,
		DXGI_FORMAT_R9G9B9E5_SHAREDEXP,
		DXGI_FORMAT_R8G8_B8G8_UNORM,
		DXGI_FORMAT_G8R8_G8B8_UNORM,
		DXGI_FORMAT_BC1_TYPELESS,
		DXGI_FORMAT_BC1_UNORM,
		DXGI_FORMAT_BC1_UNORM_SRGB,
		DXGI_FORMAT_BC2_TYPELESS,
		DXGI_FORMAT_BC2_UNORM,
		DXGI_FORMAT_BC2_UNORM_SRGB,
		DXGI_FORMAT_BC3_TYPELESS,
		DXGI_FORMAT_BC3_UNORM,
		DXGI_FORMAT_BC3_UNORM_SRGB,
		DXGI_FORMAT_BC4_TYPELESS,
		DXGI_FORMAT_BC4_UNORM,
		DXGI_FORMAT_BC4_SNORM,
		DXGI_FORMAT_BC5_TYPELESS,
		DXGI_FORMAT_BC5_UNORM,
		DXGI_FORMAT_BC5_SNORM,
		DXGI_FORMAT_B5G6R5_UNORM,
		DXGI_FORMAT_B5G5R5A1_UNORM,
		DXGI_FORMAT_B8G8R8A8_UNORM,
		DXGI_FORMAT_B8G8R8X8_UNORM,
		DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM,
		DXGI_FORMAT_B8G8R8A8_TYPELESS,
		DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,
		DXGI_FORMAT_B8G8R8X8_TYPELESS,
		DXGI_FORMAT_B8G8R8X8_UNORM_SRGB,
		DXGI_FORMAT_BC6H_TYPELESS,
		DXGI_FORMAT_BC6H_UF16,
		DXGI_FORMAT_BC6H_SF16,
		DXGI_FORMAT_BC7_TYPELESS,
		DXGI_FORMAT_BC7_UNORM,
		DXGI_FORMAT_BC7_UNORM_SRGB,
		DXGI_FORMAT_AYUV,
		DXGI_FORMAT_Y410,
		DXGI_FORMAT_Y416,
		DXGI_FORMAT_NV12,
		DXGI_FORMAT_P010,
		DXGI_FORMAT_P016,
		DXGI_FORMAT_420_OPAQUE,
		DXGI_FORMAT_YUY2,
		DXGI_FORMAT_Y210,
		DXGI_FORMAT_Y216,
		DXGI_FORMAT_NV11,
		DXGI_FORMAT_AI44,
		DXGI_FORMAT_IA44,
		DXGI_FORMAT_P8,
		DXGI_FORMAT_A8P8,
		DXGI_FORMAT_B4G4R4A4_UNORM,
		DXGI_FORMAT_P208,
		DXGI_FORMAT_V208,
		DXGI_FORMAT_V408,
		DXGI_FORMAT_FORCE_UINT,
	};

	enum DX10ResourceDimension : UInt32
	{
		Unknown = 0,
		Buffer = 1,
		Texture1D = 2,
		Texture2D = 3,
		Texture3D = 4,
	};

	enum DX10AlphaMode : UInt32
	{
		Unknown = 0,
		Straight = 1,
		PreMultiplied = 2,
		Opaque = 3,
		Custom = 4,
	};
	

}
