using System;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.Core;

namespace VoiceFalloffOverride {
	public static class PlayerTrafficHooks {
		private static bool IsInitialized;
		private static bool SeenFire;
		private static bool AFiredFirst;

		public static event Action<Player> OnJoin;
		public static event Action<Player> OnLeave;

		public static void EventHandlerA(Player player) {
			if (!SeenFire) {
				AFiredFirst = true;
				SeenFire = true;
			}

			if (player == null) return;
			(AFiredFirst ? OnJoin : OnLeave)?.Invoke(player);
		}

		public static void EventHandlerB(Player player) {
			if (!SeenFire) {
				AFiredFirst = false;
				SeenFire = true;
			}

			if (player == null) return;
			(AFiredFirst ? OnLeave : OnJoin)?.Invoke(player);
		}

		public static void InitializeNetEvents() {
			if (IsInitialized) return;

			NetworkManager mgr = NetworkManager.field_Internal_Static_NetworkManager_0;
			if (mgr == null) return;

			VRCEventDelegate<Player> field0 = mgr.field_Internal_VRCEventDelegate_1_Player_0;
			VRCEventDelegate<Player> field1 = mgr.field_Internal_VRCEventDelegate_1_Player_1;

			AddDelegate(field0, EventHandlerA);
			AddDelegate(field1, EventHandlerB);

			IsInitialized = true;
		}

		private static void AddDelegate(VRCEventDelegate<Player> field, Action<Player> eventHandlerA) {
			field.field_Private_HashSet_1_UnityAction_1_T_0.Add(eventHandlerA);
		}
	}
}