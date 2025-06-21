using System;
using Kobold.Core;
using UnityEngine;

namespace Kobold
{
	public class KoboldCanvasManager : MonoBehaviour
	{
		private static KoboldCanvasManager _instance;
		public static KoboldCanvasManager Instance => _instance;
		
		[SerializeField] private UnburyUIFeedback UnburyUI;
		

		protected void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
			}
			else
			{
				Debug.LogError("Multiple KoboldCanvasManagers found. Destroying duplicate.");
				Destroy(gameObject);
				return;
			}
			UnburyUI.gameObject.SetActive(false);
		}

		public void OnPlayerSpawned(UnburyController unburyController)
		{
			UnburyUI.gameObject.SetActive(true);
			UnburyUI.Initialize(unburyController);
		}

		/// <summary>
		/// Show the pause menu
		/// </summary>
		public void OnPlayerPause()
		{
			
		}

		/// <summary>
		/// Hide the pause menu
		/// </summary>
		public void OnPlayerUnpause()
		{
			
		}
	}
}