using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperMethods
{
	//ClampAngle & WrapAngle courtesy of Parziphal: https://stackoverflow.com/questions/25818897/problems-limiting-object-rotation-with-mathf-clamp
	public static float ClampAngle(float currentValue, float minAngle, float maxAngle, float clampAroundAngle = 0)
	{
		return Mathf.Clamp(WrapAngle(currentValue - (clampAroundAngle + 180)) - 180, minAngle, maxAngle) + 360 + clampAroundAngle;
	}

	public static float WrapAngle(float angle)
	{
		while (angle < 0)
		{
			angle += 360;
		}
		return Mathf.Repeat(angle, 360);
	}

	public static void DrawBoxBounds(Vector3 min, Vector3 max)
	{
		Gizmos.DrawLine(min, new Vector3(min.x, min.y, max.z));
		Gizmos.DrawLine(min, new Vector3(min.x, max.y, min.z));
		Gizmos.DrawLine(min, new Vector3(max.x, min.y, min.z));

		Gizmos.DrawLine(max, new Vector3(min.x, max.y, max.z));
		Gizmos.DrawLine(max, new Vector3(max.x, min.y, max.z));
		Gizmos.DrawLine(max, new Vector3(max.x, max.y, min.z));

		Gizmos.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z));
		Gizmos.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z));
		Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z));
		Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(max.x, min.y, min.z));

		Gizmos.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z));
		Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z));
	}
}

// Reference: https://discussions.unity.com/t/how-can-you-visualize-a-boxcast-boxcheck-etc/161101/2
public static class ExtDebug
{
	//Draws just the box at where it is currently hitting.
	public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color, float time = 0)
	{
		origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
		DrawBox(origin, halfExtents, orientation, color, time);
	}

	//Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
	public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color, float time = 0)
	{
		direction.Normalize();
		Box bottomBox = new Box(origin, halfExtents, orientation);
		Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

		Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color, time);
		Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color, time);
		Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color, time);
		Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color, time);
		Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color, time);
		Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color, time);
		Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color, time);
		Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color, time);

		DrawBox(bottomBox, color, time);
		DrawBox(topBox, color, time);
	}

	public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color, float time = 0)
	{
		DrawBox(new Box(origin, halfExtents, orientation), color, time);
	}
	public static void DrawBox(Box box, Color color, float time = 0)
	{
		Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color, time);
		Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color, time);
		Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color, time);
		Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color, time);

		Debug.DrawLine(box.backTopLeft, box.backTopRight, color, time);
		Debug.DrawLine(box.backTopRight, box.backBottomRight, color, time);
		Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color, time);
		Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color, time);

		Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color, time);
		Debug.DrawLine(box.frontTopRight, box.backTopRight, color, time);
		Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color, time);
		Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color, time);
	}

	public struct Box
	{
		public Vector3 localFrontTopLeft { get; private set; }
		public Vector3 localFrontTopRight { get; private set; }
		public Vector3 localFrontBottomLeft { get; private set; }
		public Vector3 localFrontBottomRight { get; private set; }
		public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
		public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
		public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
		public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

		public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
		public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
		public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
		public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
		public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
		public Vector3 backTopRight { get { return localBackTopRight + origin; } }
		public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
		public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

		public Vector3 origin { get; private set; }

		public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
		{
			Rotate(orientation);
		}
		public Box(Vector3 origin, Vector3 halfExtents)
		{
			this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
			this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
			this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
			this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

			this.origin = origin;
		}

		public void Rotate(Quaternion orientation)
		{
			localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
			localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
			localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
			localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
		}
	}

	//This should work for all cast types
	static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
	{
		return origin + (direction.normalized * hitInfoDistance);
	}

	static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
	{
		Vector3 direction = point - pivot;
		return pivot + rotation * direction;
	}
}