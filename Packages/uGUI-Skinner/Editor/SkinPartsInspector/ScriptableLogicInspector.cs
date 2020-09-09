using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Type = System.Type;

namespace Pspkurara.UI.Skinner
{

	[SkinPartsInspector(typeof(ScriptableLogic))]
	internal sealed class ScriptableLogicInspector : ISkinPartsInspector
	{

		private sealed class UserLogicVariableDisplayData
		{

			public GUIContent DisplayName;
			public SerializedPropertyType PropertyType;
			public UserLogicVariable VariableData;
			public int FieldIndex;
			public GUIContent[] PopupDisplayName;
			public int[] PopupValue;

		}

		private static Dictionary<Type, List<UserLogicVariableDisplayData>> m_CachedVariableDisplayDatas = new Dictionary<Type, List<UserLogicVariableDisplayData>>();

		private List<UserLogicVariableDisplayData> userLogicVariableDisplayDatas = new List<UserLogicVariableDisplayData>();
		private UserLogic currentUserLogic;
		private int objectReferenceArrayCount = 0;
		private int boolArrayCount = 0;
		private int colorArrayCount = 0;
		private int floatArrayCount = 0;
		private int intArrayCount = 0;
		private int vector4ArrayCount = 0;
		private int stringArrayCount = 0;
		private SkinPartsPropertry validateProperty = new SkinPartsPropertry();

		public void CleanupFields(EditorSkinPartsPropertry property)
		{
			SkinnerEditorUtility.CleanObjectReferenceArrayWithFlexibleSize<Object>(property.objectReferenceValues, ScriptableLogic.RequiredObjectLength);

			var userLogic = property.objectReferenceValues.GetArrayElementAtIndex(ScriptableLogic.LogicIndex).objectReferenceValue as UserLogic;
			bool isCorrect = CreateDisplayData(userLogic);
			if (isCorrect)
			{
				SkinnerEditorUtility.CleanArray(property.objectReferenceValues, objectReferenceArrayCount);
				for (int i = 0; i < userLogicVariableDisplayDatas.Count; i++)
				{
					var v = userLogicVariableDisplayDatas[i];
					SkinnerEditorUtility.CleanObject(property.objectReferenceValues, v.VariableData.FieldType, i + ScriptableLogic.RequiredObjectLength);
				}
				SkinnerEditorUtility.CleanArray(property.boolValues, boolArrayCount);
				SkinnerEditorUtility.CleanArray(property.colorValues, colorArrayCount);
				SkinnerEditorUtility.CleanArray(property.floatValues, floatArrayCount);
				SkinnerEditorUtility.CleanArray(property.intValues, intArrayCount);
				SkinnerEditorUtility.CleanArray(property.vector4Values, vector4ArrayCount);
				SkinnerEditorUtility.CleanArray(property.stringValues, stringArrayCount);
			}
		}

