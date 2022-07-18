using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
	public class PermissionsPopup : Popup
	{
		#region Inspector Variables

		[SerializeField] private Text messageText = null;

		#endregion

		#region Member Variables

		private const string messageBody = "The required permission has not been granted to this application.\n\nPlease open your device settings and give this application the required {0} permission. Thank you!";

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			string permission = (string)inData[0];

			messageText.text = string.Format(messageBody, permission);
		}

		#endregion
	}
}
