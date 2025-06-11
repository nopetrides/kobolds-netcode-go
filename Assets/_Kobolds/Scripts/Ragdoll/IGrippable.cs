using UnityEngine;

namespace Kobolds
{
	public interface IGrippable
	{
		/// <summary>
		/// Called by a GripMagnetPoint trying to attach to this object.
		/// Return true if the attachment is allowed/successful.
		/// </summary>
		bool TryAttach(GripMagnetPoint magnet);

		/// <summary>
		/// Called when the object is being detached from a magnet.
		/// </summary>
		void Detach(GripMagnetPoint magnet);

		/// <summary>
		/// Returns a user-facing string prompt for UI display (e.g. "Press F to Bite").
		/// </summary>
		string GetInteractionPrompt();

		public GameObject GetObject();
	}
}