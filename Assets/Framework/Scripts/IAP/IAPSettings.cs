using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public class IAPSettings : SingletonScriptableObject<IAPSettings>
	{
		#region Classes

		[System.Serializable]
		public class ProductInfo
		{
			public string	productId;
			public bool		consumable;
		}

		#endregion

		#region Member Variables

		public List<ProductInfo> productInfos = new List<ProductInfo>();

		#endregion

		#region Properties

		public static bool IsIAPEnabled
		{
			get
			{
				#if BBG_IAP
				return true;
				#else
				return false;
				#endif
			}
		}

		#endregion
	}
}
