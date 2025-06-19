using System;
using UnityEngine;

namespace Kobolds.Gameplay
{
	public class KoboldPlayerSpawnPoints : MonoBehaviour
	{
		internal static KoboldPlayerSpawnPoints Instance;

		[SerializeField]
		Transform[] m_PlayerSpawnPoints;

		void Awake()
		{
			Instance = this;
		}

		internal Transform GetRandomSpawnPoint()
		{
			if (m_PlayerSpawnPoints.Length == 0)
			{
				throw new Exception("No player Transforms found in m_PlayerSpawnPoints");
			}
			return m_PlayerSpawnPoints[UnityEngine.Random.Range(0, m_PlayerSpawnPoints.Length)];
		}
	}
}