		public void DrawInspector(EditorSkinPartsPropertry property)
		{
			SkinnerEditorUtility.ResetArray(property.objectReferenceValues, ScriptableLogic.RequiredObjectLength, false);

			var logicProperty = property.objectReferenceValues.GetArrayElementAtIndex(ScriptableLogic.LogicIndex);
			bool showMixedValue = EditorGUI.showMixedValue;
			if (logicProperty.hasMultipleDifferentValues)
			{
				EditorGUI.showMixedValue = true;
			}
			logicProperty.objectReferenceValue = EditorGUILayout.ObjectField(SkinContent.Logic, logicProperty.objectReferenceValue, typeof(UserLogic), false);
			EditorGUI.showMixedValue = showMixedValue;

			if (logicProperty.hasMultipleDifferentValues) return;

			var userLogic = property.objectReferenceValues.GetArrayElementAtIndex(ScriptableLogic.LogicIndex).objectReferenceValue as UserLogic;
			bool isCorrect = CreateDisplayData(userLogic);
			if (isCorrect)
			{
				SkinnerEditorUtility.ResetArray(property.objectReferenceValues, objectReferenceArrayCount + ScriptableLogic.RequiredObjectLength, false);
				SkinnerEditorUtility.ResetArray(property.boolValues, boolArrayCount);
				SkinnerEditorUtility.ResetArray(property.colorValues, colorArrayCount);
				SkinnerEditorUtility.ResetArray(property.floatValues, floatArrayCount);
				SkinnerEditorUtility.ResetArray(property.intValues, intArrayCount);
				SkinnerEditorUtility.ResetArray(property.vector4Values, vector4ArrayCount);
				SkinnerEditorUtility.ResetArray(property.stringValues, stringArrayCount);
				for (int i = 0; i < userLogicVariableDisplayDatas.Count; i++)
				{
					var v = userLogicVariableDisplayDatas[i];
					switch (v.PropertyType)
					{
						case SerializedPropertyType.ObjectReference:
							{
								bool isComponent = v.VariableData.FieldType.IsSubclassOf(typeof(Component));
								bool isGameObject = v.VariableData.FieldType == typeof(GameObject);
								var element = property.objectReferenceValues.GetArrayElementAtIndex(v.FieldIndex + ScriptableLogic.RequiredObjectLength);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.objectReferenceValue = EditorGUILayout.ObjectField(v.DisplayName, element.objectReferenceValue, v.VariableData.FieldType, isComponent || isGameObject);
							}
							break;
						case SerializedPropertyType.Boolean:
							{
								var element = property.boolValues.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.boolValue = EditorGUILayout.Toggle(v.DisplayName, element.boolValue);
							}
							break;
						case SerializedPropertyType.Color:
							{
								var element = property.colorValues.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.colorValue = EditorGUILayout.ColorField(v.DisplayName, element.colorValue);
							}
							break;
						case SerializedPropertyType.Float:
							{
								var element = property.floatValues.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.floatValue = EditorGUILayout.FloatField(v.DisplayName, element.floatValue);
							}
							break;
						case SerializedPropertyType.Integer:
							{
								var element = property.intValues.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.intValue = EditorGUILayout.IntField(v.DisplayName, element.intValue);
							}
							break;
						case SerializedPropertyType.Enum:
							{
								var element = property.intValues.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.intValue = EditorGUILayout.IntPopup(v.DisplayName, element.intValue, v.PopupDisplayName, v.PopupValue);
							}
							break;
						case SerializedPropertyType.Vector2:
							{
								var element = property.vector4Values.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.vector4Value = EditorGUILayout.Vector2Field(v.DisplayName, element.vector4Value);
							}
							break;
						case SerializedPropertyType.Vector3:
							{
								var element = property.vector4Values.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.vector4Value = EditorGUILayout.Vector3Field(v.DisplayName, element.vector4Value);
							}
							break;
						case SerializedPropertyType.Vector4:
							{
								var element = property.vector4Values.GetArrayElementAtIndex(v.FieldIndex);
								if (element.hasMultipleDifferentValues) EditorGUI.showMixedValue = true;
								element.vector4Value = EditorGUILayout.Vector4Field(v.DisplayName, element.vector4Value);
							}
							break;
					}
					EditorGUI.showMixedValue = showMixedValue;
				}

				SkinnerEditorUtility.MapRuntimePropertyFromEditorProperty(validateProperty, property);
				userLogic.ValidateProperty(validateProperty);
				SkinnerEditorUtility.MapRuntimePropertyFromEditorProperty(property, validateProperty);
			}
		}

