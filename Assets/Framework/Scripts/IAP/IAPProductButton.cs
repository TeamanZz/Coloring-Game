using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if BBG_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

#pragma warning disable 0414 // Reason: Some inspector variables are only used in specific platforms and their usages are removed using #if blocks

namespace BizzyBeeGames
{
	[RequireComponent(typeof(Button))]
	public class IAPProductButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private int	productIndex	= 0;
		[SerializeField] private Text	titleText		= null;
		[SerializeField] private Text	descriptionText	= null;
		[SerializeField] private Text	priceText		= null;

		#endregion

		#region Member Variables

		private const string LogTag = "IAPProductButton";

		private Button button;

		#endregion

		#region Properties

		private IAPSettings.ProductInfo AssignedProductInfo
		{
			get
			{
				if (productIndex == 0 || productIndex - 1 >= IAPSettings.Instance.productInfos.Count) return null;

				return IAPSettings.Instance.productInfos[productIndex - 1];
			}
		}
		
		#endregion // Properties

		#region Unity Methods

		private void Start()
		{
			button = gameObject.GetComponent<Button>();

			button.onClick.AddListener(OnClicked);

			UpdateButton();
			
			IAPManager.Instance.OnIAPInitialized	+= OnIAPInitialized;
			IAPManager.Instance.OnProductPurchased	+= OnProductPurchased;
		}

		#endregion

		#region Private Methods

		private void OnClicked()
		{
			#if BBG_IAP
			if (AssignedProductInfo != null)
			{
				IAPManager.Instance.BuyProduct(AssignedProductInfo.productId);
			}
			#endif
		}

		private void OnProductPurchased(string id)
		{
			if (AssignedProductInfo != null && AssignedProductInfo.productId == id)
			{
				UpdateButton();
			}
		}

		private void OnIAPInitialized(bool success)
		{
			UpdateButton();
		}

		private void UpdateButton()
		{
			button.interactable = false;

			IAPSettings.ProductInfo productInfo = AssignedProductInfo;

			if (IAPManager.Instance.IsInitialized)
			{
				#if BBG_IAP
				if (AssignedProductInfo == null)
				{
					GameDebugManager.LogError(LogTag, "IAP Product Button does not have a product id assigned to it.");
					SetTitleText("un-assigned");
				}
				else
				{
					Product product = IAPManager.Instance.GetProductInformation(productInfo.productId);

					if (product == null)
					{
						GameDebugManager.LogError(LogTag, "Product with id \"" + productInfo.productId + "\" does not exist.");
						SetTitleText(productInfo.productId + ": does not exist");
					}
					else if (!product.availableToPurchase)
					{
						GameDebugManager.LogError(LogTag, "Product with id \"" + productInfo.productId + "\" is not available to purchase.");

						SetTitleText(productInfo.productId + ": un-available");
					}
					else
					{
						button.interactable = true;

						SetupButton(product);
					}
				}
				#endif
			}
		}

		#if BBG_IAP
		private void SetupButton(Product product)
		{
			if (IAPManager.Instance.IsProductPurchased(product.definition.id))
			{
				// If the product has been purchased then hide the button (Only for non-consumable products)
				gameObject.SetActive(false);
			}
			else
			{
				gameObject.SetActive(true);

				if (priceText != null)
				{
					priceText.text = product.metadata.localizedPriceString;
				}

				if (titleText != null)
				{
					#if UNITY_EDITOR
					titleText.text = product.definition.id;
					#else
					string title = product.metadata.localizedTitle;
					
					// Strip the "(App Name)" text that is included by google for some reason
					int startIndex	= title.LastIndexOf('(');
					int endIndex	= title.LastIndexOf(')');
					
					if (startIndex > 0 && endIndex > 0 && startIndex < endIndex)
					{
					title = title.Remove(startIndex, endIndex - startIndex + 1);
					title = title.Trim();
					}
					
					titleText.text = title;
					#endif
				}

				if (descriptionText != null)
				{
					descriptionText.text = product.metadata.localizedDescription;
				}
			}
		}
		#endif

		private void SetTitleText(string text)
		{
			if (titleText != null)
			{
				titleText.text = text;
			}
		}

		#endregion
	}
}
