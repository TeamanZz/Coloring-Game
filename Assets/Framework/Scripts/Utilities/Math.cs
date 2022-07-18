using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public static class Math
	{
		/// <summary>
		/// Returns true if the triangle define using triPoint1/2/3 contains the given point
		/// </summary>
		public static bool TriangleContainsPoint(Vector2 point, Vector2 triPoint1, Vector2 triPoint2, Vector2 triPoint3)
		{
			float rectMinX = Mathf.Min(triPoint1.x, triPoint2.x, triPoint3.x);
			float rectMinY = Mathf.Min(triPoint1.y, triPoint2.y, triPoint3.y);
			float rectMaxX = Mathf.Max(triPoint1.x, triPoint2.x, triPoint3.x);
			float rectMaxY = Mathf.Max(triPoint1.y, triPoint2.y, triPoint3.y);

			// First check if the point is inside the bounding box of the triangle
			if (!RectangleContainsPoint(point, rectMinX, rectMinY, rectMaxX, rectMaxY))
			{
				return false;
			}

			// Now check if the point is within the triangle
			float d1 = TriangleContainsPointHelper(point, triPoint1, triPoint2);
			float d2 = TriangleContainsPointHelper(point, triPoint2, triPoint3);
			float d3 = TriangleContainsPointHelper(point, triPoint3, triPoint1);

			bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			bool pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(neg && pos);
		}
		
		/// <summary>
		/// Helper method
		/// </summary>
		private static float TriangleContainsPointHelper(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		/// <summary>
		/// Returns true if the rectangle defined using rectMin/Max contains the given point
		/// </summary>
		public static bool RectangleContainsPoint(Vector2 point, Rect rect)
		{
			return point.x >= rect.xMin && point.y >= rect.yMin && point.x <= rect.xMax && point.y <= rect.yMax;
		}

		/// <summary>
		/// Returns true if the rectangle defined using rectMin/Max contains the given point
		/// </summary>
		public static bool RectangleContainsPoint(Vector2 point, Vector2 rectMin, Vector2 rectMax)
		{
			return point.x >= rectMin.x && point.y >= rectMin.y && point.x <= rectMax.x && point.y <= rectMax.y;
		}

		/// <summary>
		/// Returns true if the rectangle defined using rectMin/Max contains the given point
		/// </summary>
		public static bool RectangleContainsPoint(Vector2 point, float rectMinX, float rectMinY, float rectMaxX, float rectMaxY)
		{
			return point.x >= rectMinX && point.y >= rectMinY && point.x <= rectMaxX && point.y <= rectMaxY;
		}
	}
}
