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
		static FieldInfo currentClipProperty = typeof(TVScript).GetField("currentClip", BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo currentTimeProperty = typeof(TVScript).GetField("currentClipTime", BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo wasLastFrameProp = typeof(TVScript).GetField("wasTvOnLastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo timeSinceTurningOffTVProp = typeof(TVScript).GetField("timeSinceTurningOffTV", BindingFlags.NonPublic | BindingFlags.Instance);
		static MethodInfo setMatProperty = typeof(TVScript).GetMethod("SetTVScreenMaterial", BindingFlags.NonPublic | BindingFlags.Instance);



		[HarmonyPatch(typeof(TVScript), "Update")]
		[HarmonyPrefix]
		public static bool Update(TVScript __instance) => false;
		//{

		//	if (NetworkManager.Singleton.ShutdownInProgress || GameNetworkManager.Instance.localPlayerController == null)
		//		return false;

		//	if (!__instance.tvOn || GameNetworkManager.Instance.localPlayerController.isInsideFactory)
		//	{
		//		if ((bool)wasLastFrameProp.GetValue(__instance))
		//		{
		//			wasLastFrameProp.SetValue(__instance, false);
		//			setMatProperty.Invoke(__instance, new object[] { false });
		//			currentTimeProperty.SetValue(__instance, (float)__instance.video.time);
		//			__instance.video.Stop();
		//		}
		//		if (__instance.IsServer && !__instance.tvOn)
		//		{
		//			float timeSince = (float)timeSinceTurningOffTVProp.GetValue(__instance);
		//			timeSinceTurningOffTVProp.SetValue(__instance, timeSince + Time.deltaTime);
		//		}
		//		float curTime = (float)currentClipProperty.GetValue(__instance);
		//		curTime += Time.deltaTime;
		//		currentTimeProperty.SetValue(__instance, curTime);

		//		//if ((double)curTime > VideoManager.Videos[currentClip].length)
		//		//{
		//		//	currentClip = (currentClip + 1) % tvClips.Length;
		//		//	currentClipTime = 0f;
		//		//	if (tvOn)
		//		//	{
		//		//		tvSFX.clip = tvAudioClips[currentClip];
		//		//		tvSFX.Play();
		//		//	}
		//		//}
		//	}
		//	else
		//	{
		//		if (!(bool)wasLastFrameProp.GetValue(__instance))
		//		{
		//			wasLastFrameProp.SetValue(__instance, true);
		//			setMatProperty.Invoke(__instance, new object[] { true });
		//			__instance.video.url = $"file://{VideoManager.Videos[(int)currentClipProperty.GetValue(__instance)]}";
		//			__instance.video.time = (float)currentClipProperty.GetValue(__instance); ;
		//			__instance.video.Play();
		//		}
		//		currentClipProperty.SetValue(__instance, (float)__instance.video.time);
		//	}
		//	return false;
		//}

		[HarmonyPatch(typeof(TVScript), "TurnTVOnOff")]
		[HarmonyPrefix]
		public static bool TurnTVOnOff(TVScript __instance, bool on)
		{
			if (VideoManager.Videos.Count == 0) return false;

			if (__instance.video.source != VideoSource.Url)
			{
				__instance.video.clip = null;
				__instance.tvSFX.clip = null;
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
			if (VideoManager.Videos.Count == 0) return;

			TVLoaderPlugin.Log.LogInfo($"Playing next video...");
			int currentClip = (int)currentClipProperty.GetValue(instance);
			currentClip = (currentClip + 1) % VideoManager.Videos.Count;
			TVLoaderPlugin.Log.LogInfo($"currentClip: {currentClip} - {VideoManager.Videos[currentClip]}");

			currentTimeProperty.SetValue(instance, 0f);
			currentClipProperty.SetValue(instance, currentClip);

			instance.tvSFX.time = 0f;

			instance.video.url = $"file://{VideoManager.Videos[currentClip]}";
			instance.video.source = VideoSource.Url;
			instance.video.controlledAudioTrackCount = 1;
			instance.video.audioOutputMode = VideoAudioOutputMode.AudioSource;
			instance.video.SetTargetAudioSource(0, instance.tvSFX);

			instance.video.Play();

			instance.SyncTVServerRpc();
		}

	}
}