using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames
{
	[CustomEditor(typeof(IAPManager))]
	public class IAPManagerEditor : Editor
	{
		#region Member Variables

		private SerializedObject settingsSerializedObject;

		private bool showNoPluginError;
		private bool showBadPluginError;

		private Texture2D lineTexture;

		#endregion

		#region Properties

		private SerializedObject SettingsSerializedObject
		{
			get
			{
				if (settingsSerializedObject == null)
				{
					settingsSerializedObject = new SerializedObject(IAPSettings.Instance);
				}

				return settingsSerializedObject;
			}
		}

		private Texture2D LineTexture
		{
			get
			{
				if (lineTexture == null)
				{
					lineTexture = new Texture2D(1, 1);
					lineTexture.SetPixel(0, 0, new Color(37f/255f, 37f/255f, 37f/255f));
					lineTexture.Apply();
				}

				return lineTexture;
			}
		}

		#endregion

		#region Unity Methods

		private void OnDisable()
		{
			DestroyImmediate(LineTexture);
		}

		#endregion

		#region Public Methods

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			SettingsSerializedObject.Update();

			EditorGUILayout.Space();

			DrawEnableDisableButtons();
			
			EditorGUILayout.Space();

			BeginBox("Product Information");

			EditorGUI.indentLevel++;

			DrawProductInformation();

			EditorGUI.indentLevel--;

			EndBox();

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
			SettingsSerializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Protected Methods

		#endregion

		#region Private Methods

		private void DrawProductInformation()
		{
			SerializedProperty productInfosProp		= SettingsSerializedObject.FindProperty("productInfos");
			SerializedProperty purchaseEventsProp	= serializedObject.FindProperty("purchaseEvents");

			bool	deleted			= false;
			int		deletedIndex	= 0;

			for (int i = 0; i < productInfosProp.arraySize; i++)
			{
				SerializedProperty productInfoProp		= productInfosProp.GetArrayElementAtIndex(i);
				SerializedProperty purchaseEventProp	= purchaseEventsProp.GetArrayElementAtIndex(i);

				if (DrawProduct(productInfoProp, purchaseEventProp))
				{
					deleted			= true;
					deletedIndex	= i;
				}
			}

			if (deleted)
			{
				productInfosProp.DeleteArrayElementAtIndex(deletedIndex);
				purchaseEventsProp.DeleteArrayElementAtIndex(deletedIndex);
			}

			if (productInfosProp.arraySize == 0)
			{
				EditorGUILayout.LabelField("No products, click the button below to add a product.");
			}

			if (GUILayout.Button("Add Product"))
			{
				AddNewProductInformation(productInfosProp);
				AddNewPurchaseEvent(purchaseEventsProp);
			}
		}

		private bool DrawProduct(SerializedProperty productInfoProp, SerializedProperty purchaseEventProp)
		{
			bool deleted = false;

			EditorGUILayout.PropertyField(productInfoProp, false);

			if (productInfoProp.isExpanded)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(productInfoProp.FindPropertyRelative("productId"));
				if (GUILayout.Button("Delete", GUILayout.Width(50)))
				{
					deleted = true;
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.PropertyField(productInfoProp.FindPropertyRelative("consumable"));

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(35);
				EditorGUILayout.PropertyField(purchaseEventProp.FindPropertyRelative("onProductPurchasedEvent"));
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel--;
			}

			return deleted;
		}

		private void AddNewPurchaseEvent(SerializedProperty purchaseEventsProp)
		{
			int index = purchaseEventsProp.arraySize;

			purchaseEventsProp.InsertArrayElementAtIndex(index);

			// Clear the events since Unity copies them from the last array element
			SerializedProperty purchaseEventProp	= purchaseEventsProp.GetArrayElementAtIndex(index);
			SerializedProperty eventsProp			= purchaseEventProp.FindPropertyRelative("onProductPurchasedEvent");
			SerializedProperty persistentCalls		= eventsProp.FindPropertyRelative ("m_PersistentCalls.m_Calls");

			for (int i = persistentCalls.arraySize - 1; i >= 0; i--)
			{
				persistentCalls.DeleteArrayElementAtIndex(i);
			}
		}

		private void AddNewProductInformation(SerializedProperty productInfosProp)
		{
			int index = productInfosProp.arraySize;

			productInfosProp.InsertArrayElementAtIndex(index);

			// Clear the values since Unity copies them from the last array element
			SerializedProperty productInfoProp	= productInfosProp.GetArrayElementAtIndex(index);

			productInfoProp.FindPropertyRelative("productId").stringValue	= "";;
			productInfoProp.FindPropertyRelative("consumable").boolValue	= false;
		}

		private int FindPurchaseEvent(SerializedProperty purchaseEventsProp, string productId, int startIndex)
		{
			for (int i = startIndex; i < purchaseEventsProp.arraySize; i++)
			{
				if (productId == purchaseEventsProp.GetArrayElementAtIndex(i).FindPropertyRelative("productId").stringValue)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Begins a new box, must call EndBox
		/// </summary>
		private void BeginBox(string boxTitle = "")
		{
			GUIStyle style		= new GUIStyle("HelpBox");
			style.padding.left	= 0;
			style.padding.right	= 0;

			GUILayout.BeginVertical(style);

			if (!string.IsNullOrEmpty(boxTitle))
			{
				DrawBoldLabel(boxTitle);

				DrawLine();
			}
		}

		/// <summary>
		/// Ends the box.
		/// </summary>
		private void EndBox()
		{
			GUILayout.EndVertical();
		}

		/// <summary>
		/// Draws a bold label
		/// </summary>
		protected void DrawBoldLabel(string text)
		{
			EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
		}

		/// <summary>
		/// Draws a simple 1 pixel height line
		/// </summary>
		private void DrawLine()
		{
			GUIStyle lineStyle			= new GUIStyle();
			lineStyle.normal.background	= LineTexture;

			GUILayout.BeginVertical(lineStyle);
			GUILayout.Space(1);
			GUILayout.EndVertical();
		}




		private void DrawEnableDisableButtons()
		{
			if (!IAPSettings.IsIAPEnabled)
			{
				EditorGUILayout.HelpBox("IAP is not enabled, please import the IAP plugin using the Services window then click the button below.", MessageType.Info);

				if (GUILayout.Button("Enable IAP"))
				{
					if (!EditorUtilities.CheckNamespacesExists("UnityEngine.Purchasing"))
					{
						showNoPluginError = true;
					}
					else if (!EditorUtilities.CheckClassExists("UnityEngine.Purchasing", "StandardPurchasingModule"))
					{
						showBadPluginError = true;
					}
					else
					{
						showNoPluginError	= false;
						showBadPluginError	= false;

						EditorUtilities.SyncScriptingDefineSymbols("BBG_IAP", true);
					}
				}

				if (showNoPluginError)
				{
					EditorGUILayout.HelpBox("The Unity IAP plugin was not been detected. Please import the Unity IAP plugin using the Services window and make sure there are no compiler errors in your project. Consult the documentation for more information.", MessageType.Error);
				}
				else if (showBadPluginError)
				{
					EditorGUILayout.HelpBox("The Unity IAP plugin has not been imported correctly. Please click the re-import button in the IAP section of the Services window.", MessageType.Error);
				}
			}
			else
			{
				if (GUILayout.Button("Disable IAP"))
				{
					// Remove BBG_IAP from scripting define symbols
					EditorUtilities.SyncScriptingDefineSymbols("BBG_IAP", false);
				}
			}
		}


		#endregion
	}
}
