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

		private Header lastHeader;

		// used to check for mismatching fourCC
		//private string pixel_FourCC;

		public void LoadImage(BinaryReader reader)
		{
			// load the header
			Header hdr = new(reader);
			// override lastHeader if this dds is higher resolution (should make my life easier)
			if (lastHeader == null || hdr.Width * hdr.Height > lastHeader.Width * lastHeader.Height)
			{
				lastHeader = hdr;
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
			// make sure that the mipmap flag is set
			if (lastHeader.MipMapCount > 0)
			{
				lastHeader.Flags |= 0x20000;
			}

			// write the header
			lastHeader.Save(writer);
			// write the image data
			for (int i = mipmaps.Count - 1; i >= 0; --i)
			{
				writer.Write(mipmaps.Values[i]);
			}
			
		}
	}
}
