using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	[RequireComponent(typeof(Text))]
	public class CurrencyText : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string	currencyId			= "";
		[SerializeField] private bool	displayZeroString	= false;
		[SerializeField] private string	zeroString			= "";

		#endregion

		#region Member Variables

		private Text uiText;

		#endregion

		#region Unity Methods

		private void Start()
		{
			uiText = gameObject.GetComponent<Text>();

			UpdateAmountText();

			CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
		}

		#endregion

		#region Private Methods

		private void OnCurrencyChanged(string id)
		{
			if (currencyId == id)
			{
				UpdateAmountText();
			}
		}

		private void UpdateAmountText()
		{
			int amount = CurrencyManager.Instance.GetAmount(currencyId);

			uiText.text = (amount == 0 && displayZeroString) ? zeroString : amount.ToString();
		}

		#endregion
	}
}
