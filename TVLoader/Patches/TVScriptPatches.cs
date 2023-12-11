using HarmonyLib;

using System.Reflection;

using TVLoader.Utils;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Video;

namespace TVLoader.Patches
{

	[HarmonyPatch(typeof(TVScript))]
	internal class TVScriptPatches
	{
		private static FieldInfo currentClipProperty = typeof(TVScript).GetField("currentClip", BindingFlags.NonPublic | BindingFlags.Instance);
		private static FieldInfo currentTimeProperty = typeof(TVScript).GetField("currentClipTime", BindingFlags.NonPublic | BindingFlags.Instance);
		private static FieldInfo wasTvOnLastFrameProp = typeof(TVScript).GetField("wasTvOnLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
		private static FieldInfo timeSinceTurningOffTVProp = typeof(TVScript).GetField("timeSinceTurningOffTV", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo setMatMethod = typeof(TVScript).GetMethod("SetTVScreenMaterial", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo onEnableMethod = typeof(TVScript).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);

		private static bool tvHasPlayedBefore = false;

		private static RenderTexture renderTexture;

		private static VideoPlayer currentVideoPlayer; // Current playing video
		private static VideoPlayer nextVideoPlayer; // Next video to play, prepared for better experience.

		[HarmonyPrefix]
		[HarmonyPatch("Update")]
		public static bool Update(TVScript __instance)
		{
			if (currentVideoPlayer == null)
			{ // Basically firstRun
				currentVideoPlayer = __instance.GetComponent<VideoPlayer>();
				renderTexture = currentVideoPlayer.targetTexture;

				if (VideoManager.Videos.Count > 0)
					PrepareVideo(__instance, 0);
			}

			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch("TurnTVOnOff")]
		public static bool TurnTVOnOff(TVScript __instance, bool on)
		{
			TVLoaderPlugin.Log.LogInfo($"TVOnOff: {on}");
			if (VideoManager.Videos.Count == 0) return false;

			int currentClip = (int)currentClipProperty.GetValue(__instance);

			// Skip to the next video if this is not our first time turning on the TV
			if (on && tvHasPlayedBefore)
			{
				currentClip = (currentClip + 1) % VideoManager.Videos.Count;
				currentClipProperty.SetValue(__instance, currentClip);
			}

			__instance.tvOn = on;
			if (on)
			{
				PlayVideo(__instance);
				__instance.tvSFX.PlayOneShot(__instance.switchTVOn);
				WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn);
			}
			else
			{
				__instance.video.Stop();
				__instance.tvSFX.PlayOneShot(__instance.switchTVOff);
				WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOff);
			}

			setMatMethod.Invoke(__instance, new object[] { on });
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch("TVFinishedClip")]
		public static bool TVFinishedClip(TVScript __instance, VideoPlayer source)
		{
			// Don't bother with TV stuff if it's off or we're in the factory
			if (!__instance.tvOn || GameNetworkManager.Instance.localPlayerController.isInsideFactory)
				return false;

			// Skip to the next video
			TVLoaderPlugin.Log.LogInfo("TVFinishedClip");
			int currentClip = (int)currentClipProperty.GetValue(__instance);
			if (VideoManager.Videos.Count > 0)
				currentClip = (currentClip + 1) % VideoManager.Videos.Count;

			currentTimeProperty.SetValue(__instance, 0f);
			currentClipProperty.SetValue(__instance, currentClip);

			// Play it
			PlayVideo(__instance);
			return false;
		}

		private static void PrepareVideo(TVScript instance, int index = -1)
		{
			if (index == -1)
			{
				int currentClip = (int)currentClipProperty.GetValue(instance);
				index = currentClip + 1;
			}

			if (nextVideoPlayer != null && nextVideoPlayer.gameObject.activeInHierarchy)
				GameObject.Destroy(nextVideoPlayer);

			// Also prepare the next video
			nextVideoPlayer = instance.gameObject.AddComponent<VideoPlayer>();
			nextVideoPlayer.playOnAwake = false;
			nextVideoPlayer.isLooping = false;
			nextVideoPlayer.source = VideoSource.Url;
			nextVideoPlayer.controlledAudioTrackCount = 1;
			nextVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
			nextVideoPlayer.SetTargetAudioSource(0, instance.tvSFX);
			nextVideoPlayer.url = $"file://{VideoManager.Videos[(index) % VideoManager.Videos.Count]}";
			nextVideoPlayer.Prepare();
			nextVideoPlayer.prepareCompleted += (VideoPlayer source) => { TVLoaderPlugin.Log.LogInfo("Prepared next video!"); };
		}

		private static void PlayVideo(TVScript instance)
		{
			tvHasPlayedBefore = true;
			if (VideoManager.Videos.Count == 0) return;

			// If the next video is prepared, switch out the videoPlayer
			if (nextVideoPlayer != null)
			{
				var deleteMe = currentVideoPlayer;

				instance.video = currentVideoPlayer = nextVideoPlayer;
				nextVideoPlayer = null;

				TVLoaderPlugin.Log.LogInfo($"Destroy {deleteMe}");
				GameObject.Destroy(deleteMe);

				// Add the EventHandler again
				onEnableMethod.Invoke(instance, new object[] { });
			}

			currentTimeProperty.SetValue(instance, 0f);

			instance.video.targetTexture = renderTexture;
			instance.video.Play();

			PrepareVideo(instance);
		}

	}
}