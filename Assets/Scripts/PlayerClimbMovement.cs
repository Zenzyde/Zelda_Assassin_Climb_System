using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class PlayerClimbMovement : PlayerMovementBase
{
	[Header("Generic player climbing facing settings")]
	[SerializeField] protected float PlayerFacingCapsulecastDistance;
	[SerializeField] protected float PlayerFacingCapsulecastRadius;
	[SerializeField] protected float PlayerFacingCapsulecastHalfSize;
	[SerializeField] protected Vector3 PlayerFacingCapsulecastOffset;

	protected CameraController cameraController;
	protected Vector3 boxCastDirection;

	protected CancellationTokenSource bezierTransitionTokenSource;
	protected UniTask.Awaiter bezierTransitionAwaiter;

	// Zelda climb:
	// Surface-based, should be constantly checking for new surfaces and align to them, needs directional as well as forward and down raycasting

	// Assassin climb:
	// Point-based, needs access to climbable points and their closest neighbours, aligns to the point

	protected void CreateBezierTransitionToken(ref CancellationTokenSource source) => source = new CancellationTokenSource();

	protected void CancelBezierTransition()
	{
		if (bezierTransitionTokenSource != null)
		{
			bezierTransitionTokenSource.Cancel();
			bezierTransitionTokenSource.Dispose();
			bezierTransitionTokenSource = null;
		}
	}

	protected virtual async UniTask PerformBezierTransition(Vector3 end, Vector3 curveNormal, float transitionDuration = 1, float curveHeight = 3, bool rotate = false, Vector3 endDirection = new Vector3(), Vector3 up = new Vector3())
	{
		if (bezierTransitionTokenSource == null)
			CreateBezierTransitionToken(ref bezierTransitionTokenSource);

		Vector3 start = Controller.transform.position;

		Vector3 middle = ((start + end) / 2.0f) + curveNormal * curveHeight;

		float t = 0;

		Quaternion playerRotation = Controller.transform.rotation;
		Quaternion endRotation = Quaternion.LookRotation(endDirection, up == Vector3.zero ? Vector3.up : up);

		while (t < transitionDuration && !bezierTransitionTokenSource.IsCancellationRequested)
		{
			float percentage = t / transitionDuration;
			Vector3 pointA = Vector3.Lerp(start, middle, percentage);
			Vector3 pointB = Vector3.Lerp(middle, end, percentage);
			Vector3 pointC = Vector3.Lerp(pointA, pointB, percentage);
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.DrawRay(pointC, curveNormal * curveHeight, Color.Lerp(Color.red, Color.green, percentage), transitionDuration);
#endif
			Controller.rigidbody.MovePosition(pointC);

			if (rotate)
			{
				Quaternion rotation = Quaternion.Slerp(playerRotation, endRotation, percentage);
				Controller.rigidbody.MoveRotation(rotation);
			}

			t += Time.unscaledDeltaTime;
			await UniTask.Yield(PlayerLoopTiming.Update, bezierTransitionTokenSource.Token, true);
		}
	}

	protected bool CanJumpOff(Vector3 movementDirection) => movementDirection.y < -0.5f;

	protected virtual void JumpDown(Vector3 movementDirection)
	{
		movementDirection = new Vector3(movementDirection.x, 0, movementDirection.y);
		Vector3 movement = Controller.transform.TransformDirection(movementDirection);
		movement += Vector3.ProjectOnPlane(cameraController.transform.TransformDirection(movementDirection), Vector3.up);
		movement /= 2;
		movement = movement.normalized;

		bezierTransitionAwaiter = PerformBezierTransition(Controller.transform.position + Controller.transform.up + (movement * 2), Vector3.up, 0.5f, 0.1f)
			.ContinueWith(() => { SwitchState(EPlayerMovementMode.Walk); Controller.rigidbody.AddForce(-Controller.transform.forward, ForceMode.Impulse); }).GetAwaiter();
	}

	protected bool IsTransitionActive() => !bezierTransitionAwaiter.IsCompleted;

	public override void SwitchState(EPlayerMovementMode newState)
	{
		Controller.SwitchMoveMode(newState);
	}

	public override void Enter()
	{
		base.Enter();
		Controller.rigidbody.useGravity = false;
		Controller.rigidbody.isKinematic = true;
		Controller.rigidbody.velocity = Vector3.zero;
		Controller.rigidbody.angularVelocity = Vector3.zero;
		Controller.rigidbody.constraints = RigidbodyConstraints.FreezeRotationY ^ RigidbodyConstraints.FreezeRotationZ;
		cameraController.SetCameraRotationState(true);
	}

	public override void Exit()
	{
		base.Exit();
		Controller.rigidbody.velocity = Vector3.zero;
		Controller.rigidbody.angularVelocity = Vector3.zero;
		CancelBezierTransition();
	}

	public override EMoveStatus FixedMovementUpdate()
	{
		if (IsTransitionActive())
		{
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.Log($"Bezier early exit: {!bezierTransitionAwaiter.IsCompleted}");
#endif
			return EMoveStatus.Transitioning;
		}

		// Player facing wall
		Vector3 playerFacingCapsuleBottom = Controller.transform.position + (Controller.transform.up * PlayerFacingCapsulecastOffset.y) + (Controller.transform.right * PlayerFacingCapsulecastOffset.x);
		Vector3 playerFacingCapsuleTop = playerFacingCapsuleBottom + Controller.transform.up * (PlayerFacingCapsulecastHalfSize * 2);
		bool bottomHit = Physics.SphereCast(playerFacingCapsuleBottom, PlayerFacingCapsulecastRadius, Controller.transform.forward, out RaycastHit hitBottom, PlayerFacingCapsulecastDistance, PlayerMask.value);
		bool topHit = Physics.SphereCast(playerFacingCapsuleTop, PlayerFacingCapsulecastRadius, Controller.transform.forward, out RaycastHit hitTop, PlayerFacingCapsulecastDistance, PlayerMask.value);
		bottomHit = bottomHit && !hitBottom.transform.CompareTag("Floor") && Vector3.Dot(hitBottom.normal, Vector3.up) < GroundedMinWalkableAngleDot;
		topHit = topHit && !hitTop.transform.CompareTag("Floor") && Vector3.Dot(hitTop.normal, Vector3.up) < GroundedMinWalkableAngleDot;
		if (bottomHit && topHit)
		{
			Vector3 averageNormal = hitBottom.normal + hitTop.normal / 2.0f;
			Vector3 averagePoint = Vector3.Lerp(hitBottom.point, hitTop.point, 0.5f);
#if UNITY_EDITOR
			if (bShowDebug)
			{
				Debug.Log("Correcting player facing");
				Debug.DrawRay(averagePoint, averageNormal * 5, Color.yellow, 10);
			}
#endif
			Controller.transform.rotation = Quaternion.LookRotation(-averageNormal);
		}
		else if (bottomHit && !topHit)
		{
#if UNITY_EDITOR
			if (bShowDebug)
			{
				Debug.Log("Correcting player facing");
				Debug.DrawRay(hitBottom.point, hitBottom.normal * 5, Color.yellow, 10);
			}
#endif
			Controller.transform.rotation = Quaternion.LookRotation(-hitBottom.normal);
		}
		else if (topHit && !bottomHit)
		{
#if UNITY_EDITOR
			if (bShowDebug)
			{
				Debug.Log("Correcting player facing");
				Debug.DrawRay(hitTop.point, hitTop.normal * 5, Color.yellow, 10);
			}
#endif
			Controller.transform.rotation = Quaternion.LookRotation(-hitTop.normal);
		}

		RaycastHit hit;
		// Grounded check
		if (Vector3.Dot(boxCastDirection, -Controller.transform.up) >= 0.5f)
		{
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.Log("Climbing floor check - active");
#endif
			Vector3 offset = Controller.transform.up * GroundedBoxcastOffset.y + Controller.transform.right * GroundedBoxcastOffset.x;
			if (Physics.BoxCast(Controller.transform.position + offset, GroundedBoxcastHalfsize, -Controller.transform.up, out hit, Quaternion.LookRotation(Controller.transform.forward, Controller.transform.up), GroundedBoxcastDistance, PlayerMask.value))
			{
#if UNITY_EDITOR
				if (bShowDebug)
				{
					Debug.Log("Climbing floor check - verified");
					Debug.DrawRay(hit.point, hit.normal * 5, Color.white, 10);
				}
#endif
				float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
				if (hit.transform.CompareTag("Floor") && dotProduct >= GroundedMinWalkableAngleDot)
				{
#if UNITY_EDITOR
					if (bShowDebug)
					{
						Debug.DrawLine(Controller.transform.position + GroundedBoxcastOffset, hit.point, Color.green, 1f);
						Debug.Log("Climbing floor check - success");
					}
#endif
					boxCastDirection = Vector3.zero;
					SwitchState(EPlayerMovementMode.Walk);
					return EMoveStatus.Walkable;
				}
			}
		}

		// Climbing direction check -- cardinal directions: up, right, down, left
		// Also checks for any immediate blocking in the cardinal directions
#if UNITY_EDITOR
		if (bShowDebug)
		{
			Debug.DrawRay(Controller.transform.position + Controller.transform.up * MoveDirectionBoxcastOffset.y + Controller.transform.right * MoveDirectionBoxcastOffset.x, boxCastDirection * MoveDirectionBoxcastDistance, Color.green);
			ExtDebug.DrawBoxCastBox(Controller.transform.position + Controller.transform.up * MoveDirectionBoxcastOffset.y + Controller.transform.right * MoveDirectionBoxcastOffset.x, MoveDirectionBoxcastHalfsize, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), boxCastDirection, MoveDirectionBoxcastDistance, Color.yellow);
		}
#endif
		if (Physics.BoxCast(Controller.transform.position + Controller.transform.up * MoveDirectionBoxcastOffset.y + Controller.transform.right * MoveDirectionBoxcastOffset.x, MoveDirectionBoxcastHalfsize, boxCastDirection, out hit, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), MoveDirectionBoxcastDistance, PlayerMask.value))
		{
#if UNITY_EDITOR
			if (bShowDebug)
			{
				ExtDebug.DrawBoxCastBox(Controller.transform.position + Controller.transform.up * MoveDirectionBoxcastOffset.y + Controller.transform.right * MoveDirectionBoxcastOffset.x, MoveDirectionBoxcastHalfsize, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), boxCastDirection, MoveDirectionBoxcastDistance, Color.yellow, 30);
				ExtDebug.DrawBoxCastOnHit(Controller.transform.position + Controller.transform.up * MoveDirectionBoxcastOffset.y + Controller.transform.right * MoveDirectionBoxcastOffset.x, MoveDirectionBoxcastHalfsize, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), boxCastDirection, hit.distance, Color.magenta, 30);
				Debug.Log($"Climbing directional check -- hit: dot - {Vector3.Dot(hit.normal, Vector3.up)}, tag - {hit.transform.tag}");
			}
