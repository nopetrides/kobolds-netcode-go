using System.Collections.Generic;
using FIMSpace;
using Unity.Cinemachine;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
*/

namespace Kobolds
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class KoboldLeanController : MonoBehaviour
	{
		private const float Threshold = 0.01f;

		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 2.0f;

		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 5.335f;

		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		public float RotationSmoothTime = 0.12f;

		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		public AudioClip LandingAudioClip;
		public AudioClip[] FootstepAudioClips;
		[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;

		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.50f;

		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;

		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;

		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.28f;

		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;

		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 70.0f;

		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -30.0f;

		[Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
		public float CameraAngleOverride;

		[Tooltip("For locking the camera position on all axis")]
		public bool LockCameraPosition;

		[Header("Aim Camera")]
		[Tooltip("Camera that is used during the aim state")]
		[SerializeField] private CinemachineVirtualCameraBase _aimCamera;

		[Tooltip("How sensitive the camera look is normally")]
		[SerializeField] private float NormalSensitivity = 1f;

		[Tooltip("How sensitive the camera look is when the aim state is active")]
		[SerializeField] private float AimSensitivity = 0.5f;

		// Lean Animator stuff
		[Tooltip("Blend between direct and smoothed movement")]
		[Range(0, 1f)]
		[SerializeField] private float DirectMoveBlend;

		[Tooltip("Blend between direct and smoothed movement (script driven)")]
		[Range(0, 1f)]
		[SerializeField]
		private float _directBlendDelay;

		[Tooltip("Mult to Time.DeltaTime")]
		[SerializeField] private float DirectBlendAccelerationMultiplier;

		[Tooltip("Mult to Time.DeltaTime")]
		[SerializeField] private float DirectBlendDecelerationMultiplier;

		[Tooltip("Blend between direct and smoothed movement when at full acceleration")]
		[Range(0, 1f)]
		[SerializeField] private float DirectBlendMin;

		[Tooltip("Blend between direct and smoothed movement when starting from a stop")]
		[Range(0, 1f)]
		[SerializeField] private float DirectBlendMax;

		[Header("Bones")]
		[Tooltip("List of important joints from hips to head.")]
		[SerializeField] private List<Transform> Joints;

		[Tooltip("Root joint, often the hips.")]
		[SerializeField] private Transform Hips;

		[Tooltip("Top-most joint or head.")]
		[SerializeField] private Transform Head;

		[Header("Head-Body Separation")]
		[Tooltip("How fast the head rotates toward the camera direction when aiming")]
		[SerializeField] private float HeadTrackingSpeed = 180f;

		[Tooltip("Maximum angle the head can rotate from the hips (90° = full left/right range)")]
		[Range(45f, 135f)]
		[SerializeField] private float MaxHeadHipAngle = 90f;

		[Tooltip("How smoothly the hip rotation adjusts when needed")]
		[SerializeField] private float HipAdjustmentSpeed = 90f;

		[Tooltip(
			"Blend factor for how much each spine joint contributes to head tracking (0=no contribution, 1=full contribution)")]
		[Range(0f, 1f)]
		[SerializeField] private float SpineTrackingBlend = 0.7f;

#if FIM
		[SerializeField] private LeaningAnimator LeaningAnim;
#endif

		// Store original bone rotations to blend with
		private readonly Dictionary<Transform, Quaternion> _originalBoneRotations = new();
		private readonly float _terminalVelocity = 53.0f;
		private float _animationBlend;
		private float _animationDirectionMultiplier = 1f;
		private Animator _animator;
		private int _animIDAnimationDirection;
		private int _animIDFreeFall;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDMotionSpeed;

		// animation IDs
		private int _animIDSpeed;
		private float _cinemachineTargetPitch;

		// cinemachine
		private float _cinemachineTargetYaw;
		private CharacterController _controller;

		// Head-body separation variables
		private Quaternion _currentHeadLookRotation;
		private Vector3 _currentMovementDirection;
		private float _fallTimeoutDelta;

		private bool _hasAnimator;
		private KoboldInputs _input;
		private bool _isBackPedaling;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private GameObject _mainCamera;

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private float _rotationVelocity;

		// player
		private float _speed;
		private Vector3 _targetDirection;
		private Quaternion _targetHeadLookRotation;
		private float _targetRotation;
		private float _verticalVelocity;
		private bool _wasAiming;

		private bool IsCurrentDeviceMouse
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
			}
		}


		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null) _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}

		private void Start()
		{
			_cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

			_hasAnimator = TryGetComponent(out _animator);
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<KoboldInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			AssignAnimationIDs();
			InitializeHeadBodySeparation();

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

		private void Update()
		{
			_hasAnimator = TryGetComponent(out _animator);

			JumpAndGravity();
			GroundedCheck();
			Move();
		}

		private void LateUpdate()
		{
			// Handle head-body separation after movement calculations
			if (_input.Aim)
				UpdateHeadBodySeparation();
			else
				ResetHeadBodySeparation();

			// Finalize camera adjustments
			CameraRotation();
		}

		private void OnDrawGizmosSelected()
		{
			var transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			var transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(
				new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
				GroundedRadius);
		}

		private void AssignAnimationIDs()
		{
			_animIDSpeed = Animator.StringToHash("Speed");
			_animIDGrounded = Animator.StringToHash("Grounded");
			_animIDJump = Animator.StringToHash("Jump");
			_animIDFreeFall = Animator.StringToHash("FreeFall");
			_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
			_animIDAnimationDirection = Animator.StringToHash("AnimationDirection");
		}

		private void InitializeHeadBodySeparation()
		{
			if (Head != null)
			{
				_currentHeadLookRotation = Head.rotation;
				_targetHeadLookRotation = Head.rotation;
			}
		}

		private void UpdateHeadBodySeparation()
		{
			if (Head == null || Hips == null) return;

			// Update original rotations every frame to match current animation
			if (Joints != null)
				foreach (var joint in Joints)
					if (joint != null)
						_originalBoneRotations[joint] = joint.localRotation;

			// Calculate target head look direction based on camera
			var clampedPitch = Mathf.Clamp(_cinemachineTargetPitch + 45f, -85f, 85f);
			var cameraForward = Quaternion.Euler(clampedPitch, _cinemachineTargetYaw, 0f) * Vector3.forward;
			_targetHeadLookRotation = Quaternion.LookRotation(cameraForward, Vector3.up);


			// DEBUG: Comprehensive coordinate comparison
			// Debug.Log("=== COORDINATE DEBUG ===");
			// Debug.Log($"Camera World Rotation: {_mainCamera.transform.rotation.eulerAngles}");
			// Debug.Log($"Camera World Forward: {_mainCamera.transform.forward}");
			// Debug.Log($"Cinemachine Target Yaw: {_cinemachineTargetYaw}, Pitch: {_cinemachineTargetPitch}");
			// Debug.Log($"Calculated Camera Forward: {cameraForward}");
			// Debug.Log($"");
			// Debug.Log($"Character Root World Rotation: {transform.rotation.eulerAngles}");
			// Debug.Log($"Character Root World Forward: {transform.forward}");
			// Debug.Log($"");
			// Debug.Log($"Head World Rotation: {Head.rotation.eulerAngles}");
			// Debug.Log($"Head World Forward: {Head.forward}");
			// Debug.Log($"Head Local Rotation: {Head.localRotation.eulerAngles}");
			// Debug.Log($"Head Parent Rotation: {Head.parent.rotation.eulerAngles}");
			// Debug.Log($"");
			// Debug.Log($"Target Head Rotation: {_targetHeadLookRotation.eulerAngles}");
			// Debug.Log($"Target Head Forward: {cameraForward}");
			// Debug.Log("========================");

			//var cameraForward = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f) * Vector3.forward;
			//_targetHeadLookRotation = Quaternion.LookRotation(cameraForward, Vector3.up);

			// Debug.Log($"Camera Forward: {cameraForward}");
			// Debug.Log($"Camera Yaw: {_cinemachineTargetYaw}, Pitch: {_cinemachineTargetPitch}");
			// Debug.Log($"Head Current Forward: {Head.forward}");
			// Debug.Log($"Target Head Forward: {cameraForward}");

			// Smoothly rotate head toward target
			_currentHeadLookRotation = Quaternion.RotateTowards(
				_currentHeadLookRotation,
				_targetHeadLookRotation,
				HeadTrackingSpeed * Time.deltaTime
			);

			// // DEBUG: Check what we're trying to apply
			// Debug.Log($"Target Head Rotation (Euler): {_targetHeadLookRotation.eulerAngles}");
			// Debug.Log($"Current Head Rotation (Euler): {_currentHeadLookRotation.eulerAngles}");
			// Debug.Log($"Head Transform Rotation (Euler): {Head.rotation.eulerAngles}");
			// Debug.Log($"Head Transform LOCAL Rotation (Euler): {Head.localRotation.eulerAngles}");

			//Head.rotation = _currentHeadLookRotation;

			// DEBUG: Check if rotation was actually applied
			// Debug.Log($"Just SET Head rotation to: {_currentHeadLookRotation.eulerAngles}");
			// Debug.Log($"Head rotation NOW is: {Head.rotation.eulerAngles}");
			// Debug.Log($"Are they the same? {Quaternion.Angle(_currentHeadLookRotation, Head.rotation) < 1f}");

			// Calculate angle between head look direction and hip forward direction
			var headForward = _currentHeadLookRotation * Vector3.forward;
			var hipForward = Hips.forward;

			var angleFromHips = Vector3.SignedAngle(hipForward, headForward, Vector3.up);

			// DEBUG: Add these lines
			//Debug.Log($"Head Forward: {headForward}");
			//Debug.Log($"Hip Forward: {hipForward}");
			//Debug.Log($"Angle between them: {angleFromHips}");
			//Debug.Log($"Absolute angle: {Mathf.Abs(angleFromHips)}");
			//Debug.Log($"Max allowed angle: {MaxHeadHipAngle}");
			//Debug.Log($"Should back pedal: {Mathf.Abs(angleFromHips) > MaxHeadHipAngle}");

			// Check if we need back-pedaling
			var shouldBackPedal = Mathf.Abs(angleFromHips) > MaxHeadHipAngle;

			if (shouldBackPedal != _isBackPedaling)
			{
				_isBackPedaling = shouldBackPedal;
				UpdateAnimationDirection();
			}

			// Adjust character rotation if needed to keep head in comfortable range
			if (_isBackPedaling)
			{
				if (_currentMovementDirection.magnitude > 0.1f)
				{
					// When moving: rotate to face opposite to movement direction
					var oppositeDirection = -_currentMovementDirection;
					var targetCharacterRotation = Quaternion.LookRotation(oppositeDirection, Vector3.up);

					transform.rotation = Quaternion.RotateTowards(
						transform.rotation,
						targetCharacterRotation,
						HipAdjustmentSpeed * Time.deltaTime
					);
				}
				else if (Mathf.Abs(angleFromHips) > MaxHeadHipAngle * 0.7f)
				{
					// When not moving: rotate to reduce head-hip angle strain
					// Rotate character toward head's yaw direction to bring angle back under 90°
					headForward = _currentHeadLookRotation * Vector3.forward;
					var projectedHeadForward = new Vector3(headForward.x, 0, headForward.z).normalized;
					var targetCharacterRotation = Quaternion.LookRotation(projectedHeadForward, Vector3.up);

					transform.rotation = Quaternion.RotateTowards(
						transform.rotation,
						targetCharacterRotation,
						HipAdjustmentSpeed * Time.deltaTime
					);
				}
			}
			else
			{
				if (_currentMovementDirection.magnitude > 0.1f)
				{
					headForward = _currentHeadLookRotation * Vector3.forward;
					var projectedHeadForward = new Vector3(headForward.x, 0, headForward.z).normalized;
					var targetCharacterRotation = Quaternion.LookRotation(projectedHeadForward, Vector3.up);

					transform.rotation = Quaternion.RotateTowards(
						transform.rotation,
						targetCharacterRotation,
						HipAdjustmentSpeed * Time.deltaTime
					);
				}
				// When head is getting close to the limit, gradually rotate character to help
				else if (Mathf.Abs(angleFromHips) > MaxHeadHipAngle * 0.7f) // Start rotating at 70% of max angle
				{
					// Rotate character slightly toward the head's look direction to reduce strain
					headForward = _currentHeadLookRotation * Vector3.forward;
					var projectedHeadForward = new Vector3(headForward.x, 0, headForward.z).normalized;
					var targetCharacterRotation = Quaternion.LookRotation(projectedHeadForward, Vector3.up);

					var rotationSpeed = HipAdjustmentSpeed;
					transform.rotation = Quaternion.RotateTowards(
						transform.rotation,
						targetCharacterRotation,
						rotationSpeed * Time.deltaTime
					);
				}
			}

			// Apply head tracking to spine chain
			ApplyHeadTrackingToSpine();
			//Head.rotation = _currentHeadLookRotation;
		}

		private void ApplyHeadTrackingToSpine()
		{
			if (Joints == null || Joints.Count == 0) return;

			// DEBUG: Check what we have stored
			// Debug.Log("=== STORED ROTATIONS DEBUG ===");
			// for (int i = 0; i < Joints.Count; i++)
			// {
			// 	if (Joints[i] != null)
			// 	{
			// 		Debug.Log($"Joint {i} ({Joints[i].name}):");
			// 		Debug.Log($"  Current Local: {Joints[i].localRotation.eulerAngles}");
			// 		Debug.Log($"  Stored Original: {(_originalBoneRotations.ContainsKey(Joints[i]) ? _originalBoneRotations[Joints[i]].eulerAngles.ToString() : "NOT STORED")}");
			// 	}
			// }
			// Debug.Log("========================");

			// Find head in joints list to determine distribution
			var headIndex = -1;
			for (var i = 0; i < Joints.Count; i++)
				if (Joints[i] == Head)
				{
					headIndex = i;
					break;
				}

			if (headIndex == -1) return;

			// Calculate total rotation needed - separate pitch and yaw
			var currentHeadForward = Head.forward;
			var targetHeadForward = _currentHeadLookRotation * Vector3.forward;

			// Calculate yaw (left/right) and pitch (up/down) separately
			var totalYawDifference = Vector3.SignedAngle(
				new Vector3(currentHeadForward.x, 0, currentHeadForward.z).normalized,
				new Vector3(targetHeadForward.x, 0, targetHeadForward.z).normalized,
				Vector3.up
			);

			var totalPitchDifference = Vector3.SignedAngle(
				currentHeadForward,
				targetHeadForward,
				Vector3.Cross(Vector3.up, targetHeadForward)
			);

			// DEBUG: Check the calculated differences
			//Debug.Log($"Total Yaw Difference: {totalYawDifference}°");
			//Debug.Log($"Total Pitch Difference: {totalPitchDifference}°");

			var previousYaw = 0f;
			var previousPitch = 0f;

			// Distribute rotations across spine joints incrementally
			for (var i = 0; i <= headIndex; i++)
			{
				if (Joints[i] == null) continue;

				var normalizedPosition = (float) i / headIndex;
				var blendFactor = Mathf.Pow(normalizedPosition, 0.5f) * SpineTrackingBlend;

				// Calculate target cumulative rotation for this joint
				var targetYaw = totalYawDifference * blendFactor;
				var targetPitch = totalPitchDifference * blendFactor;

				// Calculate only the ADDITIONAL rotation this joint needs
				var additionalYaw = targetYaw - previousYaw;
				var additionalPitch = targetPitch - previousPitch;

				//Debug.Log($"Joint {i}: Additional Yaw={additionalYaw:F1}°, Pitch={additionalPitch:F1}°");

				var partialRotation = Quaternion.Euler(additionalPitch, additionalYaw, 0);
				var originalRotation = _originalBoneRotations[Joints[i]];
				Joints[i].localRotation = originalRotation * partialRotation;

				// Update previous values for next joint
				previousYaw = targetYaw;
				previousPitch = targetPitch;
			}
		}

		private void UpdateAnimationDirection()
		{
			Debug.Log($"UpdateAnimationDirection called. _isBackPedaling: {_isBackPedaling}");

			if (_isBackPedaling)
			{
				_animationDirectionMultiplier = -1f; // Reverse animation
				Debug.Log("Setting animation to REVERSE");
			}
			else
			{
				_animationDirectionMultiplier = 1f; // Forward animation
				Debug.Log("Setting animation to FORWARD");
			}
		}

		private void ResetHeadBodySeparation()
		{
			// Smoothly return to normal state when not aiming
			if (Joints != null)
				foreach (var joint in Joints)
					if (joint != null && _originalBoneRotations.ContainsKey(joint))
						joint.localRotation = Quaternion.Lerp(
							joint.localRotation,
							_originalBoneRotations[joint],
							Time.deltaTime * 2f
						);

			// Reset animation direction
			if (_isBackPedaling)
			{
				_isBackPedaling = false;
				_animationDirectionMultiplier = 1f;
			}
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			var spherePosition = new Vector3(
				transform.position.x, transform.position.y - GroundedOffset,
				transform.position.z);
			Grounded = Physics.CheckSphere(
				spherePosition, GroundedRadius, GroundLayers,
				QueryTriggerInteraction.Ignore);

			// update animator if using character
			if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
		}

		// Previous camera rotation logic
		private void CameraRotation()
		{
			// Always update Cinemachine camera angles, even when there is no look input
			var deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
			var sensitivityMultiplier = _input.Aim ? AimSensitivity : NormalSensitivity;

			if (_input.Look.sqrMagnitude >= Threshold)
			{
				// Adjust camera yaw and pitch based on input
				_cinemachineTargetYaw += _input.Look.x * deltaTimeMultiplier * sensitivityMultiplier;
				_cinemachineTargetPitch += _input.Look.y * deltaTimeMultiplier * sensitivityMultiplier;
			}

			// Clamp rotations so values are within valid ranges
			_cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// Always update the Cinemachine camera's rotation
			CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
				_cinemachineTargetPitch + CameraAngleOverride,
				_cinemachineTargetYaw,
				0.0f
			);
		}

		private void Move()
		{
			if (_aimCamera)
				_aimCamera.gameObject.SetActive(_input.Aim);

			// Set speed based on movement and sprinting
			var targetSpeed = _input.Sprint ? SprintSpeed : MoveSpeed;
			if (_input.Move == Vector2.zero) targetSpeed = 0.0f;

			// Smooth the player's speed transitions
			var currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
			var inputMagnitude = _input.analogMovement ? _input.Move.magnitude : 1f;

			if (currentHorizontalSpeed < targetSpeed - 0.1f || currentHorizontalSpeed > targetSpeed + 0.1f)
			{
				_speed = Mathf.Lerp(
					currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// Normalize input direction
			var inputDirection = new Vector3(_input.Move.x, 0.0f, _input.Move.y).normalized;

			Vector3 targetDirection;
			if (_input.Aim)
			{
				// DON'T rotate the character root when aiming - let head track independently
				// Character root stays in current orientation

				// Strafing logic in aiming mode (using current character orientation)
				targetDirection = transform.right * inputDirection.x + transform.forward * inputDirection.z;
			}
			/*if (_input.aim)
			{
				// Smoothly rotate the character to align with camera when aiming
				var targetRotation = Quaternion.Euler(0f, _cinemachineTargetYaw, 0f);
				transform.rotation = Quaternion.RotateTowards(
					transform.rotation, targetRotation, AimRotationSpeed * Time.deltaTime);

				// Strafing logic in aiming mode
				targetDirection = transform.right * inputDirection.x + transform.forward * inputDirection.z;
			}*/
			else
			{
				// Standard character movement logic when not aiming
				if (_input.Move != Vector2.zero)
				{
					_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
									_mainCamera.transform.eulerAngles.y;
					var rotation = Mathf.SmoothDampAngle(
						transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
					transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
				}

				// Standard direction calculation
				targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
			}

			// Store current movement direction for head-body separation
			_currentMovementDirection = targetDirection.normalized;

			// Move the character
			_controller.Move(
				targetDirection.normalized * (_speed * Time.deltaTime) +
				new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
			);

			// Update animator for movement and aiming
			if (_hasAnimator)
			{
				_animator.SetFloat(_animIDSpeed, _speed);
				_animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

				// Combine motion speed with direction for animation control
				var finalAnimationValue = inputMagnitude * _animationDirectionMultiplier;
				_animator.SetFloat(_animIDAnimationDirection, finalAnimationValue);

				// DEBUG: Add these lines
				// Debug.Log($"Input Magnitude: {inputMagnitude}");
				// Debug.Log($"Animation Direction Multiplier: {_animationDirectionMultiplier}");
				// Debug.Log($"Final Animation Value sent to animator: {finalAnimationValue}");
				// Debug.Log($"Current animator AnimationDirection value: {_animator.GetFloat(_animIDAnimationDirection)}");
			}
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// update animator if using character
				if (_hasAnimator)
				{
					_animator.SetBool(_animIDJump, false);
					_animator.SetBool(_animIDFreeFall, false);
				}

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

				// Jump
				if (_input.Jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

					// update animator if using character
					if (_hasAnimator) _animator.SetBool(_animIDJump, true);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}
				else
				{
					// update animator if using character
					if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
				}

				// if we are not grounded, do not jump
				_input.Jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnFootstep(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight > 0.5f)
				if (FootstepAudioClips.Length > 0)
				{
					var index = Random.Range(0, FootstepAudioClips.Length);
					AudioSource.PlayClipAtPoint(
						FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
				}
		}

		private void OnLand(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight > 0.5f)
				AudioSource.PlayClipAtPoint(
					LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
		}
	}
}
