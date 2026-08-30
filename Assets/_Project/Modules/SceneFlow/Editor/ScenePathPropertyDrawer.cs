using UnityEditor;
using UnityEngine;

namespace UnityTemplates.SceneFlow.Editor
{
	[CustomPropertyDrawer(typeof(ScenePathAttribute))]
	public sealed class ScenePathPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.LabelField(position, label.text, "ScenePath requires a string field.");
				return;
			}

			SceneAsset current = string.IsNullOrEmpty(property.stringValue)
				? null
				: AssetDatabase.LoadAssetAtPath<SceneAsset>(property.stringValue);

			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();

			SceneAsset selected = (SceneAsset)EditorGUI.ObjectField(
				position,
				label,
				current,
				typeof(SceneAsset),
				allowSceneObjects: false
			);

			if (EditorGUI.EndChangeCheck())
			{
				property.stringValue = selected == null
					? string.Empty
					: AssetDatabase.GetAssetPath(selected);
			}

			EditorGUI.EndProperty();
		}
	}
}
