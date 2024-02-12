using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
	[SerializeField] float MouseSensitivity = 1;
	[SerializeField] Vector3 CameraOffset;
	[SerializeField] float CameraMinAngle = -25;
	[SerializeField] float CameraMaxAngle = 45;
	[SerializeField] Vector3 CameraLookOffset;
	[SerializeField] Transform CameraLookAtTarget;
	[SerializeField] bool ThirdPersonCam = true;
	[SerializeField] bool Lag = false;
	[SerializeField] float CameraSmoothTime = 0.25f;

	Vector3 rotation;
	GameObject cameraPivot;
	bool bIsGroundedCam;

	public delegate void OnCameraRotateEvent(Vector3 rotation);
	public event OnCameraRotateEvent OnCameraRotate;

	private Vector3 cameraSmoothVelocity;

	// Start is called before the first frame update
	void Start()
	{
		cameraPivot = new GameObject("CameraPivot");
		rotation = cameraPivot.transform.eulerAngles;
		transform.SetParent(cameraPivot.transform);
		ResetCameraLookDirection();
		RecalculateLookAt();
	}

	void LateUpdate()
	{
		Vector3 cameraPivotRotation = cameraPivot.transform.localEulerAngles;
		cameraPivotRotation.z = 0;
		cameraPivot.transform.localRotation = Quaternion.Euler(cameraPivotRotation);

		Vector3 cameraRotation = transform.localEulerAngles;
		cameraRotation.z = 0;
		transform.localRotation = Quaternion.Euler(cameraRotation);
	}

	void Update()
	{
		OnCameraRotate?.Invoke(transform.forward);
	}

	void FixedUpdate()
	{
		if (!Lag)
		{
			cameraPivot.transform.position = CameraLookAtTarget.position;
		}
		else
		{
			cameraPivot.transform.position = Vector3.SmoothDamp(cameraPivot.transform.position, CameraLookAtTarget.position, ref cameraSmoothVelocity, CameraSmoothTime);
		}
	}

	public void OnMouseLook(InputAction.CallbackContext ctx)
	{
		Vector2 lookDelta = ctx.ReadValue<Vector2>() * MouseSensitivity * 0.01f;
		rotation.z = 0;
		rotation.x += lookDelta.y;
		rotation.x *= ThirdPersonCam ? 1 : -1;
		rotation.x = HelperMethods.ClampAngle(rotation.x, CameraMinAngle, CameraMaxAngle);
		rotation.y += lookDelta.x;
		cameraPivot.transform.rotation = Quaternion.Euler(new Vector3(-rotation.x, rotation.y, 0));
	}

	public Vector3 GetForward(bool flat = false)
	{
		if (flat)
			return Vector3.ProjectOnPlane(transform.forward, CameraLookAtTarget.up);
		return transform.forward;
	}
	public Vector3 GetRight() => transform.right;
	public Vector3 GetUp() => transform.up;
	public void RecalculateLookAt() => transform.LookAt(CameraLookAtTarget.transform.position + CameraLookOffset, CameraLookAtTarget.up);
	public void SetCameraRotationState(bool isOnWall) => bIsGroundedCam = !isOnWall;
	public void ResetCameraLookDirection()
	{
		cameraPivot.transform.rotation = Quaternion.identity;

		Vector3 camOffset = cameraPivot.transform.InverseTransformVector(CameraOffset);
		transform.localPosition = cameraPivot.transform.localPosition + camOffset;
	}

	void OnDrawGizmos()
	{
		if (!Application.isPlaying)
			return;
	}
}