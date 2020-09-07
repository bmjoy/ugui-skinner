using Pspkurara.UI.Skinner;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pspkurara.UI
{

	/// <summary>
	/// <see cref="UISkinner"/>のインスペクター
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(UISkinner))]
	internal partial class UISkinnerInspector : Editor
	{

		const string FOLDOUT_EDITOR_PREFS_KEY = "COMPONENT_SKINNER_FOLDOUT_";

		public static class FieldName
		{
			public const string StyleIndex = "m_StyleIndex";
			public const string Styles = "m_Styles";
			public const string StyleKey = "m_StyleKey";
			public const string Parts = "m_Parts";
			public const string Type = "m_Type";
			public const string Property = "m_Property";
		}

		#region TempField

		private SerializedProperty currentStyle;
		private SerializedProperty skinnerObject;

		private EditorSkinPartsPropertry skinnerPartsProperty = null;

		private int[] m_SkinnerPartsOptionValues = null;
		private GUIContent[] m_SkinnerPartsDisplayNames = null;
		private GUIContent m_SkinFoldoutTitle = null;
		private GUIContent m_CurrentSelectStyleTitle = null;

		#endregion

		#region メソッド

		private void OnEnable()
		{
			m_SkinnerPartsOptionValues = SkinPartsAccess.GetAllSkinPartsIds();
			m_SkinnerPartsDisplayNames = m_SkinnerPartsOptionValues.Select(id => new GUIContent(SkinnerEditorUtility.GetEditorName(SkinPartsAccess.GetSkinPartsRootType(id).Name))).ToArray();

			m_SkinFoldoutTitle = new GUIContent();
			m_CurrentSelectStyleTitle = new GUIContent();

			skinnerObject = serializedObject.FindProperty(FieldName.Styles);
			currentStyle = serializedObject.FindProperty(FieldName.StyleIndex);

			skinnerPartsProperty = new EditorSkinPartsPropertry();
		}

		public override void OnInspectorGUI()
		{

			serializedObject.Update();

			int edittedCurrentStyle = currentStyle.intValue;
			GUILayout.Label(EditorConst.CurrentSelectStyleTitle);
			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(EditorConst.LeftSkinSelectArrow, EditorConst.SkinSelectArrowMaxWidth))
				{
					edittedCurrentStyle--;
				}
				GUILayout.FlexibleSpace();
				m_CurrentSelectStyleTitle.text = currentStyle.hasMultipleDifferentValues ? EditorConst.CurrentSkinHasMultipleDifferentValue : edittedCurrentStyle.ToString();
				GUILayout.Label(m_CurrentSelectStyleTitle);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(EditorConst.RightSkinSelectArrow, EditorConst.SkinSelectArrowMaxWidth))
				{
					edittedCurrentStyle++;
				}
			}
			GUILayout.EndHorizontal();

			SkinnerEditorUtility.DrawLine();

			if (currentStyle.intValue != edittedCurrentStyle)
			{
				foreach (Object t in serializedObject.targetObjects)
				{
					UISkinner skinnerObj = t as UISkinner;
					skinnerObj.SetSkin(Mathf.Clamp(edittedCurrentStyle, 0, skinnerObj.Length - 1));
				}
			}

			for (int skinnerObjectIndex = 0; skinnerObjectIndex < skinnerObject.arraySize; skinnerObjectIndex++)
			{

				SerializedProperty objProp = skinnerObject.GetArrayElementAtIndex(skinnerObjectIndex).FindPropertyRelative(FieldName.Parts);
				SerializedProperty styleKey = skinnerObject.GetArrayElementAtIndex(skinnerObjectIndex).FindPropertyRelative(FieldName.StyleKey);

				GUIStyle style = (edittedCurrentStyle == skinnerObjectIndex) ? EditorConst.HighLightFoldoutStyle : EditorConst.NormalFoldoutStyle;

				bool hasStyleKey = !string.IsNullOrEmpty(styleKey.stringValue);

				EditorGUILayout.BeginHorizontal();
				m_SkinFoldoutTitle.text = hasStyleKey ? string.Format(EditorConst.SkinFoldTitleHasStyleKey, skinnerObjectIndex, styleKey.stringValue) : string.Format(EditorConst.SkinFoldTitle, skinnerObjectIndex);
				bool foldOut = EditorGUILayout.Foldout(GetFoldOut(skinnerObjectIndex), m_SkinFoldoutTitle, style);
				SetFoldOut(skinnerObjectIndex, foldOut);

				if (GetFoldOut(skinnerObjectIndex))
				{
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.PropertyField(styleKey, EditorConst.SkinnerStyleKeyFieldTitle);

					for (int skinnerPartsIndex = 0; skinnerPartsIndex < objProp.arraySize; skinnerPartsIndex++)
					{

						SerializedProperty partsProp = objProp.GetArrayElementAtIndex(skinnerPartsIndex);
						SerializedProperty uiSkinnedPartsTypeProperty = partsProp.FindPropertyRelative(FieldName.Type);
						int uiSkinnerPartsType = uiSkinnedPartsTypeProperty.intValue;
						uiSkinnedPartsTypeProperty.intValue = EditorGUILayout.IntPopup(uiSkinnerPartsType, m_SkinnerPartsDisplayNames, m_SkinnerPartsOptionValues);

						skinnerPartsProperty.MapProperties(partsProp.FindPropertyRelative(FieldName.Property));

						var rootType = SkinPartsAccess.GetSkinPartsRootType(uiSkinnerPartsType);
						var inspector = SkinPartsInspectorAccess.GetSkinInspector(rootType);

						EditorGUI.indentLevel++;

						inspector.DrawInspector(skinnerPartsProperty);

						EditorGUI.indentLevel--;

						if (SkinnerEditorUtility.DrawRemoveButton(EditorConst.RemovePartsButtonTitle, () => {
							objProp.DeleteArrayElementAtIndex(skinnerPartsIndex);
							serializedObject.ApplyModifiedProperties();
						})) return;
					}

					EditorGUILayout.Space();
					EditorGUILayout.BeginHorizontal();
					if (SkinnerEditorUtility.DrawAddButton(EditorConst.AddPartsButtonTitle, () => {
						objProp.InsertArrayElementAtIndex(objProp.arraySize);
						serializedObject.ApplyModifiedProperties();
					})) return;

					EditorGUILayout.Space();
				}

				if (SkinnerEditorUtility.DrawRemoveButton(EditorConst.RemoveSkinButtonTitle, () => {
					skinnerObject.DeleteArrayElementAtIndex(skinnerObjectIndex);
					serializedObject.ApplyModifiedProperties();
				})) return;
				EditorGUILayout.EndHorizontal();

				if (foldOut)
				{
					EditorGUILayout.Space();
					SkinnerEditorUtility.DrawLine();
					EditorGUILayout.Space();
				}
				serializedObject.ApplyModifiedProperties();
			}

			EditorGUILayout.BeginHorizontal();

			if (SkinnerEditorUtility.DrawAddButton(EditorConst.AddSkinButtonTitle, () => {
				skinnerObject.InsertArrayElementAtIndex(skinnerObject.arraySize);
				serializedObject.ApplyModifiedProperties();
			})) return;

			EditorGUILayout.Space();

			if (SkinnerEditorUtility.DrawCleanupButton(EditorConst.CleanupButtonTitle, () => {
				Cleanup();
				serializedObject.ApplyModifiedProperties();
			})) return;

			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();

		}

		private void Cleanup()
		{
			for (int skinnerObjectIndex = 0; skinnerObjectIndex < skinnerObject.arraySize; skinnerObjectIndex++)
			{
				SerializedProperty objProp = skinnerObject.GetArrayElementAtIndex(skinnerObjectIndex).FindPropertyRelative(FieldName.Parts);
				for (int skinnerPartsIndex = 0; skinnerPartsIndex < objProp.arraySize; skinnerPartsIndex++)
				{
					SerializedProperty partsProp = objProp.GetArrayElementAtIndex(skinnerPartsIndex);
					SerializedProperty uiSkinnedPartsTypeProperty = partsProp.FindPropertyRelative(FieldName.Type);
					int uiSkinnerPartsType = uiSkinnedPartsTypeProperty.intValue;

					var rootType = SkinPartsAccess.GetSkinPartsRootType(uiSkinnerPartsType);
					var inspector = SkinPartsInspectorAccess.GetSkinInspector(rootType);

					skinnerPartsProperty.MapProperties(partsProp.FindPropertyRelative(FieldName.Property));

					inspector.CleanupFields(skinnerPartsProperty);

				}
			}
		}

		#region EditorPrefs

		private static string GetFoldoutKey(int foldOutIndex)
		{
			return FOLDOUT_EDITOR_PREFS_KEY + foldOutIndex;
		}

		private static bool GetFoldOut(int foldOutIndex)
		{
			return EditorPrefs.GetBool(GetFoldoutKey(foldOutIndex), true);
		}

		private static void SetFoldOut(int foldOutIndex, bool value)
		{
			EditorPrefs.SetBool(GetFoldoutKey(foldOutIndex), value);
		}

		#endregion

		#endregion

	}

}