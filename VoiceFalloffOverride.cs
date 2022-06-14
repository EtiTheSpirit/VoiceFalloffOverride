/*
Voice Falloff Override v0.3
A VRChat modification for MelonLoader
By Adnezz

Modified from Rank Volume Control by dave-kun
https://github.com/dave-kun/RankVolumeControl
Makes use of NetworkManagerHooks.cs from JoinNotifier by Knah
https://github.com/knah/VRCMods/tree/master/JoinNotifier

Both sources, and thus this source, are licensed under GPL-3.0
/*


/*
VRCPlayer[Remote] **ID**
	PlayerAudioManager

field_Private_Single_0 <- Gain
field_Private_Single_1 <- Audio Falloff Range
Method_Private_Void_0 <- Updates state after changing values

Public_Void_PDM_0 <- Appears to make the audio go world space?
*/

//Todo: Research and implement different method of changing audio falloff. 
//  This code currently alters Unity components on the actual Game Objects. 
//  It does not prevent later alteration of the falloff distance.
//  It also does not support properly restoring the distance back to what
//  the world creator intended. 

//  Will need to find a way to proxy or replace the VRChat function that 
//  alters the falloff distance. It's beyond my current skill and knowledge
//  to do so, however. If you're reading this, there's a good chance you 
//  have some idea! Feel free to get in touch or contribute code. :3


using MelonLoader;
using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using VRC;
using VRC.Core;
using HarmonyLib;

namespace VoiceFalloffOverride {

	public static class BuildInfo {
		public const string NAME = "Voice Falloff Override (Xan's Version)";
		public const string AUTHOR = "Adnezz, Xan";
		public const string COMPANY = null;
		public const string VERSION = "1.0.0";
		public const string DOWNLOAD_LINK = "https://github.com/EtiTheSpirit/VoiceFalloffOverride";
	}

	public class VoiceFalloffOverrideMod : MelonMod {

		#region Default Data & States
		public const float DEFAULT_VOICE_RANGE = 25;
		public static float DEFAULT_NEAR_RANGE = 0;
		public static float DEFAULT_GAIN = 15;
		public const bool DEFAULT_PA_STATE = false;
		public const float DEFAULT_PA_RANGE = 100;

		public static bool InitializingWorld { get; set; } = true;
		#endregion

		#region Prefs
		public static bool Enabled => MelonPreferences.GetEntryValue<bool>("VFO", "Enabled");
		public static float VoiceRange { get; set; }
		public static float VoiceNearRange { get; set; }
		public static float VoiceGain { get; set; }
		public static bool PAPassthrough => MelonPreferences.GetEntryValue<bool>("VFO", "PAPassthrough");
		public static float PADetectRange => MelonPreferences.GetEntryValue<float>("VFO", "PADetectRange");
		#endregion

		#region MelonLoader Hooks
		public override void OnApplicationStart() {
			MelonPreferences.CreateCategory("VFO", "Voice Falloff Override");
			MelonPreferences.CreateEntry("VFO", "Enabled", false, "VFO Enabled");
			MelonPreferences.CreateEntry("VFO", "Distance", DEFAULT_VOICE_RANGE, "Falloff Distance", $"Range in meters where volume reaches 0% Default: {DEFAULT_VOICE_RANGE}");
			MelonPreferences.CreateEntry("VFO", "NearDistance", DEFAULT_NEAR_RANGE, "Falloff Start Distance", $"Range in meters where volume begins dropping off. Default: {DEFAULT_NEAR_RANGE}");
			MelonPreferences.CreateEntry("VFO", "Gain", DEFAULT_GAIN, "Gain", $"Gain adjustment. Default: {DEFAULT_GAIN}");
			MelonPreferences.CreateEntry("VFO", "PAPassthrough", DEFAULT_PA_STATE, $"Allow PA Systems (Experimental). Default: {DEFAULT_PA_STATE}");
			MelonPreferences.CreateEntry("VFO", "PADetectRange", 100f, "PA Range Threshold", $"Threshold for detecting a public announcement or whole world voice. Default: {DEFAULT_PA_RANGE}");

			VoiceRange = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
			VoiceNearRange = MelonPreferences.GetEntryValue<float>("VFO", "NearDistance");
			VoiceGain = MelonPreferences.GetEntryValue<float>("VFO", "Gain");
			HarmonyInstance.Patch(typeof(PlayerAudioManager).GetMethod("Method_Private_Void_0"), prefix: new HarmonyMethod(typeof(VoiceFalloffOverrideMod).GetMethod("OnUpdateAvAudio", BindingFlags.Static | BindingFlags.Public)));

			MelonCoroutines.Start(InitializeForGameStartup());
		}

