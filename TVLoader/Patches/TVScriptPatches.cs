using HarmonyLib;

using System.IO;
using System.Reflection;

using TVLoader.Utils;

using UnityEngine.Video;

namespace TVLoader.Patches
{

	[HarmonyPatch(typeof(TVScript))]
	internal class TVScriptPatches
	{
		static FieldInfo currentClipProperty = typeof(TVScript).GetField("currentClip", BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo currentTimeProperty = typeof(TVScript).GetField("currentClipTime", BindingFlags.NonPublic | BindingFlags.Instance);
		static MethodInfo setMatProperty = typeof(TVScript).GetMethod("SetTVScreenMaterial", BindingFlags.NonPublic | BindingFlags.Instance);

		[HarmonyPatch(typeof(TVScript), "Update")]
		[HarmonyPrefix]
		public static bool Update(TVScript __instance) => false;

		[HarmonyPatch(typeof(TVScript), "TurnTVOnOff")]
		[HarmonyPrefix]
		public static bool TurnTVOnOff(TVScript __instance, bool on)
		{
			if (VideoManager.Videos.Count == 0) return false;

			if (__instance.video.source != UnityEngine.Video.VideoSource.Url)
			{
				__instance.video.clip = null;
				__instance.tvSFX.clip = null;

				__instance.video.source = UnityEngine.Video.VideoSource.Url;
				__instance.video.controlledAudioTrackCount = 1;
				__instance.video.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;
				__instance.video.SetTargetAudioSource(0, __instance.tvSFX);
			}

			__instance.tvOn = on;
			if (on)
			{
				PlayNextVideo(__instance);
				__instance.tvSFX.PlayOneShot(__instance.switchTVOn);
				WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn);
			}
			else
			{
				__instance.video.Stop();
				__instance.tvSFX.PlayOneShot(__instance.switchTVOff);
				WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOff);
			}

			setMatProperty.Invoke(__instance, new object[] { on });
			return false;
		}

		[HarmonyPatch(typeof(TVScript), "TVFinishedClip")]
		[HarmonyPrefix]
		public static bool TVFinishedClip(TVScript __instance, VideoPlayer source)
		{
			PlayNextVideo(__instance);
			return false;
		}

		private static void PlayNextVideo(TVScript instance)
		{
			TVLoaderPlugin.Log.LogInfo($"Playing next video...");
			int currentClip = (int)currentClipProperty.GetValue(instance);
			currentClip = (currentClip + 1) % VideoManager.Videos.Count;
			TVLoaderPlugin.Log.LogInfo($"currentClip: {currentClip} - {VideoManager.Videos[currentClip]}");

			currentTimeProperty.SetValue(instance, 0f);
			currentClipProperty.SetValue(instance, currentClip);

			//instance.tvSFX.time = 0;
			instance.video.url = $"file://{VideoManager.Videos[currentClip]}";
			instance.video.Play();
		}

	}
}