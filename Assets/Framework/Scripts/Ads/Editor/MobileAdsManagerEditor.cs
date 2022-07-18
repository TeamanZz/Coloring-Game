using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames
{
	[CustomEditor(typeof(MobileAdsManager))]
	public class MobileAdsManagerEditor : Editor
	{
		#region Public Methods

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("retryLoadIfFailed"));

			GUI.enabled = serializedObject.FindProperty("retryLoadIfFailed").boolValue;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("retryWaitTime"));

			GUI.enabled = true;

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("showConsentPopup"));

			GUI.enabled = serializedObject.FindProperty("showConsentPopup").boolValue;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("consentPopupId"));

			GUI.enabled = true;

			EditorGUILayout.Space();

			if (GUILayout.Button("Open Mobile Ads Settings Window"))
			{
				MobileAdsSettingsWindow.Open();
			}

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		#endregion
	}
}
