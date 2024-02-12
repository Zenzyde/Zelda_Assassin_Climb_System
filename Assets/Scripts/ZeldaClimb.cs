using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

// Zelda climb:
// Surface-based, should be constantly checking for new surfaces and align to them, needs directional as well as forward and down raycasting

public class ZeldaClimb : PlayerClimbMovement
{
	// Zelda climb settings
	[Header("Zelda-like climbing settings")]
	[Range(-1, 1)] public float ClimbTransitionMaxAngleDot;
	public Vector3 ClimbTransitionBoxcastOffset;
	public float ClimbTransitionBoxcastDistance;
	public Vector3 ClimbTransitionBoxcastHalfsize;
	public float ClimbTransitionMinGapDistance;
	[Range(-1, 1)] public float ClimbTransitionMinSphericalNormalAngleDot;

	private Vector3 climbDirection;
	private Vector3 climbMovement;

	public override void Enter()
	{
		base.Enter();
	}

	public override void Exit()
	{
		base.Exit();
	}

	public override EMoveStatus FixedMovementUpdate()
	{
		EMoveStatus moveStatus = base.FixedMovementUpdate();
#if UNITY_EDITOR
		if (bShowDebug)
			Debug.Log($"Move Status from PlayerClimb to ZeldaClimb: {moveStatus}");
#endif

		if (IsTransitionActive())
		{
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.Log("Zelda-climb transition early exit");
#endif
			return EMoveStatus.Transitioning;
		}

		RaycastHit hit;
#if UNITY_EDITOR
		if (bShowDebug)
			ExtDebug.DrawBoxCastBox(Controller.transform.position + Controller.transform.up * ClimbTransitionBoxcastOffset.y + Controller.transform.right * ClimbTransitionBoxcastOffset.x, ClimbTransitionBoxcastHalfsize, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), boxCastDirection, ClimbTransitionBoxcastDistance, Color.yellow);
#endif
		if (moveStatus == EMoveStatus.PossiblyClimbable)
		{
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.Log("Player climb has decided zelda climb might not be allowed");
#endif
			// If bezier is not happening, it could be possible it found a blocking wall that faces towards the player -- try to bezier onto the wall
			if (Physics.BoxCast(Controller.transform.position + Controller.transform.up * ClimbTransitionBoxcastOffset.y + Controller.transform.right * ClimbTransitionBoxcastOffset.x, ClimbTransitionBoxcastHalfsize, boxCastDirection, out hit, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), ClimbTransitionBoxcastDistance, PlayerMask.value))
			{
				float dotProduct = Vector3.Dot(hit.normal, Vector3.up);

#if UNITY_EDITOR
				if (bShowDebug)
				{
					ExtDebug.DrawBoxCastBox(Controller.transform.position + Controller.transform.up * ClimbTransitionBoxcastOffset.y + Controller.transform.right * ClimbTransitionBoxcastOffset.x, ClimbTransitionBoxcastHalfsize, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), boxCastDirection, ClimbTransitionBoxcastDistance, Color.yellow, 30);
					ExtDebug.DrawBoxCastOnHit(Controller.transform.position + Controller.transform.up * ClimbTransitionBoxcastOffset.y + Controller.transform.right * ClimbTransitionBoxcastOffset.x, ClimbTransitionBoxcastHalfsize, Quaternion.LookRotation(Vector3.Lerp(Controller.transform.forward, Controller.transform.right, 0.5f), Controller.transform.up), boxCastDirection, hit.distance, Color.magenta, 30);
				}
#endif
				// Check for blocking wall
				if (hit.transform.CompareTag("ClimbSurface") && dotProduct < GroundedMinWalkableAngleDot)
				{
					// Most likely hit a blocking wall
#if UNITY_EDITOR
					if (bShowDebug)
					{
						Debug.DrawLine(Controller.transform.position + Controller.transform.up * ClimbTransitionBoxcastOffset.y + Controller.transform.right * ClimbTransitionBoxcastOffset.x, hit.point, Color.red, 1f);
						Debug.Log("Climbing directional check for walk-surface failed -- blocking wall found -- transitioning");
					}
#endif
					bezierTransitionAwaiter = PerformBezierTransition(hit.point + hit.normal, Vector3.Lerp(-Controller.transform.forward, hit.normal, 0.5f), 3, 1, true, -hit.normal)
						.ContinueWith(() => { boxCastDirection = Vector3.zero; }).GetAwaiter();
					return EMoveStatus.Climbable;
				}
			}
			return EMoveStatus.NotClimbable;
		}

		//? Move position seems to somehow be blocked by the blocking climb check below??.....
		// Calling to sync transforms seems to fix the issues, guess it becomes desynced somehow?..
		Physics.SyncTransforms();
		Controller.rigidbody.MovePosition(Controller.transform.position + climbMovement * Time.fixedDeltaTime);

		// Do multi-check casts in increments to check if climbing is possibly still possible
		// If gap is detected between player and possible climbable/transitionable surface, check the distance of the gap
		// -- if it's too small climb/bezier transition is not possible!
		// This is also part of blocking climb check after cardinal check in PlayerClimb
		// -- if Boxcast returns true there could be something blocking a climb-transition onto a surface that faces away from the player
		// -- (such as climbing onto a floor/balcony above or around the wall of a house)
		Vector3 multicastPosition = Controller.transform.position + Controller.transform.up * ClimbTransitionBoxcastOffset.y + Controller.transform.right * ClimbTransitionBoxcastOffset.x;
		bool bNonHitDetected = false;
		bool bSecondaryHitDetected = false;
		Vector3 originalNormalAngle = Vector3.zero;
		Vector3 compareNormalAngle = Vector3.zero;
		Vector3 gapOpeningPoint = Vector3.zero;
		Vector3 gapClosingPoint = Vector3.zero;
		while (Vector3.Distance(Controller.transform.position, multicastPosition) < ClimbTransitionBoxcastDistance)
		{
			if (boxCastDirection == Vector3.zero)
			{
				// Had been experiencing crashes after a bezier transition for a while now, turns out this while-loop was the culprit -- this break fixed it!
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.LogWarning("Capsule cast direction was Zero -- early while-loop exit triggered");
#endif
				break;
			}

			if (Physics.Linecast(Controller.transform.position + Controller.transform.up * ClimbTransitionBoxcastOffset.y + Controller.transform.right * ClimbTransitionBoxcastOffset.x, multicastPosition, PlayerMask.value))
			{
				// Possible abort if multicast check goes inside/past a collider in the way of the player, no point in checking past that
				// This seems to have fixed the bug that caused the player to transition into/below the floor when trying to step onto a floor from an angled climbing surface
				// -- woo!
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.Log("Zelda-climb multicast attempted check through collider -- aborting");
#endif
				break;
			}

			// Blocking climb check -- if this returns true there's something blocking a climb-transition onto a surface that faces away from the player (such as climbing onto a floor/balcony above or around the wall of a house)
			// PlayerClimbMovement checked cardinal direction is not blocked, this part checks where PlayerClimbMovement left off + in players forward direction to double-check transition
			if (Physics.BoxCast(multicastPosition, ClimbTransitionBoxcastHalfsize, Controller.transform.forward, out hit, Quaternion.LookRotation(Controller.transform.forward, Controller.transform.up), ClimbTransitionBoxcastDistance, PlayerMask.value))
			{
				if (!hit.transform.CompareTag("ClimbSurface"))
				{
					multicastPosition += boxCastDirection * 0.5f;
					continue;
				}

				if (originalNormalAngle != Vector3.zero && compareNormalAngle == Vector3.zero && hit.normal != originalNormalAngle)
				{
					compareNormalAngle = hit.normal;
#if UNITY_EDITOR
					if (bShowDebug)
						Debug.DrawRay(hit.point, hit.normal * 5, Color.red, 2);
#endif
				}

				if (originalNormalAngle == Vector3.zero)
				{
					originalNormalAngle = hit.normal;
#if UNITY_EDITOR
					if (bShowDebug)
						Debug.DrawRay(hit.point, hit.normal * 5, Color.blue, 2);
#endif
				}

				if (bNonHitDetected)
				{
					bSecondaryHitDetected = true;
					if (gapClosingPoint == Vector3.zero)
					{
						gapClosingPoint = multicastPosition;
					}
				}

#if UNITY_EDITOR
				if (bShowDebug)
					Debug.DrawRay(hit.point, hit.normal * 5, Color.black);
#endif
			}
			else
			{
				bNonHitDetected = true;
				if (gapOpeningPoint == Vector3.zero)
				{
					gapOpeningPoint = multicastPosition;
				}
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.DrawRay(multicastPosition, -Controller.transform.forward * 5, Color.white);
#endif
			}
			multicastPosition += boxCastDirection * 0.5f;
		}
