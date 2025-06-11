using Unity.Netcode;
using UnityEngine;

namespace Kobold.Net
{
	/// <summary>
	///     Synchronizes key bone transforms for Kobold characters.
	///     More efficient than using NetworkTransform for specific bones.
	/// </summary>
	public class KoboldTransformSync : NetworkBehaviour
	{
		[Header("Bone References")]
		[SerializeField] private Transform _mouthBone;

		[SerializeField] private Transform _leftHandBone;
		[SerializeField] private Transform _rightHandBone;

		[Header("Sync Settings")]
		[SerializeField] private float _sendRate = 15f; // Hz

		[SerializeField] private bool _interpolate = true;
		[SerializeField] private float _interpolationSpeed = 10f;
		
		// Network variables for each bone's local position and rotation
		private readonly NetworkVariable<Vector3> _mouthLocalPos = new();
		private readonly NetworkVariable<Quaternion> _mouthLocalRot = new();

		private readonly NetworkVariable<Vector3> _leftHandLocalPos = new();
		private readonly NetworkVariable<Quaternion> _leftHandLocalRot = new();


		private readonly NetworkVariable<Vector3> _rightHandLocalPos = new();
		private readonly NetworkVariable<Quaternion> _rightHandLocalRot = new();
		
		private float _nextSendTime;

		
		private Vector3 _targetLeftHandPos;
		private Quaternion _targetLeftHandRot;

		// For interpolation
		private Vector3 _targetMouthPos;
		private Quaternion _targetMouthRot;
		private Vector3 _targetRightHandPos;
		private Quaternion _targetRightHandRot;

		private void Update()
		{
			if (IsOwner)
			{
				// Send updates at specified rate
				if (Time.time >= _nextSendTime)
				{
					UpdateBoneTransforms();
					_nextSendTime = Time.time + 1f / _sendRate;
				}
			}
			else if (_interpolate)
			{
				// Interpolate to received values
				InterpolateBones();
			}
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			// Initialize interpolation targets
			if (_mouthBone != null)
			{
				_targetMouthPos = _mouthBone.localPosition;
				_targetMouthRot = _mouthBone.localRotation;
			}

			if (_leftHandBone != null)
			{
				_targetLeftHandPos = _leftHandBone.localPosition;
				_targetLeftHandRot = _leftHandBone.localRotation;
			}

			if (_rightHandBone != null)
			{
				_targetRightHandPos = _rightHandBone.localPosition;
				_targetRightHandRot = _rightHandBone.localRotation;
			}

			// Subscribe to value changes for remote players
			if (!IsOwner)
			{
				_mouthLocalPos.OnValueChanged += OnMouthTransformChanged;
				_mouthLocalRot.OnValueChanged += OnMouthRotationChanged;

				_leftHandLocalPos.OnValueChanged += OnLeftHandPositionChanged;
				_leftHandLocalRot.OnValueChanged += OnLeftHandRotationChanged;

				_rightHandLocalPos.OnValueChanged += OnRightHandPositionChanged;
				_rightHandLocalRot.OnValueChanged += OnRightHandRotationChanged;
			}
		}

		public override void OnNetworkDespawn()
		{
			if (!IsOwner)
			{
				_mouthLocalPos.OnValueChanged -= OnMouthTransformChanged;
				_mouthLocalRot.OnValueChanged -= OnMouthRotationChanged;

				_leftHandLocalPos.OnValueChanged -= OnLeftHandPositionChanged;
				_leftHandLocalRot.OnValueChanged -= OnLeftHandRotationChanged;

				_rightHandLocalPos.OnValueChanged -= OnRightHandPositionChanged;
				_rightHandLocalRot.OnValueChanged -= OnRightHandRotationChanged;
			}

			base.OnNetworkDespawn();
		}

		private void UpdateBoneTransforms()
		{
			// Update mouth bone
			if (_mouthBone != null)
			{
				_mouthLocalPos.Value = _mouthBone.localPosition;
				_mouthLocalRot.Value = _mouthBone.localRotation;
			}

			// Update left hand
			if (_leftHandBone != null)
			{
				_leftHandLocalPos.Value = _leftHandBone.localPosition;
				_leftHandLocalRot.Value = _leftHandBone.localRotation;
			}

			// Update right hand
			if (_rightHandBone != null)
			{
				_rightHandLocalPos.Value = _rightHandBone.localPosition;
				_rightHandLocalRot.Value = _rightHandBone.localRotation;
			}
		}

		private void InterpolateBones()
		{
			var t = Time.deltaTime * _interpolationSpeed;

			// Interpolate mouth
			if (_mouthBone != null)
			{
				_mouthBone.localPosition = Vector3.Lerp(_mouthBone.localPosition, _targetMouthPos, t);
				_mouthBone.localRotation = Quaternion.Lerp(_mouthBone.localRotation, _targetMouthRot, t);
			}

			// Interpolate hands
			if (_leftHandBone != null)
			{
				_leftHandBone.localPosition = Vector3.Lerp(_leftHandBone.localPosition, _targetLeftHandPos, t);
				_leftHandBone.localRotation = Quaternion.Lerp(_leftHandBone.localRotation, _targetLeftHandRot, t);
			}

			if (_rightHandBone != null)
			{
				_rightHandBone.localPosition = Vector3.Lerp(_rightHandBone.localPosition, _targetRightHandPos, t);
				_rightHandBone.localRotation = Quaternion.Lerp(_rightHandBone.localRotation, _targetRightHandRot, t);
			}
		}

		// Callbacks for value changes
		private void OnMouthTransformChanged(Vector3 previous, Vector3 current)
		{
			_targetMouthPos = current;
			if (!_interpolate && _mouthBone != null)
				_mouthBone.localPosition = current;
		}

		private void OnMouthRotationChanged(Quaternion previous, Quaternion current)
		{
			_targetMouthRot = current;
			if (!_interpolate && _mouthBone != null)
				_mouthBone.localRotation = current;
		}

		private void OnLeftHandPositionChanged(Vector3 previous, Vector3 current)
		{
			_targetLeftHandPos = current;
			if (!_interpolate && _leftHandBone != null)
				_leftHandBone.localPosition = current;
		}

		private void OnLeftHandRotationChanged(Quaternion previous, Quaternion current)
		{
			_targetLeftHandRot = current;
			if (!_interpolate && _leftHandBone != null)
				_leftHandBone.localRotation = current;
		}

		private void OnRightHandPositionChanged(Vector3 previous, Vector3 current)
		{
			_targetRightHandPos = current;
			if (!_interpolate && _rightHandBone != null)
				_rightHandBone.localPosition = current;
		}

		private void OnRightHandRotationChanged(Quaternion previous, Quaternion current)
		{
			_targetRightHandRot = current;
			if (!_interpolate && _rightHandBone != null)
				_rightHandBone.localRotation = current;
		}
	}
}
