using BepInEx;
using BepInEx.Logging;

using HarmonyLib;

using TVLoader.Utils;

namespace TVLoader
{
	[BepInPlugin(MyGUID, PluginName, VersionString)]
	public class TVLoaderPlugin : BaseUnityPlugin
	{
		private const string MyGUID = "rattenbonkers.TVLoader";
		private const string PluginName = "TVLoader";
		private const string VersionString = "1.0.3";

		private static readonly Harmony Harmony = new Harmony(MyGUID);
		public static ManualLogSource Log = new ManualLogSource(PluginName);

		private void Awake()
		{
			Log = Logger;


			Harmony.PatchAll();
			VideoManager.Load();
			Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded. Video Count: ${VideoManager.Videos.Count}");
		}

	}
}
