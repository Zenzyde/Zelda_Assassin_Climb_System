using System.Data;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : PlayerMovementBase
{
	[Header("Player grounded movement settings")]
	[SerializeField] float jumpMoveControlFactor = 0.5f;
	[SerializeField] float highJumpMultiplier = 0.3f;
	[SerializeField] float highJumpBufferTime = 0.2f;
	[SerializeField] float coyoteBufferTime = 0.15f;
	[SerializeField][Range(-1, 1)] float minSlopeDetectionAngle = 0.65f;
	[SerializeField] float slopeUpwalkSpeedMultiplier = 0.6f;
	[SerializeField] float slopeDownwalkSpeedMultiplier = 1.2f;
	[SerializeField][Range(0.1f, 1f)] float lookRotationSpeed = 0.7f;

	private bool bIsGrounded;
	private CameraController cameraController;
	private Vector3 capsuleCastDirection;
	private Vector3 movementDirection;
	private Vector3 movement;
	private bool bJumpPressed = false;
	private Vector3 highJumpPhysics;
	private Vector3 normalJumpPhysics;
	private float highJumpBufferTimer = 0;
	private bool bCoyoteJumpAttempted = false;
	private bool bDisableCoyoteJump = false;
	private bool bIsOnSlope = false;

	private CancellationTokenSource highJumpTimerTokenSource;
	private UniTask.Awaiter highJumpTimerAwaiter;
	private CancellationTokenSource coyoteTimerTokenSource;
	private UniTask.Awaiter coyoteTimerAwaiter;

	public override void Enter()
	{
		base.Enter();
		Controller.rigidbody.useGravity = true;
		Controller.rigidbody.isKinematic = false;
		Controller.rigidbody.velocity = Vector3.zero;
		Controller.rigidbody.angularVelocity = Vector3.zero;
		Controller.rigidbody.constraints = RigidbodyConstraints.FreezeRotationX ^ RigidbodyConstraints.FreezeRotationZ;
	}

	public override void Exit()
	{
		base.Exit();
		Controller.rigidbody.velocity = Vector3.zero;
		Controller.rigidbody.angularVelocity = Vector3.zero;
		movementDirection = Vector3.zero;
		movement = Vector3.zero;
		capsuleCastDirection = Vector3.zero;
		Controller.rigidbody.constraints = RigidbodyConstraints.FreezeRotationX ^ RigidbodyConstraints.FreezeRotationZ;
	}

	private void CreateHighJumpTimerToken(ref CancellationTokenSource source) => source = new CancellationTokenSource();

	private void CancelHighJumpTimer()
	{
		if (highJumpTimerTokenSource != null)
		{
			highJumpTimerTokenSource.Cancel();
			highJumpTimerTokenSource.Dispose();
			highJumpTimerTokenSource = null;
		}
	}

	private async UniTask PerformHighJumpTiming()
	{
		if (highJumpTimerTokenSource == null)
			CreateHighJumpTimerToken(ref highJumpTimerTokenSource);

		Controller.rigidbody.AddForce(Vector3.up * JumpStrength, ForceMode.Impulse);

		while (!bIsGrounded && highJumpBufferTimer > 0.0f && !highJumpTimerTokenSource.IsCancellationRequested)
		{
			highJumpBufferTimer -= Time.deltaTime;

			await UniTask.Yield(PlayerLoopTiming.Update, highJumpTimerTokenSource.Token, true);
		}

		if (bJumpPressed)
		{
			Physics.gravity = highJumpPhysics;
		}
		else if (bIsGrounded || highJumpTimerTokenSource.IsCancellationRequested)
		{
			highJumpBufferTimer = highJumpBufferTime;
			Physics.gravity = normalJumpPhysics;
		}
	}

	private void CreateCoyoteTimerToken(ref CancellationTokenSource source) => source = new CancellationTokenSource();

	private void CancelCoyoteTimer()
	{
		if (coyoteTimerTokenSource != null)
		{
			coyoteTimerTokenSource.Cancel();
			coyoteTimerTokenSource.Dispose();
			coyoteTimerTokenSource = null;
		}
	}

	private async UniTask PerformCoyoteJump()
	{
		if (coyoteTimerTokenSource == null)
			CreateCoyoteTimerToken(ref coyoteTimerTokenSource);

		float coyoteTimer = coyoteBufferTime;

		while (!bIsGrounded && coyoteTimer > 0.0f && !coyoteTimerTokenSource.IsCancellationRequested)
		{
			coyoteTimer -= Time.deltaTime;

			if (bJumpPressed)
			{
				highJumpTimerAwaiter = PerformHighJumpTiming().GetAwaiter();
				CancelCoyoteTimer();
			}

			await UniTask.Yield(PlayerLoopTiming.Update, coyoteTimerTokenSource.Token, true);
		}
	}

	public override EMoveStatus FixedMovementUpdate()
	{
		// Grounded check
		if (Physics.BoxCast(Controller.transform.position + GroundedBoxcastOffset, GroundedBoxcastHalfsize, Vector3.down, out RaycastHit groundedHit, Quaternion.identity, GroundedBoxcastDistance, PlayerMask.value))
		{
			float dotProduct = Vector3.Dot(groundedHit.normal, Vector3.up);
			if (dotProduct >= GroundedMinWalkableAngleDot || dotProduct >= minSlopeDetectionAngle)
			{
#if UNITY_EDITOR
				if (bShowDebug)
				{
					Debug.DrawLine(Controller.transform.position + GroundedBoxcastOffset, groundedHit.point, Color.green, 1f);
					Debug.Log("Player movement grounded check success -- hit and grounded");
				}
#endif
				if (!highJumpTimerAwaiter.IsCompleted)
					CancelHighJumpTimer();
				if (!coyoteTimerAwaiter.IsCompleted)
					CancelCoyoteTimer();
				bCoyoteJumpAttempted = false;
				bDisableCoyoteJump = false;
				bIsGrounded = true;
				bIsOnSlope = dotProduct >= minSlopeDetectionAngle && dotProduct < GroundedMinWalkableAngleDot;

#if UNITY_EDITOR
				if (bShowDebug)
				{
					Debug.Log($"Is on slope: {bIsOnSlope} - {dotProduct} - {minSlopeDetectionAngle}");
				}
#endif

				movement = Vector3.ProjectOnPlane(movement, groundedHit.normal);
			}
			else
			{
#if UNITY_EDITOR
				if (bShowDebug)
				{
					Debug.DrawRay(Controller.transform.position + GroundedBoxcastOffset, Vector3.down * GroundedBoxcastDistance, Color.magenta, 1f);
					Debug.Log("Player movement grounded check fail -- hit but not grounded");
				}
#endif
				bIsGrounded = false;
				if (Controller.rigidbody.velocity.y > 0)
					bDisableCoyoteJump = true;
			}
		}
		else
		{
#if UNITY_EDITOR
			if (bShowDebug)
			{
				Debug.DrawRay(Controller.transform.position + GroundedBoxcastOffset, Vector3.down * GroundedBoxcastDistance, Color.red);
				Debug.Log("Player movement grounded check fail -- no hit and not grounded");
			}
#endif
			bIsGrounded = false;
			if (Controller.rigidbody.velocity.y > 0)
				bDisableCoyoteJump = true;
		}

		// Movement
		if (bIsOnSlope)
		{
			Vector3 slopeRight = Vector3.Cross(groundedHit.normal, Vector3.up);
			Vector3 slopeUp = Vector3.Cross(groundedHit.normal, -slopeRight);
			float playerToSlopeDot = Vector3.Dot(slopeUp, movement.normalized);
			float slopeToUpDot = Vector3.Dot(groundedHit.normal, Vector3.up);

			//TODO: "proper" slope-movement
			Vector3 slopeMovement = Vector3.zero;
			// Heading up on slope -- decrease movement by slope-steepness
			if (playerToSlopeDot > 0)
			{
				slopeMovement += slopeToUpDot * slopeUpwalkSpeedMultiplier * movement;
			}
			else // Heading down on slope -- increase movement by slope-steepness
			{
				slopeMovement += slopeToUpDot * slopeDownwalkSpeedMultiplier * movement;
			}
#if UNITY_EDITOR
			if (bShowDebug)
			{
				Debug.Log($"Player To Slope Dot: {playerToSlopeDot} - Slope To Up Dot: {slopeToUpDot} - movement: {slopeMovement}");
			}
#endif
			Controller.rigidbody.AddForce(slopeMovement, ForceMode.Force);
		}
		else
		{
			Controller.rigidbody.AddForce(movement, ForceMode.Force);
		}

		// Rotation
		Vector3 direction = Vector3.ProjectOnPlane(cameraController.transform.TransformDirection(new Vector3(movementDirection.x, 0, movementDirection.y)), Vector3.up);
		Controller.rigidbody.MoveRotation(Quaternion.Slerp(Controller.transform.rotation, Quaternion.LookRotation(direction, Vector3.up), lookRotationSpeed));

		// climb surface/point detection
#if UNITY_EDITOR
		if (bShowDebug)
			Debug.DrawRay(Controller.transform.position + MoveDirectionBoxcastOffset, capsuleCastDirection * MoveDirectionBoxcastDistance, Color.green);
#endif
		if (Physics.BoxCast(Controller.transform.position + MoveDirectionBoxcastOffset, MoveDirectionBoxcastHalfsize, capsuleCastDirection == Vector3.zero ? Controller.transform.forward : capsuleCastDirection, out RaycastHit hit, Quaternion.identity, MoveDirectionBoxcastDistance, PlayerMask.value))
		{
			float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
#if UNITY_EDITOR
			if (bShowDebug)
			{
				Debug.DrawRay(hit.point, hit.normal * 5, new Color(255, 165, 0), 5f);
				Debug.Log($"Player movement climb detection: {dotProduct}");
			}
#endif
			if (dotProduct >= GroundedMinWalkableAngleDot || dotProduct >= minSlopeDetectionAngle)
			{
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.Log("Player movement climb detection -- surface is walkable with normal angle");
#endif
				return EMoveStatus.Walkable;
			}

			//? Somehow switches state back to ZeldaClimb even if standing on a walkable surface??...
			// Fixed, turns out the issue was with doing a Box/CapsulecastAll -- All was the issue, which does make sense to a certain degree
			// -- if i keep checking the hits, multiple cases could definitely trigger
			if (hit.transform.CompareTag("ClimbSurface"))
			{
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.Log("Player movement climb detection -- surface is not walkable with normal angle -- Zelda");
#endif
				SwitchState(EPlayerMovementMode.ZeldaClimb);
			}
			else if (hit.transform.CompareTag("ClimbPoint") && hit.transform.TryGetComponent(out Climbable climbable))
			{
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.Log("Player movement climb detection -- surface is not walkable with normal angle -- Assassin");
#endif
				Controller.transform.position = climbable.WallPlayerPosition;
				Controller.transform.rotation = Quaternion.LookRotation(climbable.WallNormal);
				Physics.SyncTransforms();
				SwitchState(EPlayerMovementMode.AssassinClimb);
			}
		}
		return EMoveStatus.Walkable;
	}

	public override void Init(PlayerController controller)
	{
		Controller = controller;
		cameraController = controller.cameraController;
		highJumpPhysics = Physics.gravity * highJumpMultiplier;
		normalJumpPhysics = Physics.gravity;
	}

	public override void OnWalkJump(InputAction.CallbackContext ctx)
	{
		if (ctx.canceled)
		{
			bJumpPressed = false;
			if (!highJumpTimerAwaiter.IsCompleted)
				CancelHighJumpTimer();
			else
				Physics.gravity = normalJumpPhysics;
		}

		if (ctx.performed)
		{
			bJumpPressed = true;

			if (bIsGrounded)
			{
				highJumpTimerAwaiter = PerformHighJumpTiming().GetAwaiter();
			}

			if (!bDisableCoyoteJump && !bCoyoteJumpAttempted && !bIsGrounded && Controller.rigidbody.velocity.y < 0)
			{
				coyoteTimerAwaiter = PerformCoyoteJump().GetAwaiter();
				bCoyoteJumpAttempted = true;
			}
		}
	}

	public override void OnWalkMove(InputAction.CallbackContext ctx)
	{
		movementDirection = ctx.ReadValue<Vector2>();
	}

	public override void SwitchState(EPlayerMovementMode newState)
	{
		Controller.SwitchMoveMode(newState);
	}

	public override EMoveStatus MovementUpdate()
	{
		capsuleCastDirection = Vector3.zero;
		movement = Vector3.zero;
		movement = Vector3.ProjectOnPlane(cameraController.transform.TransformDirection(new Vector3(movementDirection.x, 0, movementDirection.y)), Vector3.up);
		if (bIsGrounded)
		{
			movement *= MoveSpeed;
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.DrawRay(Controller.transform.position, movement, Color.red);
#endif

			if (bJumpPressed)
			{
				capsuleCastDirection += Vector3.up;
			}
		}
		else
		{
			movement *= MoveSpeed * jumpMoveControlFactor;
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.DrawRay(Controller.transform.position, movement, Color.red);
#endif
		}
		capsuleCastDirection += movement;
		capsuleCastDirection += Controller.rigidbody.velocity.normalized;

		if (bIsGrounded && bJumpPressed)
		{
			capsuleCastDirection /= 3;
		}
		else
		{
			capsuleCastDirection /= 2;
		}

		capsuleCastDirection = capsuleCastDirection.normalized;
		return EMoveStatus.Walkable;
	}
}