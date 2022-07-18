using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BizzyBeeGames
{
	public class ToggleSlider : UIMonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
	{
		#region Inspector Variables

		[SerializeField] private bool			defaultIsOn			= true;
		
		[Space]

		[SerializeField] private RectTransform	handle				= null;
		[SerializeField] private RectTransform	handleSlideArea		= null;
		[SerializeField] private float			handleAnimSpeed		= 0f;
		[SerializeField] private bool			handleFollowsMouse	= false;

		[Space]

		[SerializeField] private Graphic		handleColorGraphic	= null;
		[SerializeField] private Color			handleOnColor		= Color.white;
		[SerializeField] private Color			handleOffColor		= Color.white;

		[Space]

		[SerializeField] private Text 			onText				= null;
		[SerializeField] private Text 			offText				= null;

		#endregion

		#region Member Variables

		private Camera	canvasCamera;
		private bool	isHandleMoving;
		private bool	isHandleAnimating;

		private bool toggleHasBeenSet;

		#endregion

		#region Properties

		public bool IsOn { get; set; }

		public System.Action<bool> OnValueChanged { get; set; }

		#endregion

		#region Unity Methods

		private void Start()
		{
			canvasCamera = Utilities.GetCanvasCamera(transform);

			if (!toggleHasBeenSet)
			{
				SetToggle(defaultIsOn, false);
			}
		}

		private void Update()
		{
			if (isHandleMoving || isHandleAnimating)
			{
				SetUI((handle.anchoredPosition.x + handleSlideArea.rect.width / 2f) / handleSlideArea.rect.width);
			}
		}

		#endregion

		#region Public Methods

		public void OnPointerDown(PointerEventData data)
		{
			isHandleMoving = true;

			UpdateHandlePosition(data.position);
		}

		public void OnDrag(PointerEventData data)
		{
			UpdateHandlePosition(data.position);
		}

		public void OnPointerUp(PointerEventData data)
		{
			isHandleMoving = false;

			UpdateHandlePosition(data.position, true);
		}

		public void Toggle()
		{
			SetToggle(!IsOn, true);
		}

		public void SetToggle(bool on, bool animate)
		{
			toggleHasBeenSet = true;
			
			IsOn = on;

			if (OnValueChanged != null)
			{
				OnValueChanged(on);
			}

			float handleX = on ? handleSlideArea.rect.width / 2f : -handleSlideArea.rect.width / 2f;

			if (animate && handleAnimSpeed > 0)
			{
				UIAnimation anim = UIAnimation.PositionX(handle, handleX, handleAnimSpeed);

				anim.style = UIAnimation.Style.EaseOut;

				isHandleAnimating = true;

				anim.OnAnimationFinished = (GameObject obj) => 
				{
					isHandleAnimating = false;

					SetUI(on ? 1f : 0f);
				};

				anim.Play();
			}
			else
			{
				handle.anchoredPosition = new Vector2(handleX, 0f);

				SetUI(on ? 1f : 0f);
			}
		}

		#endregion

		#region Private Methods

		private void UpdateHandlePosition(Vector2 screenPosition, bool dragEnded = false)
		{
			Vector2 localPosition;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(handleSlideArea,  screenPosition, canvasCamera, out localPosition);

			float	handleX					= Mathf.Clamp(localPosition.x, -handleSlideArea.rect.width / 2f, handleSlideArea.rect.width / 2f);
			bool	isHandleInOnPosition	= handleX > 0;

			if (dragEnded || !handleFollowsMouse)
			{
				if (isHandleInOnPosition && !IsOn)
				{
					SetToggle(true, true);
				}
				else if (!isHandleInOnPosition && IsOn)
				{
					SetToggle(false, true);
				}
			}
			else if (handleFollowsMouse)
			{
				handle.anchoredPosition = new Vector2(handleX, 0f);
			}
		}

		private void SetUI(float t)
		{
			handleColorGraphic.color = Color.Lerp(handleOffColor, handleOnColor, t);

			Color onTextColorOn = onText.color;
			Color onTextColorOff = onText.color;

			onTextColorOn.a		= 1f;
			onTextColorOff.a	= 0f;

			onText.color = Color.Lerp(onTextColorOff, onTextColorOn, t);

			Color offTextColorOn	= offText.color;
			Color offTextColorOff	= offText.color;

			offTextColorOn.a	= 0f;
			offTextColorOff.a	= 1f;

			offText.color = Color.Lerp(offTextColorOff, offTextColorOn, t);
		}

		#endregion
	}
}
