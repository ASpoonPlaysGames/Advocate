﻿namespace Advocate.Models.JSON
{
	internal class Mod
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Version { get; set; }
		public int LoadPriority { get; set; }

		// there are more fields here but we dont need them for a simple skin mod
	}
}

