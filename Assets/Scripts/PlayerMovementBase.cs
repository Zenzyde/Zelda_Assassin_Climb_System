using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EPlayerMovementMode
{
	None,
	Walk,
	AssassinClimb,
	ZeldaClimb
}

public enum EMoveStatus
{
	Climbable,
	PossiblyClimbable,
	NotClimbable,
	Walkable,
	PossiblyWalkable,
	NotWalkable,
	Transitioning
}

[Serializable]
[RequireComponent(typeof(PlayerController))]
public abstract class PlayerMovementBase : MonoBehaviour, IPlayerMovement
{
	[Header("Generic base movement settings")]
	[SerializeField] protected float MoveSpeed;
	[SerializeField] protected float JumpStrength;
	[SerializeField][Range(-1, 1)] protected float GroundedMinWalkableAngleDot;
	[SerializeField] protected float GroundedBoxcastDistance;
	[SerializeField] protected Vector3 GroundedBoxcastOffset;
	[SerializeField] protected Vector3 GroundedBoxcastHalfsize;
	[SerializeField] protected Vector3 MoveDirectionBoxcastOffset;
	[SerializeField] protected float MoveDirectionBoxcastDistance;
	[SerializeField] protected Vector3 MoveDirectionBoxcastHalfsize;
	[SerializeField] protected LayerMask PlayerMask;

	protected PlayerController Controller;
	protected bool bShowDebug = true;

	public virtual void Init(PlayerController controller) => Controller = controller;
	public abstract EMoveStatus MovementUpdate();
	public abstract EMoveStatus FixedMovementUpdate();
	public virtual void Enter()
	{
#if UNITY_EDITOR
		string className = GetType().Name;
		Debug.Log($"Entered {className}");
#endif
	}
	public virtual void Exit()
	{
#if UNITY_EDITOR
		string className = GetType().Name;
		Debug.Log($"Exited {className}");
#endif
	}
	public abstract void SwitchState(EPlayerMovementMode newState);
	public virtual void OnWalkMove(InputAction.CallbackContext ctx) { }
	public virtual void OnClimbMove(InputAction.CallbackContext ctx) { }
	public virtual void OnWalkJump(InputAction.CallbackContext ctx) { }
	public virtual void OnClimbJump(InputAction.CallbackContext ctx) { }
	public virtual void SetDebugState(bool state) => bShowDebug = state;
	public virtual bool GetDebugState() => bShowDebug;
}