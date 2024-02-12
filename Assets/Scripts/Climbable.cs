using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbable : MonoBehaviour
{
	[SerializeField] private Climbable[] nearbyWalls;
	[SerializeField] private float wallHalfRange;
	[SerializeField] private Vector3 wallPlayerOffset;

	public Climbable[] NearbyWalls { get { return nearbyWalls; } }
	public Vector3 WallNormal => transform.forward;
	public Vector3 WallPlayerPosition => transform.position + (transform.up * wallPlayerOffset.y) + (transform.right * wallPlayerOffset.x) + (transform.forward * wallPlayerOffset.z);

	public Vector3[] GetNearbyDirections(Vector3 player)
	{
		Vector3[] directions = new Vector3[nearbyWalls.Length];
		for (int i = 0; i < nearbyWalls.Length; i++)
		{
			directions[i] = (nearbyWalls[i].transform.position - player).normalized;
		}
		return directions;
	}

	public bool CanMoveOnWall(Vector3 nextPos)
	{
		Debug.DrawRay(nextPos, transform.forward * 5, Color.white);
		Vector3 LeftEdge = WallPlayerPosition - transform.right * wallHalfRange;
		Vector3 RightEdge = WallPlayerPosition + transform.right * wallHalfRange;

		Vector3 leftToNext = (nextPos - LeftEdge).normalized;
		Vector3 rightToNext = (nextPos - RightEdge).normalized;

		float boundaryCheck = Vector3.Dot(leftToNext, rightToNext);
		return boundaryCheck < 0;
	}

	void OnDrawGizmosSelected()
	{
		if (wallHalfRange > 0.0f)
		{
			Gizmos.color = Color.black;
			Gizmos.DrawLine(transform.position, WallPlayerPosition);
			Vector3 LeftEdge = WallPlayerPosition - transform.right * wallHalfRange;
			Vector3 RightEdge = WallPlayerPosition + transform.right * wallHalfRange;
			Gizmos.DrawLine(LeftEdge, RightEdge);
			Gizmos.DrawRay(LeftEdge, transform.forward * 5);
			Gizmos.DrawRay(RightEdge, transform.forward * 5);
		}

		if (nearbyWalls.Length == 0)
			return;

		foreach (Climbable climbable in nearbyWalls)
		{
			if (climbable == null)
				continue;
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.position, climbable.transform.position);
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, 1f);
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(climbable.transform.position, Vector3.one);
		}

		Gizmos.color = Color.blue;
		Gizmos.DrawRay(transform.position, WallNormal * 2);
	}
}