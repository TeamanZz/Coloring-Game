using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BizzyBeeGames
{
	public class ProgressRing : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private RectTransform	firstHalf	= null;
		[SerializeField] private RectTransform	secondHalf	= null;

		#endregion

		#region Unity Methods

		private void Awake()
		{
			SetProgress(0f);
		}

		#endregion

		#region Public Methods

		public void SetProgress(float percent)
		{
			float z1 = Mathf.Lerp(180f, 0f, Mathf.Clamp01(percent * 2f));
			float z2 = Mathf.Lerp(180f, 0f, Mathf.Clamp01((percent - 0.5f) * 2f));

			firstHalf.localEulerAngles	= new Vector3(firstHalf.localEulerAngles.x, firstHalf.localEulerAngles.y, z1);
			secondHalf.localEulerAngles	= new Vector3(secondHalf.localEulerAngles.x, secondHalf.localEulerAngles.y, z2);
		}

		#endregion
	}
}