#if UNITY_EDITOR
		if (bShowDebug)
			Debug.Log($"Multicast result: {bNonHitDetected} - {bSecondaryHitDetected}");
#endif
		// Gap detected between player and possible climbable/transitionable surface, if gap is too small climb/bezier transition is not possible!
		if (bNonHitDetected && bSecondaryHitDetected && (gapClosingPoint - gapOpeningPoint).sqrMagnitude < ClimbTransitionMinGapDistance * ClimbTransitionMinGapDistance)
		{
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.Log("Zelda-climb check for surface facing away from the player -- blocked!");
#endif
			return EMoveStatus.NotClimbable;
		}

		// Maxmimum normal angle check for possible blocking fail transition -- mostly meant as failsafe for spherical case
		// If the normal difference is not big enough, player can climb as usual
		// 3 cases for normal angle comparing:
		// *1: original angle not set -- the safety check above exited early, should not be a need for doing a transition in this case since we probably just exited one
		// *2: original set but not the compare -- most likely a straight wall, can keep climbing as normal
		// *3: both are set, compare normal to see if it's needed or not
		if (originalNormalAngle == Vector3.zero || originalNormalAngle != Vector3.zero && compareNormalAngle == Vector3.zero || compareNormalAngle != Vector3.zero && Vector3.Dot(originalNormalAngle, compareNormalAngle) > ClimbTransitionMinSphericalNormalAngleDot)
		{
#if UNITY_EDITOR
			if (bShowDebug)
				Debug.Log("Zelda-climb difference between normal angles for spherical surface is not enough for transition -- exiting early");
#endif
			return EMoveStatus.Climbable;
		}

		// Blocking check failed, nothing is blocking a potential climb-transition onto above mentioned surfaces
		// Check for possible climbable surface around a bend using the current opposite movement-direction to check towards the player
		if (Physics.BoxCast(Controller.transform.position + ClimbTransitionBoxcastOffset + (boxCastDirection * ClimbTransitionBoxcastDistance) + (Controller.transform.forward * 1.25f), ClimbTransitionBoxcastHalfsize, -boxCastDirection, out hit, Quaternion.LookRotation(Controller.transform.forward, Controller.transform.up), ClimbTransitionBoxcastDistance, PlayerMask.value))
		{
			if (!hit.transform.CompareTag("ClimbSurface"))
				return EMoveStatus.NotClimbable;

#if UNITY_EDITOR
			if (bShowDebug)
			{
				Debug.Log("Zelda-climb check for surface facing away from the player -- success!");
				Debug.DrawRay(hit.point, hit.normal * 5, Color.yellow, 10);
				Debug.DrawRay(Controller.transform.position, Vector3.up * 5, Color.magenta, 10);
			}
			// Do security-check for possibly walkable surface, reorient player to upwards direction and make transition in that case
#endif
			float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
			float dotProductMaxTransition = Vector3.Dot(hit.normal, -Controller.transform.forward);
			if (dotProduct >= GroundedMinWalkableAngleDot)
			{
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.Log("Zelda-climb check for surface facing away from the player -- success -- to walk");
#endif
				bezierTransitionAwaiter = PerformBezierTransition(hit.point, Vector3.Lerp(-Controller.transform.forward, hit.normal, 0.5f), 3, 2, true, Controller.transform.forward)
					.ContinueWith(() => { boxCastDirection = Vector3.zero; SwitchState(EPlayerMovementMode.Walk); }).GetAwaiter();
			}
			else if (dotProductMaxTransition < ClimbTransitionMaxAngleDot)
			{
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.Log("Zelda-climb check for surface facing away from the player -- success -- same state");
#endif
				bezierTransitionAwaiter = PerformBezierTransition(hit.point + hit.normal, Vector3.Lerp(-Controller.transform.forward, hit.normal, 0.5f), 3, 2, true, -hit.normal)
					.ContinueWith(() => { boxCastDirection = Vector3.zero; }).GetAwaiter();
			}
			else
			{
#if UNITY_EDITOR
				if (bShowDebug)
					Debug.Log($"Zelda-climb check for surface facing away from the player -- fail -- walk: {dotProduct >= GroundedMinWalkableAngleDot} - same state: {dotProductMaxTransition < ClimbTransitionMaxAngleDot}");
#endif			
			}
		}
		return EMoveStatus.Climbable;
	}

	public override void Init(PlayerController controller)
	{
		base.Init(controller);
	}

	public override void OnClimbJump(InputAction.CallbackContext ctx)
	{
		if (!ctx.performed)
			return;
		if (CanJumpOff(climbDirection))
		{
			JumpDown(climbDirection);
			return;
		}
		if (ctx.performed)
		{
			bezierTransitionAwaiter = PerformBezierTransition(Controller.transform.position + Controller.transform.up * JumpStrength, -Controller.transform.forward, 3, 0.5f)
				.ContinueWith(() => { boxCastDirection = Vector3.zero; }).GetAwaiter();
		}
	}

	public override void OnClimbMove(InputAction.CallbackContext ctx)
	{
		climbDirection = ctx.ReadValue<Vector2>();
	}

	public override void SwitchState(EPlayerMovementMode newState) { base.SwitchState(newState); }

	public override EMoveStatus MovementUpdate()
	{
		climbMovement = Controller.transform.TransformDirection(climbDirection);
#if UNITY_EDITOR
		if (bShowDebug)
			Debug.DrawRay(Controller.transform.position, climbMovement, Color.red);
#endif
		boxCastDirection += climbMovement;
		climbMovement *= MoveSpeed;

		boxCastDirection += Controller.transform.right * climbDirection.x;
		boxCastDirection += Controller.transform.up * climbDirection.y;
		boxCastDirection /= 3;
		boxCastDirection = boxCastDirection.normalized;
		return EMoveStatus.Climbable;
	}
}