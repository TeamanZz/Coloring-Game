using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BizzyBeeGames
{
	public class Popup : UIMonoBehaviour
	{
		#region Enums

		protected enum AnimType
		{
			Fade,
			Zoom
		}

		private enum State
		{
			Shown,
			Hidden,
			Showing,
			Hidding
		}

		#endregion

		#region Inspector Variables

		[SerializeField] protected bool				canAndroidBackClosePopup;

		[Header("Anim Settings")]
		[SerializeField] protected float			animDuration;
		[SerializeField] protected AnimType			animType;
		[SerializeField] protected AnimationCurve	animCurve;
		[SerializeField] protected RectTransform	animContainer;

		#endregion

		#region Member Variables

		private bool		isInitialized;
		private State		state;
		private PopupClosed	callback;

		#endregion

		#region Properties

		public bool CanAndroidBackClosePopup { get { return canAndroidBackClosePopup; } }

		#endregion

		#region Delegates

		public delegate void PopupClosed(bool cancelled, object[] outData);

		#endregion

		#region Public Methods

		public virtual void Initialize()
		{
			gameObject.SetActive(false);
			CG.alpha = 0f;
			state = State.Hidden;
		}

		public void Show()
		{
			Show(null, null);
		}

		public bool Show(object[] inData, PopupClosed callback)
		{
			if (state != State.Hidden)
			{
				return false;
			}

			this.callback	= callback;
			this.state		= State.Showing;

			// Show the popup object
			gameObject.SetActive(true);

			switch (animType)
			{
				case AnimType.Fade:
					DoFadeAnim();
					break;
				case AnimType.Zoom:
					DoZoomAnim();
					break;
			}

			OnShowing(inData);

			return true;
		}

		public void Hide(bool cancelled)
		{
			Hide(cancelled, null);
		}

		public void Hide(bool cancelled, object[] outData)
		{
			switch (state)
			{
				case State.Hidden:
				case State.Hidding:
					return;
				case State.Showing:
					UIAnimation.DestroyAllAnimations(gameObject);
					UIAnimation.DestroyAllAnimations(animContainer.gameObject);
					break;
			}

			state = State.Hidding;

			if (callback != null)
			{
				callback(cancelled, outData);
			}

			// Start the popup hide animations
			UIAnimation anim = null;

			anim = UIAnimation.Alpha(CG, 1f, 0f, animDuration);
			anim.style = UIAnimation.Style.EaseOut;
			anim.startOnFirstFrame = true;

			anim.OnAnimationFinished += (GameObject target) => 
			{
				state = State.Hidden;
				gameObject.SetActive(false);
			};

			anim.Play();

			OnHiding(cancelled);
		}

		public void HideWithAction(string action)
		{
			Hide(false, new object[] { action });
		}

		public virtual void OnShowing(object[] inData)
		{

		}

		public virtual void OnHiding(bool cancelled)
		{
			PopupManager.Instance.OnPopupHiding(this);
		}

		#endregion

		#region Private Methods

		private void DoFadeAnim()
		{
			// Start the popup show animations
			UIAnimation anim = null;

			anim = UIAnimation.Alpha(CG, 0f, 1f, animDuration);
			anim.startOnFirstFrame = true;
			anim.OnAnimationFinished += (GameObject obj) => { state = State.Shown; };
			anim.Play();
		}

		private void DoZoomAnim()
		{
			// Start the popup show animations
			UIAnimation anim = null;

			anim = UIAnimation.Alpha(CG, 0f, 1f, animDuration);
			anim.style = UIAnimation.Style.EaseOut;
			anim.startOnFirstFrame = true;
			anim.Play();

			anim					= UIAnimation.ScaleX(animContainer, 0f, 1f, animDuration);
			anim.style				= UIAnimation.Style.Custom;
			anim.animationCurve		= animCurve;
			anim.startOnFirstFrame	= true;
			anim.Play();

			anim					= UIAnimation.ScaleY(animContainer, 0f, 1f, animDuration);
			anim.style				= UIAnimation.Style.Custom;
			anim.animationCurve		= animCurve;
			anim.startOnFirstFrame	= true;
			anim.OnAnimationFinished += (GameObject obj) => { state = State.Shown; };
			anim.Play();
		}

		#endregion
	}
}