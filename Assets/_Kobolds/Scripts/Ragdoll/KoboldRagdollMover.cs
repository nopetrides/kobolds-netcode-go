using System;
using FIMSpace;
using Kobold.Cam;
using UnityEngine;

namespace Kobold
{
	[DefaultExecutionOrder(-100)]
	public class RagdollMover : FimpossibleComponent
	{
		private static readonly int Speed = Animator.StringToHash("Speed");
		private static readonly int Moving = Animator.StringToHash("Moving");
		private static readonly int Grounded = Animator.StringToHash("Grounded");
		private static readonly int Jump = Animator.StringToHash("Jump");

		private KoboldInputs Inputs { get; set; }
		[SerializeField] private Net.KoboldNetworkController _networkController;
		[SerializeField] private KoboldStateManager _koboldStateManager;
		
		public Rigidbody Rigb;

		[Space(4)]
		public float MovementSpeed = 2f;

		[Range(0f, 1f)]
		public float RotateToSpeed = 0.8f;

		[Tooltip(
			"When true, applying rotation by rigidbody.rotation = ...\nWhen false, applying rotation using angular velocity (smoother interpolation)")]
		public bool FixedRotation = true;

		[Range(0f, 1f)] public float DirectMovement;
		[Range(0f, 1f)] public float Interia = 1f;

		// ReSharper disable once ShiftExpressionZeroLeftOperand
		[Space(4)] public LayerMask GroundMask = 0 >> 1;

		[Space(4)] public float ExtraRaycastDistance = 0.01f;

		[Tooltip("Using Spherecast is Radius greater than zero")]
		public float RaycastRadius;

		[Space(10)]
		[Tooltip("Setting 'Grounded','Moving' and 'Speed' parameters for mecanim")]
		public Animator Mecanim;

		[Tooltip("Animator property which will not allowing character movement is set to true")]
		public string IsBusyProperty = "";

		public bool DisableRootMotion;

		[Space(6)]
		public bool UpdateInput = true;

		[Space(1)]
		public float JumpPower = 3f;

		public float HoldShiftForSpeed;
		public float HoldCtrlForSpeed;

		private float _jumpTime = -1f;

		private float _rotationAngle;
		private float _sdRotationAngle;
		private Quaternion _targetInstantRotation;

		private Quaternion _targetRotation;
		private float _toJump;

		private bool _wasInitialized;

		private bool _wasRootMotion;

		private float _jumpRequest;

		private Camera _mainCamera;

		// Movement Calculation Params

		private Vector2 _moveDirectionLocal = Vector3.zero;

		private readonly Action _onJump = null;

		private Vector3 MoveDirectionWorld { get; set; }
		private Vector3 CurrentWorldAccel { get; set; }

		private bool IsGrounded { get; set; } = true;

		private void Start()
		{
			if (!Rigb) Rigb = GetComponent<Rigidbody>();
			if (!_mainCamera) _mainCamera = KoboldCameraManager.Instance.Cam;
			if (Rigb)
			{
				Rigb.maxAngularVelocity = 30f;
				if (Rigb.interpolation == RigidbodyInterpolation.None)
					Rigb.interpolation = RigidbodyInterpolation.Interpolate;
				// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags // they should be flags
				Rigb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
			}

			IsGrounded = true;
			_targetRotation = Rigb.transform.rotation;
			_targetInstantRotation = Rigb.transform.rotation;
			_rotationAngle = Rigb.transform.eulerAngles.y;
			
			Inputs = KoboldInputSystemManager.Instance.Inputs;

			if (Mecanim) Mecanim.SetBool(Grounded, true);

			_wasInitialized = true;
		}

