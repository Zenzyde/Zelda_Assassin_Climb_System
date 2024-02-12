using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AssassinsCreedClimb : PlayerClimbMovement
{
	// Assassin climb:
	// Point-based, needs access to climbable points and their closest neighbours, aligns to the point

	// Assassin climb settings
	[Header("Assassin's Creed-like climbing settings")]
	[Range(-1, 1)] public float ClimbTransitionMaxAngleDot;
	public float DetectClimbPointRadius;

	Climbable currentAttachedClimbPoint;
	Climbable nextClimbPoint;

	private Vector2 climbDirection;

	public override void Enter()
	{
		base.Enter();

		Collider[] colliders = new Collider[] { };
		Physics.OverlapSphereNonAlloc(Controller.transform.position, DetectClimbPointRadius, colliders, PlayerMask.value);
		List<Climbable> climbables = new();

		foreach (Collider collider in colliders)
		{
			if (collider.TryGetComponent(out Climbable climbable))
				climbables.Add(climbable);
		}

		float minDistance = Mathf.Infinity;

		for (int i = 0; i < climbables.Count; i++)
		{
			Climbable climbable = climbables[i];
			float currentDistance = Vector3.Distance(climbable.transform.position, Controller.transform.position);
			if (currentDistance < minDistance)
			{
				minDistance = currentDistance;
				currentAttachedClimbPoint = climbable;
			}
		}

		Controller.transform.rotation = Quaternion.LookRotation(-currentAttachedClimbPoint.WallNormal);
	}

	public override void Exit()
	{
		base.Exit();
	}

	public override void Init(PlayerController controller)
	{
		base.Init(controller);
	}

	public override void OnClimbJump(InputAction.CallbackContext ctx)
	{
		if (!nextClimbPoint)
		{
			JumpDown(climbDirection);
			return;
		}
		if (!ctx.performed)
			return;
		if (IsTransitionActive())
			return;

		currentAttachedClimbPoint = nextClimbPoint;
		// Handle position and directional transition of player and camera
		bezierTransitionAwaiter = PerformBezierTransition(currentAttachedClimbPoint.WallPlayerPosition, currentAttachedClimbPoint.WallNormal, 3, 1).GetAwaiter();
		Controller.transform.rotation = Quaternion.LookRotation(-currentAttachedClimbPoint.WallNormal);
	}

	public override void OnClimbMove(InputAction.CallbackContext ctx)
	{
		climbDirection = ctx.ReadValue<Vector2>();
	}

	public override void SwitchState(EPlayerMovementMode newState) { base.SwitchState(newState); }

	public override EMoveStatus FixedMovementUpdate()
	{
		EMoveStatus status = base.FixedMovementUpdate();
		return status;
	}

	public override EMoveStatus MovementUpdate()
	{
		if (IsTransitionActive())
			return EMoveStatus.Transitioning;

		Vector3 climbMovement = Controller.transform.TransformDirection(climbDirection);
#if UNITY_EDITOR
		if (bShowDebug)
			Debug.DrawRay(Controller.transform.position, climbMovement, Color.red);
#endif
		boxCastDirection += climbMovement;
		boxCastDirection += Controller.transform.right * climbDirection.x;
		boxCastDirection += Controller.transform.up * climbDirection.y;
		boxCastDirection /= 3;
		boxCastDirection = boxCastDirection.normalized;

		Vector3 movement = Controller.transform.up * climbDirection.y + Controller.transform.right * climbDirection.x;
		Vector3 cameraInfluenceVector = Vector3.ProjectOnPlane(cameraController.transform.TransformDirection(climbDirection), Vector3.up);
		movement += new Vector3(cameraInfluenceVector.x, cameraInfluenceVector.y, 0);
		movement /= 2;
		movement = movement.normalized;

		Vector3 forwardProjectedMovement = Vector3.ProjectOnPlane(movement, currentAttachedClimbPoint.transform.forward);
		Vector3 upProjectedMovement = Vector3.ProjectOnPlane(forwardProjectedMovement, currentAttachedClimbPoint.transform.up);
		Vector3 nextMovement = Controller.transform.position + upProjectedMovement * Time.fixedDeltaTime;
		if (currentAttachedClimbPoint.CanMoveOnWall(nextMovement))
		{
			Controller.rigidbody.MovePosition(nextMovement);
		}

		nextClimbPoint = null;
		Climbable[] climbableConnections = currentAttachedClimbPoint.NearbyWalls;

		float closestDirection = 0.15f;
		foreach (Climbable climbable in climbableConnections)
		{
			Vector3 playerToClimbable = (climbable.transform.position - Controller.transform.position).normalized;
			float currentDirection = Vector3.Dot(movement, playerToClimbable);
			if (currentDirection > closestDirection)
			{
				closestDirection = currentDirection;
				nextClimbPoint = climbable;
#if UNITY_EDITOR
				if (bShowDebug)
				{
					Debug.DrawRay(Controller.transform.position, playerToClimbable, Color.Lerp(Color.red, Color.green, currentDirection));
					Debug.DrawRay(Controller.transform.position, movement, Color.blue);
				}
#endif
			}
		}

		return EMoveStatus.Climbable;
	}
}