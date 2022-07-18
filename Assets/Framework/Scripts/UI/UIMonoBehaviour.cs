using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public class UIMonoBehaviour : MonoBehaviour
	{
		#region Properties

		public RectTransform RectT { get { return transform as RectTransform; } }

		#endregion

		#region Member Variables

		private IEnumerator shakeRoutine;
		private IEnumerator pulseRoutine;
		private IEnumerator twistRoutine;

		private CanvasGroup canvasGroup;

		#endregion

		#region Properties

		public CanvasGroup CG
		{
			get
			{
				if (canvasGroup == null)
				{
						canvasGroup = gameObject.GetComponent<CanvasGroup>();

						if (canvasGroup == null)
						{
							canvasGroup = gameObject.AddComponent<CanvasGroup>();
						}
				}

				return canvasGroup;
			}
		}

		#endregion

		#region Animations

		public void Shake(float origX, int shakeAmount, float shakeForce, float shakeAnimDuration)
		{
			StopRoutine(shakeRoutine);

			StartCoroutine(shakeRoutine = StartShake(origX, shakeAmount, shakeForce, shakeAnimDuration));
		}

		private IEnumerator StartShake(float origX, int shakeAmount, float shakeForce, float shakeAnimDuration)
		{
			for (int i = 0; i < shakeAmount; i++)
			{
				if (i % 2 == 0)
				{
					ShakeLeft(origX, shakeAnimDuration, shakeForce);
				}
				else
				{
					ShakeRight(origX, shakeAnimDuration, shakeForce);
				}

				yield return new WaitForSeconds(shakeAnimDuration);
			}

			// Move it back to the middle
			UIAnimation.PositionX(transform as RectTransform, origX, shakeAnimDuration).Play();

			shakeRoutine = null;
		}

		private void ShakeLeft(float origX, float animDuration, float shakeForce)
		{
			UIAnimation.PositionX(transform as RectTransform, origX - shakeForce, animDuration).Play();
		}

		private void ShakeRight(float origX, float animDuration, float shakeForce)
		{
			UIAnimation.PositionX(transform as RectTransform, origX + shakeForce, animDuration).Play();
		}

		public void Pulse(Vector2 origScale, int pulseAmount, float pulseForce, float pulseAnimDuration)
		{
			StopRoutine(pulseRoutine);

			StartCoroutine(pulseRoutine = StartPulse(origScale, pulseAmount, pulseForce, pulseAnimDuration));
		}

		private IEnumerator StartPulse(Vector2 origScale, int pulseAmount, float pulseForce, float pulseAnimDuration)
		{
			for (int i = 0; i < pulseAmount; i++)
			{
				UIAnimation.ScaleX(transform as RectTransform, origScale.x * pulseForce, pulseAnimDuration / 2f).Play();
				UIAnimation.ScaleY(transform as RectTransform, origScale.y * pulseForce, pulseAnimDuration / 2f).Play();

				yield return new WaitForSeconds(pulseAnimDuration / 2f);

				UIAnimation.ScaleX(transform as RectTransform, origScale.x, pulseAnimDuration / 2f).Play();
				UIAnimation.ScaleY(transform as RectTransform, origScale.y, pulseAnimDuration / 2f).Play();

				yield return new WaitForSeconds(pulseAnimDuration / 2f);
			}

			pulseRoutine = null;
		}

		public void Twist(int twistAmount, float twistForce, float twistAnimDuration)
		{
			StopRoutine(twistRoutine);

			StartCoroutine(twistRoutine = StartTwist(twistAmount, twistForce, twistAnimDuration));
		}

		private IEnumerator StartTwist(int twistAmount, float twistForce, float twistAnimDuration)
		{
			UIAnimation.RotationZ(transform as RectTransform, 0, -twistForce, twistAnimDuration).Play();

			yield return new WaitForSeconds(twistAnimDuration);

			float from	= -twistForce;
			float to	= twistForce;

			for (int i = 0; i < twistAmount; i++)
			{
				if (i % 2 == 0)
				{
					from	= -twistForce;
					to		= twistForce;
				}
				else
				{
					from	= twistForce;
					to		= -twistForce;
				}

				UIAnimation.RotationZ(transform as RectTransform, from, to, twistAnimDuration).Play();

				yield return new WaitForSeconds(twistAnimDuration);
			}

			UIAnimation.RotationZ(transform as RectTransform, to, 0, twistAnimDuration).Play();

			twistRoutine = null;
		}

		#endregion

		#region Private Methods

		private void StopRoutine(IEnumerator routine)
		{
			if (routine != null)
			{
				StopCoroutine(routine);
			}
		}

		#endregion
	}
}
