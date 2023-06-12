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
		public string chassis;
		[JsonInclude]
		public string assetPathPrefix;
		[JsonInclude]
		public string previewPathPrefix;
		[JsonInclude]
		public string name;

		[JsonInclude]
		public string[] textures;
	}
}
