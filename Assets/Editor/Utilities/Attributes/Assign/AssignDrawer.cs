#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanetIO.Core.Attributes
{
	[CustomPropertyDrawer(typeof(AssignAttribute))]
	public sealed class AssignDrawer : PropertyDrawer
	{
		private readonly struct SearchKey : IEquatable<SearchKey>
		{
			private readonly EntityId _targetId;
			private readonly string _propertyPath;

			public SearchKey(Object target, string propertyPath)
			{
				_targetId = target.GetEntityId();
				_propertyPath = propertyPath;
			}

			public bool Equals(SearchKey other)
			{
				return _targetId == other._targetId &&
				       _propertyPath == other._propertyPath;
			}

			public override bool Equals(object obj)
			{
				return obj is SearchKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(_targetId, _propertyPath);
			}
		}

		private static readonly HashSet<SearchKey> Searched = new();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			TryAutoAssign(property);

			EditorGUI.PropertyField(position, property, label, true);
		}

		private void TryAutoAssign(SerializedProperty property)
		{
			if (Event.current.type != EventType.Layout)
			{
				return;
			}

			if (property.propertyType != SerializedPropertyType.ObjectReference)
			{
				return;
			}

			if (property.objectReferenceValue != null)
			{
				return;
			}

			SerializedObject serializedObject = property.serializedObject;

			if (serializedObject.isEditingMultipleObjects)
			{
				return;
			}

			if (serializedObject.targetObject is not Component source)
			{
				return;
			}

			Type targetType = fieldInfo.FieldType;

			if (!typeof(Component).IsAssignableFrom(targetType))
			{
				return;
			}

			SearchKey key = new SearchKey(source, property.propertyPath);

			if (!Searched.Add(key))
			{
				return;
			}

			AssignAttribute assignAttribute = (AssignAttribute)attribute;

			Object found = Find(source, targetType, assignAttribute.Mode);

			if (found == null)
			{
				return;
			}

			property.objectReferenceValue = found;
			serializedObject.ApplyModifiedProperties();
		}

		private static Object Find(Component source, Type targetType, AssignMode mode)
		{
			return mode switch
			{
				AssignMode.Local => source.GetComponent(targetType),

				AssignMode.Parent => source.transform.parent != null
					? source.transform.parent.GetComponentInParent(targetType, true)
					: null,

				AssignMode.Children => source.GetComponentInChildren(targetType, true),

				AssignMode.Scene => Object.FindAnyObjectByType(targetType, FindObjectsInactive.Include),

				_ => null
			};
		}
	}
}

#endif
