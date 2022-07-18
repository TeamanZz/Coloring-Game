using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	public class UILine : Graphic
	{
		#region Inspector Variables

		[SerializeField] private float	thickness;
		[SerializeField] private int	lineRoundness;

		#endregion

		#region Member Variables

		private List<Vector2> linePoints;

		#endregion

		#region Properties

		public List<Vector2> LinePoints
		{
			get
			{
				if (linePoints == null)
				{
					linePoints = new List<Vector2>();
				}

				return linePoints;
			}
		}

		#endregion

		#region Unity Methods

		#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			// Thickness cannot be less than 0
			if (thickness < 0)
			{
				thickness = 0;
			}

			// Roundness cannot be less than 0
			if (lineRoundness < 0)
			{
				lineRoundness = 0;
			}
		}
		#endif

		#endregion

		#region Public Methods

		public void SetPoints(List<Vector2> newPoints)
		{
			LinePoints.Clear();

			// Remove any points that are adjacent and the same
			for (int i = 0; i < newPoints.Count; i++)
			{
				if (i > 0 && newPoints[i].x == newPoints[i - 1].x && newPoints[i].y == newPoints[i - 1].y)
				{
					continue;
				}

				LinePoints.Add(newPoints[i]);
			}

			SetVerticesDirty();
		}

		public void LinePointsUpdated()
		{
			SetVerticesDirty();
		}

		#endregion

		#region Protected Methods

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (LinePoints.Count > 1)
			{
				// Populate the line start half circle
				PopulateCircle(vh, LinePoints[0], LinePoints[0] - LinePoints[1], 180f);

				Vector2 prevLineDir	= Vector2.zero;
				Vector2 prevPoint2	= Vector2.zero;
				Vector2 prevPoint3	= Vector2.zero;

				// Populate the line segements
				for (int i = 1; i < LinePoints.Count; i++)
				{
					Vector2 point		= LinePoints[i];
					Vector2 prevPoint	= LinePoints[i - 1];

					if (point.x == prevPoint.x && point.y == prevPoint.y)
					{
						continue;
					}

					// Get the start and end of this line segement and get the direction
					// the line is pointing and the perpendicular vector to that direction
					Vector2 lineStart	= prevPoint;
					Vector2 lineEnd		= point;
					Vector2 lineDir		= (lineEnd - lineStart).normalized;
					Vector2 lineDirPerp	= lineDir.Rotate(-90).normalized;

					// Get the 4 corner points for list line segment
					Vector2 point1 = lineStart - lineDirPerp * thickness;
					Vector2 point2 = lineEnd - lineDirPerp * thickness;
					Vector2 point3 = lineEnd + lineDirPerp * thickness;
					Vector2 point4 = lineStart + lineDirPerp * thickness;

					int index = vh.currentVertCount;

					vh.AddVert(point1, color, Vector2.zero);
					vh.AddVert(point2, color, Vector2.zero);
					vh.AddVert(point3, color, Vector2.zero);
					vh.AddVert(point4, color, Vector2.zero);

					// Add the 2 trigangles that make up the line segment
					vh.AddTriangle(index, index + 1, index + 2);
					vh.AddTriangle(index + 2, index + 3, index);

					// Fill the little "gap" that is created where the line segments meet
					if (i > 1)
					{
						index = vh.currentVertCount;

						float x1	= prevLineDir.x;
						float y1	= prevLineDir.y;
						float x2	= lineDir.x;
						float y2	= lineDir.y;
						float angle	= Mathf.Atan2(x1 * y2 - y1 * x2, x1 * x2 + y1 * y2);

						if (angle > 0)
						{
							FillGap(vh, point4, prevPoint3, lineStart, angle * Mathf.Rad2Deg);
						}
						else if (angle < 0)
						{
							FillGap(vh, prevPoint2, point1, lineStart, angle * Mathf.Rad2Deg);
						}
					}

					prevLineDir	= lineDir;
					prevPoint2	= point2;
					prevPoint3	= point3;
				}

				// Populate the line end half circle
				PopulateCircle(vh, LinePoints[LinePoints.Count - 1], LinePoints[LinePoints.Count - 1] - LinePoints[LinePoints.Count - 2], 180f);
			}
		}

		#endregion

		#region Private Methods

		private void FillGap(VertexHelper vh, Vector2 point1, Vector2 point2, Vector2 pivot, float angle)
		{
			Vector2 middlePoint = point2 + (point1 - point2) / 2f;
			Vector2 dir			= middlePoint - pivot;

			PopulateCircle(vh, pivot, dir, angle);
		}

		private void PopulateCircle(VertexHelper vh, Vector2 center, Vector2 direction, float angle)
		{
			if (lineRoundness == 0)
			{
				return;
			}

			float	angleStep		= angle / (float)lineRoundness;
			Vector2 startAngleDir	= direction.Rotate(angle / 2f).normalized;
			Vector2 prevAngleDir	= startAngleDir;

			for (int i = 1; i <= lineRoundness; i++)
			{
				Vector2 angleDir	= startAngleDir.Rotate(-i * angleStep).normalized;
				Vector2 point1		= center + prevAngleDir * thickness;
				Vector2 point2		= center + angleDir * thickness;

				int index = vh.currentVertCount;

				vh.AddVert(point1, color, Vector2.zero);
				vh.AddVert(point2, color, Vector2.zero);
				vh.AddVert(center, color, Vector2.zero);

				vh.AddTriangle(index, index + 1, index + 2);

				prevAngleDir = angleDir;
			}
		}

		#endregion
	}
}