using BepInEx;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVLoader.Utils
{
	internal static class VideoManager
	{
		public static List<string> Videos = new List<string>();

		public static void Load()
		{
			var plugins = Directory.GetDirectories(Paths.PluginPath);
			foreach (var plugin in plugins)
			{
				string videoPath = Path.Combine(Paths.PluginPath, plugin, "Television Videos");
				if (!Directory.Exists(videoPath)) continue;
				Videos.AddRange(Directory.GetFiles(videoPath, "*.mp4"));
			}
		}
	}
}
