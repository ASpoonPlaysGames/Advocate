using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advocate.DDS
{
	internal class Manager
	{
		/* the purpose of this class is:
		 * 1. to store the raw dds images for a texture
		 * 2. to delegate mipmap generation
		 * 3. to act as the way that other classes interact with DDS files
		 * 4. to save dds images to files
		 * 
		 */

		// holds the different images used to create the single image with mipmaps
		// the key is the number of pixels in the mip level (width * height)
		private SortedList<int, byte[]> mipmaps = new();

		public int MipMapCount { get { return mipmaps.Count; } }

		private Header lastHeader;

		public void LoadImage(BinaryReader reader)
		{
			// load the header
			Header hdr = new(reader);
			// override lastHeader if this dds is higher resolution (should make my life easier)
			if (lastHeader == null || hdr.Width * hdr.Height > lastHeader.Width * lastHeader.Height)
			{
				lastHeader = hdr;
			}

			// if the fourCC does not match up, skip this image
			if (lastHeader.FourCC != hdr.FourCC)
			{
				Logging.Logger.Debug($"fourCC ({hdr.FourCC}) for added image does not match existing images ({lastHeader.FourCC})");
				return;
			}
			// if the DXGI format does not match up, also skip this image
			if (lastHeader.isDX10 == hdr.isDX10 && lastHeader.DXGIFormat != hdr.DXGIFormat)
			{
				Logging.Logger.Debug($"{lastHeader.isDX10} != {hdr.isDX10} or {lastHeader.DXGIFormat} == {hdr.DXGIFormat}");
				Logging.Logger.Debug($"DXGI format for added image ({hdr.DXGIFormat}) does not match existing images ({lastHeader.DXGIFormat})");
				return;
			}

			// read the image data into mipmaps dictionary
			for (int i = 0; i < hdr.MipMapCount || i == 0; i++)
			{
				int width = hdr.Width / (int)Math.Pow(2, i);
				int height = hdr.Height / (int)Math.Pow(2, i);
				int numPixels = width * height;
				// if we already have a mip for this resolution, dont do anything
				if (mipmaps.ContainsKey(numPixels))
				{
					continue;
				}

				int actualSize = Math.Max(hdr.PitchOrLinearSize / (int)Math.Pow(4, i), 16);
				mipmaps[numPixels] = reader.ReadBytes(actualSize);
				Logging.Logger.Debug($"Found mip level:\n Width: {width} Height: {height}");
			}
		}

		public void SaveImage(BinaryWriter writer)
		{
			// edit the header to work
			lastHeader.PitchOrLinearSize = mipmaps.Values.Last().Length;
			lastHeader.MipMapCount = mipmaps.Count;
			// make sure that the mipmap flag is set if we have more than 1 mip level
			if (lastHeader.MipMapCount > 1)
			{
				lastHeader.Flags |= 0x20000;
			}

			// write the header
			lastHeader.Save(writer);
			// write the image data (bigger mips first so iterate backwards)
			for (int i = mipmaps.Count - 1; i >= 0; --i)
			{
				writer.Write(mipmaps.Values[i]);
			}
			
		}
	}
}
