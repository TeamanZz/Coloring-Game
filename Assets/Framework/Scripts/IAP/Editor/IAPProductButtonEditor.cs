using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames
{
	[CustomEditor(typeof(IAPProductButton))]
	public class IAPProductButtonEditor : Editor
	{
		#region Public Methods

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.Space();

			DrawProductIdsDropdown();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionText"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("priceText"));

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Private Methods

		private void DrawProductIdsDropdown()
		{
			if (!IAPSettings.IsIAPEnabled)
			{
				EditorGUILayout.HelpBox("IAP is not enabled.", MessageType.Warning);
			}

			SerializedProperty	productIndexProp	= serializedObject.FindProperty("productIndex");
			List<string>		productIds			= new List<string>();

			// Gather all the prodcut ids so we can display then in a dropdown
			for (int i = 0; i < IAPSettings.Instance.productInfos.Count; i++)
			{
				string productId = IAPSettings.Instance.productInfos[i].productId;

				if (!string.IsNullOrEmpty(productId))
				{
					productIds.Add(productId);
				}
			}

			if (productIds.Count == 0)
			{
				productIds.Add("---");

				EditorGUILayout.HelpBox("There are no product ids set in the IAP Manager", MessageType.Warning);

				EditorGUILayout.Popup("Product Id", 0, productIds.ToArray());
			}
			else
			{
				productIds.Insert(0, "< unassigned >");

				productIndexProp.intValue = EditorGUILayout.Popup("Product Id", productIndexProp.intValue, productIds.ToArray());
			}

			GUI.enabled = true;
		}

		#endregion
	}
}