		public override void OnPreferencesSaved() {
			VoiceRange = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
			VoiceNearRange = MelonPreferences.GetEntryValue<float>("VFO", "NearDistance");
			VoiceGain = MelonPreferences.GetEntryValue<float>("VFO", "Gain");
			UpdateAllPlayerVolumes(VoiceRange, VoiceNearRange, VoiceGain);
		}

		public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
			if (buildIndex == -1) {
				// Build index of -1 is true if the scene was loaded from an asset bundle
				// This means that it was not included in the game natively.
				// In the case of VRC, this means it was a world that was downloaded and joined.
				// src: https://docs.unity3d.com/ScriptReference/SceneManagement.Scene-buildIndex.html
				InitializingWorld = true;
				MelonCoroutines.Start(OverwriteSettingsForWorld());
			}
		}
		#endregion

		#region Initialization Sequence

		private IEnumerator InitializeForGameStartup() {
			while (NetworkManager.field_Internal_Static_NetworkManager_0 == null) {
				yield return null;
			}

			MelonLogger.Msg("Initializing Voice Falloff Override...");
			PlayerTrafficHooks.InitializeNetEvents();
			PlayerTrafficHooks.OnJoin += OnPlayerJoined;
		}

		public static IEnumerator OverwriteSettingsForWorld() {
			while (VRCPlayer.field_Internal_Static_VRCPlayer_0 == null) {
				yield return new WaitForSecondsRealtime(2);
			}

			MelonLogger.Msg("Initializing newly joined world...");
			if (Enabled) UpdateAllPlayerVolumes(VoiceRange, VoiceNearRange, VoiceGain);
			InitializingWorld = false;

			yield break;
		}

		#endregion

		#region Volume Update Methods

		public static void UpdateAllPlayerVolumes(float falloffEnd, float falloffStart, float gain) {
			List<Player> players = PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;

			for (int i = 0; i < players.Count; i++) {
				Player player = players.System_Collections_IList_get_Item(i).Cast<Player>();
				if (player != null || player.field_Private_APIUser_0 != null) {
					UpdatePlayerVolume(player, falloffEnd, falloffStart, gain);
				}
			}
		}

		private static void UpdatePlayerVolume(Player player, float falloffEnd, float falloffStart, float gain) {
			PlayerAudioManager plrAudioMgr = player.prop_VRCPlayer_0.prop_PlayerAudioManager_0;
			if (plrAudioMgr == null) {
				MelonDebug.Msg("PlayerAudioManager is missing!");
				return;
			}

			if (!Enabled) {
				plrAudioMgr.Method_Private_Void_0();
			} else {
				AudioSource audioSrc = plrAudioMgr.field_Private_AudioSource_0;
				if (audioSrc == null) {
					MelonLogger.Msg("The player's audio source was missing.");
					return;
				}
				USpeaker speaker = audioSrc.gameObject?.GetComponent<USpeaker>();
				if (speaker == null) {
					MelonLogger.Msg("The player's audio source's parent game object (or its attached USpeaker) was missing.");
					return;
				}

				ONSPAudioSource onspSrc = speaker.field_Private_ONSPAudioSource_0;
				if (onspSrc == null) {
					MelonDebug.Msg("ONSP Source is missing! Yielding up to 10s for existence...");
					WaitToSetONSP(speaker, falloffStart, falloffEnd, gain);
					audioSrc.minDistance = falloffStart;
					audioSrc.maxDistance = falloffEnd;
					return;
				}

				audioSrc.minDistance = falloffStart;
				audioSrc.maxDistance = falloffEnd;
				onspSrc.far = falloffEnd * 2;
				onspSrc.near = falloffStart * 2;
				onspSrc.gain = gain;
			}
		}

		#endregion

		#region Utilities and Events

		private void OnPlayerJoined(Player player) {
			if (!InitializingWorld) {
				if (player != null || player.field_Private_APIUser_0 != null) {
					if (Enabled) {
						UpdatePlayerVolume(player, VoiceRange, VoiceNearRange, VoiceGain);
					}
				}
			}
		}

		public static IEnumerator WaitToSetONSP(USpeaker us, float falloffEnd, float falloffStart, float gain) {
			int count = 0;
			while ((us.field_Private_ONSPAudioSource_0 == null) && (count < 10)) {
				count++;
				yield return new WaitForSecondsRealtime(1);
			}
			if (us.field_Private_ONSPAudioSource_0 != null) {
				ONSPAudioSource a2 = us.field_Private_ONSPAudioSource_0;
				a2.far = falloffEnd * 2;
				a2.near = falloffStart * 2;
				a2.gain = gain;
			} else {
				MelonDebug.Msg("ONSP was not found after 10 seconds. Terminating wait loop.");
			}
			yield break;
		}

		#endregion

	}
}
