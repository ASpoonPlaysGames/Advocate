using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

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
            BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open));
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
                // read extra header if needed
                isDX10 = new string(pixel_FourCC) == "DX10";
                if (isDX10)
                {
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
            string str_fourCC = new string(pixel_FourCC);
            switch (str_fourCC)
            {
                case "DXT1":
                    //ToDX10(DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM);
                    break;
                case "ATI2":
                    pixel_FourCC = new char[4] { 'B', 'C', '5', 'U' };
                    if (pitchOrLinearSize == 0)
                        pitchOrLinearSize = (uint)data.Length - 4;
                    if ((flags & 0x000A0000) != 0x000A0000)
                        flags |= 0x000A0000;
                    break;
                case "BC4U":
                    //ToDX10(DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM);
                    if (pitchOrLinearSize == 0)
                        pitchOrLinearSize = (uint)data.Length - 4;// i dont even know like what the fuck
                        //pitchOrLinearSize = Math.Max(1, ((width + 3) / 4)) * 8;
                    if ((flags & 0x000A0000) != 0x000A0000)
                        flags |= 0x000A0000;
                    if ((caps & 0x00400000) != 0x00400000)
                        caps |= 0x00400000;
                    if ((caps & 0x00000008) != 0x00000008)
                        caps |= 0x00000008;
                    break;

                default:
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
            BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create));
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


        public enum DXGI_FORMAT : UInt32
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
            DXGI_FORMAT_R32G32B32A32_UINT = 3,
            DXGI_FORMAT_R32G32B32A32_SINT = 4,
            DXGI_FORMAT_R32G32B32_TYPELESS = 5,
            DXGI_FORMAT_R32G32B32_FLOAT = 6,
            DXGI_FORMAT_R32G32B32_UINT = 7,
            DXGI_FORMAT_R32G32B32_SINT = 8,
            DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            DXGI_FORMAT_R16G16B16A16_UNORM = 11,
            DXGI_FORMAT_R16G16B16A16_UINT = 12,
            DXGI_FORMAT_R16G16B16A16_SNORM = 13,
            DXGI_FORMAT_R16G16B16A16_SINT = 14,
            DXGI_FORMAT_R32G32_TYPELESS = 15,
            DXGI_FORMAT_R32G32_FLOAT = 16,
            DXGI_FORMAT_R32G32_UINT = 17,
            DXGI_FORMAT_R32G32_SINT = 18,
            DXGI_FORMAT_R32G8X24_TYPELESS = 19,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
            DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R10G10B10A2_UINT = 25,
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
            DXGI_FORMAT_R8G8B8A8_UINT = 30,
            DXGI_FORMAT_R8G8B8A8_SNORM = 31,
            DXGI_FORMAT_R8G8B8A8_SINT = 32,
            DXGI_FORMAT_R16G16_TYPELESS = 33,
            DXGI_FORMAT_R16G16_FLOAT = 34,
            DXGI_FORMAT_R16G16_UNORM = 35,
            DXGI_FORMAT_R16G16_UINT = 36,
            DXGI_FORMAT_R16G16_SNORM = 37,
            DXGI_FORMAT_R16G16_SINT = 38,
            DXGI_FORMAT_R32_TYPELESS = 39,
            DXGI_FORMAT_D32_FLOAT = 40,
            DXGI_FORMAT_R32_FLOAT = 41,
            DXGI_FORMAT_R32_UINT = 42,
            DXGI_FORMAT_R32_SINT = 43,
            DXGI_FORMAT_R24G8_TYPELESS = 44,
            DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
            DXGI_FORMAT_R8G8_TYPELESS = 48,
            DXGI_FORMAT_R8G8_UNORM = 49,
            DXGI_FORMAT_R8G8_UINT = 50,
            DXGI_FORMAT_R8G8_SNORM = 51,
            DXGI_FORMAT_R8G8_SINT = 52,
            DXGI_FORMAT_R16_TYPELESS = 53,
            DXGI_FORMAT_R16_FLOAT = 54,
            DXGI_FORMAT_D16_UNORM = 55,
            DXGI_FORMAT_R16_UNORM = 56,
            DXGI_FORMAT_R16_UINT = 57,
            DXGI_FORMAT_R16_SNORM = 58,
            DXGI_FORMAT_R16_SINT = 59,
            DXGI_FORMAT_R8_TYPELESS = 60,
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_R8_UINT = 62,
            DXGI_FORMAT_R8_SNORM = 63,
            DXGI_FORMAT_R8_SINT = 64,
            DXGI_FORMAT_A8_UNORM = 65,
            DXGI_FORMAT_R1_UNORM = 66,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
            DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
            DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_B5G6R5_UNORM = 85,
            DXGI_FORMAT_B5G5R5A1_UNORM = 86,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_B8G8R8X8_UNORM = 88,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
            DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
            DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
            DXGI_FORMAT_AYUV = 100,
            DXGI_FORMAT_Y410 = 101,
            DXGI_FORMAT_Y416 = 102,
            DXGI_FORMAT_NV12 = 103,
            DXGI_FORMAT_P010 = 104,
            DXGI_FORMAT_P016 = 105,
            DXGI_FORMAT_420_OPAQUE = 106,
            DXGI_FORMAT_YUY2 = 107,
            DXGI_FORMAT_Y210 = 108,
            DXGI_FORMAT_Y216 = 109,
            DXGI_FORMAT_NV11 = 110,
            DXGI_FORMAT_AI44 = 111,
            DXGI_FORMAT_IA44 = 112,
            DXGI_FORMAT_P8 = 113,
            DXGI_FORMAT_A8P8 = 114,
            DXGI_FORMAT_B4G4R4A4_UNORM = 115,
            DXGI_FORMAT_P208 = 130,
            DXGI_FORMAT_V208 = 131,
            DXGI_FORMAT_V408 = 132,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE,
            DXGI_FORMAT_FORCE_UINT = 0xffffffff
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

}