		protected virtual void Update()
		{
			if (KoboldInputSystemManager.Instance.IsInUIMode) return;
			if (!Rigb) return;
			
			// Safety check
			if (_networkController != null && !_networkController.IsOwner)
			{
				enabled = false;
				return;
			}

			var updateMovement = true;
			if (Mecanim)
				if (string.IsNullOrWhiteSpace(IsBusyProperty) == false)
					updateMovement = !Mecanim.GetBool(IsBusyProperty);

			if (UpdateInput && updateMovement)
			{
				if (Inputs.Jump)
				{
					Inputs.Jump = false;
					if (_toJump <= 0f)
					{
						_jumpRequest = JumpPower;
						_toJump = 0f;
					}
				}

				_moveDirectionLocal = Inputs.Move;
				
				var flatCamRot = _mainCamera != null ? 
					Quaternion.Euler(0f, _mainCamera.transform.eulerAngles.y, 0f) : 
					Quaternion.identity;

				if (_moveDirectionLocal != Vector2.zero)
				{
					_moveDirectionLocal.Normalize();
					MoveDirectionWorld = flatCamRot * new Vector3(_moveDirectionLocal.x, 0f, _moveDirectionLocal.y);
				}
				else
				{
					MoveDirectionWorld = Vector3.zero;
				}

				if (MoveDirectionWorld != Vector3.zero)
					_targetInstantRotation = Quaternion.LookRotation(MoveDirectionWorld);
			}
			else if (updateMovement == false)
			{
				MoveDirectionWorld = Vector3.zero;
			}


			bool moving = MoveDirectionWorld != Vector3.zero;

			if (RotateToSpeed > 0f)
				if (CurrentWorldAccel != Vector3.zero)
				{
					_rotationAngle = Mathf.SmoothDampAngle(
						_rotationAngle, _targetInstantRotation.eulerAngles.y, ref _sdRotationAngle,
						Mathf.Lerp(0.5f, 0.01f, RotateToSpeed));
					_targetRotation = Quaternion.Euler(
						0f, _rotationAngle,
						0f); // Quaternion.RotateTowards(targetRotation, targetInstantRotation, Time.deltaTime * 90f * RotateToSpeed);
				}

			if (Mecanim) Mecanim.SetBool(Moving, moving);

			var spd = MovementSpeed;

			if (UpdateInput)
			{
				if (HoldShiftForSpeed != 0f)
					if (Inputs.Sprint)
						spd = HoldShiftForSpeed;
				if (HoldCtrlForSpeed != 0f)
					if (Inputs.Walk)
						spd = HoldCtrlForSpeed;
			}

			var accel = 5f * MovementSpeed;
			if (!moving) accel = 7f * MovementSpeed;

			if (Interia < 1f)
				CurrentWorldAccel = Vector3.Lerp(
					Vector3.Slerp(CurrentWorldAccel, MoveDirectionWorld * spd, Time.deltaTime * accel),
					Vector3.MoveTowards(CurrentWorldAccel, MoveDirectionWorld * spd, Time.deltaTime * accel), Interia);
			else
				CurrentWorldAccel = Vector3.MoveTowards(
					CurrentWorldAccel, MoveDirectionWorld * spd, Time.deltaTime * accel);

			if (Mecanim && _networkController)
			{
				if (_networkController.IsOwner)
				{
					float speed = moving ? CurrentWorldAccel.magnitude : Rigb.linearVelocity.magnitude;
					Mecanim.SetFloat(Speed, speed);
				}
				//else
				//	NetworkAnimator should take care of it
				
			}

			MoveDirectionWorld = Vector3.zero;
		}


		private void FixedUpdate()
		{
			if (!Rigb) return;

			var targetVelo = CurrentWorldAccel;

			var yAngleDiff = Mathf.DeltaAngle(Rigb.rotation.eulerAngles.y, _targetInstantRotation.eulerAngles.y);
			var directMovement = DirectMovement;

			directMovement *= Mathf.Lerp(1f, Mathf.InverseLerp(180f, 50f, Mathf.Abs(yAngleDiff)), Interia);

			targetVelo = Vector3.Lerp(targetVelo, Rigb.transform.forward * targetVelo.magnitude, directMovement);
			targetVelo.y = Rigb.linearVelocity.y;

			_toJump -= Time.fixedDeltaTime;

			if (_jumpRequest != 0f && _toJump <= 0f)
			{
				Rigb.position += Rigb.transform.up * (_jumpRequest * 0.01f);
				targetVelo.y = _jumpRequest;
				IsGrounded = false;
				_jumpRequest = 0f;
				_jumpTime = Time.time;
				if (Mecanim) Mecanim.SetBool(Jump, true);
				if (Mecanim) Mecanim.SetBool(Grounded, false);
				_onJump?.Invoke();
			}
			else
			{
				if (IsGrounded) // Basic not recommended but working solution - snapping to the ground (this approach will push player down quick when loosing ground)
					targetVelo.y -= 2.5f * Time.fixedDeltaTime;
			}

			if (_wasRootMotion == false)
				if (Rigb.isKinematic == false)
					Rigb.linearVelocity = targetVelo;

			if (FixedRotation)
				Rigb.rotation = _targetRotation;
			else
				Rigb.angularVelocity = Rigb.rotation.QToAngularVelocity(_targetRotation, true);

			if (Time.time - _jumpTime > 0.2f)
			{
				CheckGroundedState();
			}
			else
			{
				if (IsGrounded)
				{
					IsGrounded = false;
					if (Mecanim) Mecanim.SetBool(Grounded, false);
				}
			}
		}