		/// <summary>
		/// インスペクター用表示データを生成して初期化する
		/// </summary>
		/// <param name="userLogic">userLogic</param>
		/// <returns></returns>
		private bool CreateDisplayData(UserLogic userLogic)
		{
			if (!userLogic) return false;

			// ここでキャッシュしてクリエイトを抑制
			if (currentUserLogic == userLogic)
			{
				return true;
			}
			currentUserLogic = userLogic;

			// すでに存在したらキャッシュから取ってくる
			var userLogicType = userLogic.GetType();
			if (m_CachedVariableDisplayDatas.ContainsKey(userLogicType))
			{
				this.userLogicVariableDisplayDatas = m_CachedVariableDisplayDatas[userLogicType];
			}

			var userLogicVariableDisplayDatas = new List<UserLogicVariableDisplayData>();
			objectReferenceArrayCount = 0;
			boolArrayCount = 0;
			colorArrayCount = 0;
			floatArrayCount = 0;
			intArrayCount = 0;
			vector4ArrayCount = 0;
			stringArrayCount = 0;
			foreach (var v in userLogic.variables)
			{
				bool isUnCorrect = false;
				var data = new UserLogicVariableDisplayData();
				if (v.FieldType == typeof(Object) || v.FieldType.IsSubclassOf(typeof(Object)))
				{
					data.PropertyType = SerializedPropertyType.ObjectReference;
					data.FieldIndex = objectReferenceArrayCount;
					objectReferenceArrayCount++;
				}
				else if (v.FieldType == typeof(bool))
				{
					data.PropertyType = SerializedPropertyType.Boolean;
					data.FieldIndex = boolArrayCount;
					boolArrayCount++;
				}
				else if (v.FieldType == typeof(Color) || v.FieldType == typeof(Color32))
				{
					data.PropertyType = SerializedPropertyType.Color;
					data.FieldIndex = colorArrayCount;
					colorArrayCount++;
				}
				else if (v.FieldType == typeof(float))
				{
					data.PropertyType = SerializedPropertyType.Float;
					data.FieldIndex = floatArrayCount;
					floatArrayCount++;
				}
				else if (v.FieldType == typeof(int))
				{
					data.PropertyType = SerializedPropertyType.Integer;
					data.FieldIndex = intArrayCount;
					intArrayCount++;
				}
				else if (v.FieldType.IsEnum)
				{
					data.PropertyType = SerializedPropertyType.Enum;
					data.PopupDisplayName = v.FieldType.GetEnumNames().Select(n => new GUIContent(n)).ToArray();
					data.PopupValue = v.FieldType.GetEnumValues().Cast<int>().ToArray();
					data.FieldIndex = intArrayCount;
					intArrayCount++;
				}
				else if (v.FieldType == typeof(Vector2))
				{
					data.PropertyType = SerializedPropertyType.Vector2;
					data.FieldIndex = vector4ArrayCount;
					vector4ArrayCount++;
				}
				else if (v.FieldType == typeof(Vector3))
				{
					data.PropertyType = SerializedPropertyType.Vector3;
					data.FieldIndex = vector4ArrayCount;
					vector4ArrayCount++;
				}
				else if (v.FieldType == typeof(Vector4))
				{
					data.PropertyType = SerializedPropertyType.Vector4;
					data.FieldIndex = vector4ArrayCount;
					vector4ArrayCount++;
				}
				else if (v.FieldType == typeof(char))
				{
					data.PropertyType = SerializedPropertyType.Character;
					data.FieldIndex = stringArrayCount;
					stringArrayCount++;
				}
				else if (v.FieldType == typeof(string))
				{
					data.PropertyType = SerializedPropertyType.String;
					data.FieldIndex = stringArrayCount;
					stringArrayCount++;
				}
				else
				{
					isUnCorrect = true;
				}
				if (!isUnCorrect)
				{
					data.DisplayName = new GUIContent(v.FieldDisplayName);
					data.VariableData = v;
					userLogicVariableDisplayDatas.Add(data);
				}
			}
			m_CachedVariableDisplayDatas.Add(userLogicType, userLogicVariableDisplayDatas);
			this.userLogicVariableDisplayDatas = userLogicVariableDisplayDatas;
			return true;
		}

	}

}