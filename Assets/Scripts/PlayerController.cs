using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
	[SerializeField] EPlayerMovementMode CurrentMovementMode = EPlayerMovementMode.None;

	[SerializeField] PlayerMovementBase AssassinsCreedClimbMode;
	[SerializeField] PlayerMovementBase ZeldaClimbMode;
	[SerializeField] PlayerMovementBase RegularWalkMode;

	IPlayerMovement PlayerMovementMode = null;

	public Rigidbody rigidbody { get; private set; }
	public CameraController cameraController { get; private set; }
	public EPlayerMovementMode CurrentMoveMode => CurrentMovementMode;

	private PlayerInput playerInput;

	private bool bShowDebug = false;

	// Start is called before the first frame update
	void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		cameraController = FindObjectOfType<CameraController>();
		playerInput = GetComponent<PlayerInput>();

		AssassinsCreedClimbMode?.Init(this);
		ZeldaClimbMode?.Init(this);
		RegularWalkMode?.Init(this);

		SwitchMoveMode(EPlayerMovementMode.Walk);
	}

	// Update is called once per frame
	void Update()
	{
		PlayerMovementMode?.SetDebugState(bShowDebug);
		PlayerMovementMode?.MovementUpdate();
	}

	void FixedUpdate()
	{
		PlayerMovementMode?.FixedMovementUpdate();
	}

	void OnCollisionEnter(Collision other)
	{
		PlayerMovementMode?.CollisionEnter(other);
	}

	void OnCollisionStay(Collision other)
	{
		PlayerMovementMode?.CollisionStay(other);
	}

	void OnCollisionExit(Collision other)
	{
		PlayerMovementMode?.CollisionExit(other);
	}

	void OnTriggerEnter(Collider other)
	{
		PlayerMovementMode?.TriggerEnter(other);
	}

	void OnTriggerStay(Collider other)
	{
		PlayerMovementMode?.TriggerStay(other);
	}

	void OnTriggerExit(Collider other)
	{
		PlayerMovementMode?.TriggerExit(other);
	}

	public void OnSwitchDebugMode(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			bShowDebug = !bShowDebug;
			PlayerMovementMode?.SetDebugState(bShowDebug);
		}
	}

	public void SwitchMoveMode(EPlayerMovementMode NewMode)
	{
		if (CurrentMovementMode == NewMode)
		{
#if UNITY_EDITOR
			Debug.Log($"Attempted switch from {CurrentMovementMode} to {NewMode}, aborting");
#endif
			return;
		}

#if UNITY_EDITOR
		Debug.Log($"Switching to: {NewMode} from {CurrentMovementMode}");
#endif

		CurrentMovementMode = NewMode;

		PlayerMovementMode?.Exit();

		switch (NewMode)
		{
		case EPlayerMovementMode.Walk:
			PlayerMovementMode = RegularWalkMode;
			playerInput.SwitchCurrentActionMap("Walking");
			break;
		case EPlayerMovementMode.AssassinClimb:
			PlayerMovementMode = AssassinsCreedClimbMode;
			playerInput.SwitchCurrentActionMap("Climbing");
			break;
		case EPlayerMovementMode.ZeldaClimb:
			PlayerMovementMode = ZeldaClimbMode;
			playerInput.SwitchCurrentActionMap("Climbing");
			break;
		default:
			PlayerMovementMode = null;
			break;
		}

		PlayerMovementMode?.Enter();
	}

	public void OnWalkMove(InputAction.CallbackContext ctx)
	{
		if (CurrentMovementMode == EPlayerMovementMode.None || CurrentMovementMode != EPlayerMovementMode.Walk)
			return;

		PlayerMovementMode?.OnWalkMove(ctx);
	}
	public void OnClimbMove(InputAction.CallbackContext ctx)
	{
		if (CurrentMovementMode == EPlayerMovementMode.None || CurrentMovementMode == EPlayerMovementMode.Walk)
			return;

		PlayerMovementMode?.OnClimbMove(ctx);
	}
	public void OnWalkJump(InputAction.CallbackContext ctx)
	{
		if (CurrentMovementMode == EPlayerMovementMode.None || CurrentMovementMode != EPlayerMovementMode.Walk)
			return;

		PlayerMovementMode?.OnWalkJump(ctx);
	}
	public void OnClimbJump(InputAction.CallbackContext ctx)
	{
		if (CurrentMovementMode == EPlayerMovementMode.None || CurrentMovementMode == EPlayerMovementMode.Walk)
			return;

		PlayerMovementMode?.OnClimbJump(ctx);
	}
}