		private void OnEnable()
		{
			if (!_wasInitialized) return;
			ResetTargetRotation();
			Rigb.isKinematic = false;
			Rigb.detectCollisions = true;
			if (Mecanim) Mecanim.SetBool(Jump, false);
			IsGrounded = true;
			if (Mecanim) IsGrounded = Mecanim.GetBool(Grounded);
			CheckGroundedState();
		}

		private void OnDisable()
		{
			if (_koboldStateManager.CurrentState != KoboldState.Uninitialized &&
				_koboldStateManager.CurrentState != KoboldState.Unburying)
			{
				Rigb.isKinematic = true;
			}
			Rigb.detectCollisions = true;
		}

		private void OnAnimatorMove()
		{
			if (DisableRootMotion) return;
			if (Mecanim.deltaPosition.magnitude > Time.unscaledDeltaTime * 0.1f) _wasRootMotion = true;
			else _wasRootMotion = false;
			Mecanim.ApplyBuiltinRootMotion();
		}

		private void SetTargetRotation(Vector3 dir)
		{
			_targetInstantRotation = Quaternion.LookRotation(dir);
			if (CurrentWorldAccel == Vector3.zero) CurrentWorldAccel = new Vector3(0.0000001f, 0f, 0f);
		}

		public void SetRotation(Vector3 dir)
		{
			_targetInstantRotation = Quaternion.LookRotation(dir);
			_rotationAngle = _targetInstantRotation.eulerAngles.y;
			_targetRotation = Quaternion.Euler(0f, _rotationAngle, 0f);
		}

		public void MoveTowards(Vector3 wPos, bool setDir = true)
		{
			var tPos = new Vector3(wPos.x, 0f, wPos.z);
			var mPos = new Vector3(Rigb.transform.position.x, 0f, Rigb.transform.position.z);
			var dir = (tPos - mPos).normalized;
			MoveDirectionWorld = dir;
			if (setDir) SetTargetRotation(dir);
		}

		public void ResetTargetRotation()
		{
			_targetRotation = Rigb.transform.rotation;
			_targetInstantRotation = Rigb.transform.rotation;
			_rotationAngle = Rigb.transform.eulerAngles.y;

			CurrentWorldAccel = Vector3.zero;
			_jumpRequest = 0f;
		}

		private void CheckGroundedState()
		{
			if (DoRaycast())
			{
				if (IsGrounded == false)
				{
					if (Mecanim) Mecanim.SetBool(Jump, false);
					IsGrounded = true;
					if (Mecanim) Mecanim.SetBool(Grounded, true);
				}
			}
			else
			{
				if (IsGrounded)
				{
					IsGrounded = false;
					if (Mecanim) Mecanim.SetBool(Grounded, false);
				}
			}
		}

		private bool DoRaycast()
		{
			if (RaycastRadius <= 0f)
				return Physics.Raycast(
					Rigb.transform.position + Rigb.transform.up, -Rigb.transform.up,
					(IsGrounded ? 1.2f : 1.01f) + ExtraRaycastDistance, GroundMask, QueryTriggerInteraction.Ignore);

			return Physics.SphereCast(
				new Ray(Rigb.transform.position + Rigb.transform.up, -Rigb.transform.up), RaycastRadius,
				(IsGrounded ? 1.2f : 1.01f) + ExtraRaycastDistance - RaycastRadius * 0.5f, GroundMask,
				QueryTriggerInteraction.Ignore);
		}

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying) return;
			if (Mecanim)
			{
				Gizmos.color = Mecanim.GetBool(Grounded) ? Color.green : Color.red;
				Gizmos.DrawRay(Rigb.transform.position + Rigb.transform.up, -Rigb.transform.up * (IsGrounded ? 1.2f : 1.01f));
			}
		}
	}
}
