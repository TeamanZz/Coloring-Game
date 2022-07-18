using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	[RequireComponent(typeof(Button))]
	public class OpenLinkButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string url = "";

		#endregion

		#region Unity Methods

		private void Start()
		{
			gameObject.GetComponent<Button>().onClick.AddListener(() => { Application.OpenURL(url); });
		}

		#endregion
	}
}
