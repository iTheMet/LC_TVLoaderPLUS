using BepInEx;

using System.Collections.Generic;
using System.IO;

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
				string videoPath = Path.Combine(Paths.PluginPath, plugin, "Media", "Television Videos");
				if (!Directory.Exists(videoPath)) continue;
				var videos = Directory.GetFiles(videoPath, "*.mp4");
				Videos.AddRange(videos);
				TVLoaderPlugin.Log.LogInfo($"{plugin} has {videos.Length} videos.");
			}

			TVLoaderPlugin.Log.LogInfo($"Loaded {Videos.Count} total.");
		}
	}
}
