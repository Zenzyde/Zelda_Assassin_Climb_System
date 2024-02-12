using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerMovement
{
	public abstract void Init(PlayerController controller);
	public abstract EMoveStatus MovementUpdate();
	public abstract EMoveStatus FixedMovementUpdate();
	public abstract void Enter();
	public abstract void Exit();
	public abstract void SwitchState(EPlayerMovementMode newState);
	public abstract void OnWalkMove(InputAction.CallbackContext ctx);
	public abstract void OnClimbMove(InputAction.CallbackContext ctx);
	public abstract void OnWalkJump(InputAction.CallbackContext ctx);
	public abstract void OnClimbJump(InputAction.CallbackContext ctx);
	public abstract void SetDebugState(bool state);
	public abstract bool GetDebugState();
	public virtual void UpdatePlayerRotation(Vector3 rotation) { }
	public virtual void CollisionEnter(Collision collision) { }
	public virtual void CollisionStay(Collision collision) { }
	public virtual void CollisionExit(Collision collision) { }
	public virtual void TriggerEnter(Collider collider) { }
	public virtual void TriggerStay(Collider collider) { }
	public virtual void TriggerExit(Collider collider) { }
	public virtual void OnDrawGizmos() { }
}