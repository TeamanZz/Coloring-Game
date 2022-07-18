using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	public class RewardAdGrantedPopup : Popup
	{
		#region Inspector Variables

		[SerializeField] private Text titleText;
		[SerializeField] private Text messageText;

		#endregion

		#region Unity Methods

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			string title	= inData[0] as string;
			string message	= inData[1] as string;

			titleText.text		= title;
			messageText.text	= message;
		}

		#endregion
	}
}
