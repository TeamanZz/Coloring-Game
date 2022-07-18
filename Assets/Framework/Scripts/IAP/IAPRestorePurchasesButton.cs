using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	[RequireComponent(typeof(Button))]
	public class IAPRestorePurchasesButton : MonoBehaviour
	{
		#region Unity Methods

		private void Start()
		{
			gameObject.SetActive(Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer);
			gameObject.GetComponent<Button>().onClick.AddListener(IAPManager.Instance.RestorePurchases);
		}

		#endregion
	}
}
