using System.Collections.Generic;
using UnityEngine;

namespace Kobold.GameManagement
{
	/// <summary>
	///     Manages persistent objects across scene loads and ensures no duplicates exist
	/// </summary>
	public static class KoboldPersistentObjectManager
	{
		private static readonly HashSet<string> RegisteredPersistentObjects = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			RegisteredPersistentObjects.Clear();
			Debug.Log("[KoboldPersistentObjectManager] Registry cleared");
		}

		/// <summary>
		///     Registers a persistent object and destroys it if a duplicate already exists
		/// </summary>
		/// <returns>True if this object should persist, false if it should be destroyed</returns>
		public static bool RegisterPersistentObject(MonoBehaviour obj)
		{
			if (obj == null) return false;

			var objectId = $"{obj.GetType().FullName}";

			// Check if this type is already registered
			if (RegisteredPersistentObjects.Contains(objectId))
			{
				Debug.LogWarning(
					$"[KoboldPersistentObjectManager] Duplicate {objectId} detected. Destroying new instance on {obj.gameObject.name}");
				return false;
			}

			// Register this object
			RegisteredPersistentObjects.Add(objectId);
			Debug.Log($"[KoboldPersistentObjectManager] Registered {objectId}");

			// Clean up on destroy
			obj.gameObject.AddComponent<PersistentObjectCleanup>().Initialize(objectId);

			return true;
		}

		/// <summary>
		///     Unregisters a persistent object when it's destroyed
		/// </summary>
		public static void UnregisterPersistentObject(string objectId)
		{
			if (RegisteredPersistentObjects.Remove(objectId))
				Debug.Log($"[KoboldPersistentObjectManager] Unregistered {objectId}");
		}

		/// <summary>
		///     Helper component to clean up when object is destroyed
		/// </summary>
		private class PersistentObjectCleanup : MonoBehaviour
		{
			private string _objectId;

			private void OnDestroy()
			{
				UnregisterPersistentObject(_objectId);
			}

			public void Initialize(string objectId)
			{
				_objectId = objectId;
			}
		}
	}
}
