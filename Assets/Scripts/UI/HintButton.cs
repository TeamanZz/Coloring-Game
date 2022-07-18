using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
	public class HintButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private Text hintAmountText = null;

		#endregion

		#region Unity Methods

		private void Start()
		{
			UpdateUI();

			CurrencyManager.Instance.OnCurrencyChanged += (string obj) => { UpdateUI(); };
		}

		#endregion

		#region Private Methods

		private void UpdateUI()
		{
			hintAmountText.text = CurrencyManager.Instance.GetAmount("hints").ToString();
		}

		#endregion
	}
}
