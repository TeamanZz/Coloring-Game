﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BizzyBeeGames.PictureColoring
{
	public class HintButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private TextMeshProUGUI hintAmountText = null;
		[SerializeField] private Image countView;

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
			int count = CurrencyManager.Instance.GetAmount("hints");
			hintAmountText.text = count.ToString();
			
			if(count == 0)
				countView.gameObject.SetActive(false);
            else
				countView.gameObject.SetActive(true);
		}

		#endregion
	}
}
