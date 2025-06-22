using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kobold
{
	public class KoboldCameraShootObject : MonoBehaviour
	{
		[SerializeField] private Rigidbody _toShootPrefab;
		[SerializeField] private float _velocity = 10f;
		[SerializeField] private int _floor = -10;
		private Camera _cam;

		private readonly List<Rigidbody> _created = new();

		private KoboldInputs _inputs;

		private void Awake()
		{
			_inputs = KoboldInputSystemManager.Instance?.Inputs;
			if (!_cam) _cam = Camera.main;
		}

		private void Update()
		{
			// backwards iteration is safest and fastest
			for (var i = _created.Count - 1; i >= 0; i--)
				if (_created[i].transform.position.y < _floor)
				{
					Destroy(_created[i].gameObject);
					_created.RemoveAt(i);
				}

			if (_inputs.Fire)
			{
				_inputs.Fire = false;
				if (!_cam) return;

				var ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
				RaycastHit hit;

				if (Physics.Raycast(ray.origin, ray.direction, out hit))
				{
					var targetPos = hit.point;

					var r = Instantiate(_toShootPrefab);
					_created.Add(r);
					var dir = targetPos - transform.position;
					dir.Normalize();
					r.position = transform.position + dir;

					r.AddForce(dir * _velocity, ForceMode.VelocityChange);
				}
			}
		}
	}
}
