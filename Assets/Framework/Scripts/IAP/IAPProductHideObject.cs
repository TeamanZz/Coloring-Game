using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if BBG_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace BizzyBeeGames
{
	/// <summary>
	/// This class will set the GameObject it is attached to de-active when the product id has been purchased
	/// </summary>
	public class IAPProductHideObject : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string	productId = "";

		#endregion

		#region Unity Methods

		private void Start()
		{
			IAPManager.Instance.OnIAPInitialized	+= OnIAPInitialized;
			IAPManager.Instance.OnProductPurchased	+= OnProductPurchased;

			CheckIsPurchased();
		}

		#endregion

		#region Private Methods

		private void OnProductPurchased(string id)
		{
			if (productId == id)
			{
				CheckIsPurchased();
			}
		}

		private void OnIAPInitialized(bool success)
		{
			CheckIsPurchased();
		}

		private void CheckIsPurchased()
		{
			if (IAPManager.Instance.IsInitialized)
			{
				#if BBG_IAP
				Product product = IAPManager.Instance.GetProductInformation(productId);

				if (product != null && product.availableToPurchase && IAPManager.Instance.IsProductPurchased(productId))
				{
					gameObject.SetActive(false);
					IAPManager.Instance.OnIAPInitialized	-= OnIAPInitialized;
					IAPManager.Instance.OnProductPurchased	-= OnProductPurchased;
				}
				else
				#endif
				{
					gameObject.SetActive(true);
				}
			}
		}

		#endregion
	}
}