#endif
			// Hit something that can still be considered walkable based on normal angle
			if (!hit.transform.CompareTag("Floor"))
			{
				float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
				if (dotProduct >= GroundedMinWalkableAngleDot)
				{
#if UNITY_EDITOR
					if (bShowDebug)
					{
						Debug.DrawRay(hit.point, hit.normal * 5, Color.green, 1f);
						Debug.Log("Climbing directional check for walk-surface success");
					}
#endif
					boxCastDirection = Vector3.zero;
					SwitchState(EPlayerMovementMode.Walk);
					return EMoveStatus.PossiblyWalkable;
				}
				// Most likely hit a blocking wall
				else
				{
#if UNITY_EDITOR
					if (bShowDebug)
					{
						Debug.DrawRay(hit.point, hit.normal * 5, Color.red, 1f);
						Debug.Log("Climbing directional check for walk-surface failed -- blocking wall found");
					}
#endif
					// Hit a Zelda-climbable surface
					if (hit.transform.CompareTag("ClimbSurface") && Controller.CurrentMoveMode == EPlayerMovementMode.AssassinClimb)
					{
#if UNITY_EDITOR
						if (bShowDebug)
							Debug.Log("Climbing directional check for walk-surface failed -- blocking wall found -- preparing transition from Zelda- to Assassin-climb");
#endif
						// Perform bezier-switch
						bezierTransitionAwaiter = PerformBezierTransition(hit.point + hit.normal, Vector3.Lerp(-Controller.transform.forward, hit.normal, 0.5f), 3, 2, true, Controller.transform.forward)
							.ContinueWith(() => { boxCastDirection = Vector3.zero; SwitchState(EPlayerMovementMode.ZeldaClimb); }).GetAwaiter();
						return EMoveStatus.Transitioning;
					}
					// Hit an Assassin-climbable surface
					else if (hit.transform.CompareTag("ClimbPoint") && Controller.CurrentMoveMode == EPlayerMovementMode.ZeldaClimb && hit.transform.TryGetComponent(out Climbable climbable))
					{
#if UNITY_EDITOR
						if (bShowDebug)
							Debug.Log("Climbing directional check for walk-surface failed -- blocking wall found -- preparing transition from Assassin- to Zelda-climb");
#endif
						// Perform bezier-switch
						bezierTransitionAwaiter = PerformBezierTransition(climbable.WallPlayerPosition, Vector3.Lerp(-Controller.transform.forward, climbable.WallNormal, 0.5f), 3, 2, true, Controller.transform.forward)
							.ContinueWith(() => { boxCastDirection = Vector3.zero; SwitchState(EPlayerMovementMode.AssassinClimb); }).GetAwaiter();
						return EMoveStatus.Transitioning;
					}
					return EMoveStatus.PossiblyClimbable;
				}
			}
			else
			{
				return EMoveStatus.Walkable;
			}
		}
		return EMoveStatus.Climbable;
	}

	public override void Init(PlayerController controller)
	{
		Controller = controller;
		cameraController = controller.cameraController;
	}
}