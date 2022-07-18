using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public class ProgressBar : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private RectTransform	barFillArea	= null;
		[SerializeField] private RectTransform	bar			= null;
		[SerializeField] private float			minSize		= 60;

		#endregion

		#region Member Variables

		private bool	setOnUpdate;
		private float	setProgress;

		#endregion

		#region Unity Methods

		private void Update()
		{
			if (setOnUpdate)
			{
				StartCoroutine(SetNextFrame(setProgress));
				setOnUpdate		= false;
			}
		}

		#endregion

		#region Public Methods

		public void SetProgress(float progress)
		{
			if (gameObject.activeInHierarchy)
			{
				StartCoroutine(SetNextFrame(progress));
			}
			else
			{
				setOnUpdate = true;
				setProgress = progress;
			}
		}

		private IEnumerator SetNextFrame(float progress)
		{
			yield return new WaitForEndOfFrame();

			bar.sizeDelta = new Vector2(GetBarWidth(progress), bar.sizeDelta.y);
		}

		public void SetProgressAnimated(float fromProgress, float toProgress, float animDuration, float startDelay)
		{
			UIAnimation.DestroyAllAnimations(bar.gameObject);

			float fromBarWidth	= GetBarWidth(fromProgress);
			float toBarWidth	= GetBarWidth(toProgress);

			bar.sizeDelta = new Vector2(fromBarWidth, bar.sizeDelta.y);

			UIAnimation anim = UIAnimation.Width(bar, fromBarWidth, toBarWidth, animDuration);

			anim.startDelay = startDelay;

			anim.Play();
		}

		#endregion

		#region Private Methods

		private float GetBarWidth(float progress)
		{
			float fillWidth	= barFillArea.rect.width - minSize;

			return minSize + fillWidth * progress;
		}

		#endregion
	}
}
