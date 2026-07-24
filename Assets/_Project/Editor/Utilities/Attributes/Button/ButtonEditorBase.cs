using System;
using System.Collections.Generic;
using System.Reflection;
using PlanetIO.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace TLN.Editor.Validation
{
	public abstract class ButtonEditorBase : UnityEditor.Editor
	{
		private const BindingFlags MethodFlags =
			BindingFlags.Instance |
			BindingFlags.Public |
			BindingFlags.NonPublic;

		private static readonly Dictionary<Type, List<ButtonMethod>> ButtonMethodsByType = new();

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			DrawButtons();
		}

		private void DrawButtons()
		{
			List<ButtonMethod> buttonMethods = GetButtonMethods(target.GetType());

			if (buttonMethods.Count == 0)
			{
				return;
			}

			EditorGUILayout.Space();

			for (int i = 0; i < buttonMethods.Count; i++)
			{
				ButtonMethod buttonMethod = buttonMethods[i];
				MethodInfo method = buttonMethod.Method;

				if (!IsValidButtonMethod(method))
				{
					DrawInvalidButtonWarning(method);
					continue;
				}

				string label = string.IsNullOrWhiteSpace(buttonMethod.Attribute.Label)
					? ObjectNames.NicifyVariableName(method.Name)
					: buttonMethod.Attribute.Label;

				if (GUILayout.Button(label))
				{
					InvokeForTargets(method);
				}
			}
		}

		private static List<ButtonMethod> GetButtonMethods(Type targetType)
		{
			if (ButtonMethodsByType.TryGetValue(targetType, out List<ButtonMethod> cachedMethods))
			{
				return cachedMethods;
			}

			MethodInfo[] methods = targetType.GetMethods(MethodFlags);
			List<ButtonMethod> buttonMethods = new();

			for (int i = 0; i < methods.Length; i++)
			{
				MethodInfo method = methods[i];
				ButtonAttribute attribute = method.GetCustomAttribute<ButtonAttribute>();

				if (attribute == null)
				{
					continue;
				}

				buttonMethods.Add(new ButtonMethod(method, attribute));
			}

			ButtonMethodsByType.Add(targetType, buttonMethods);

			return buttonMethods;
		}

		private static bool IsValidButtonMethod(MethodInfo method)
		{
			return method.GetParameters().Length == 0;
		}

		private static void DrawInvalidButtonWarning(MethodInfo method)
		{
			EditorGUILayout.HelpBox(
				$"[Button] method '{method.Name}' must be parameterless.",
				MessageType.Warning
			);
		}

		private void InvokeForTargets(MethodInfo method)
		{
			for (int i = 0; i < targets.Length; i++)
			{
				UnityEngine.Object currentTarget = targets[i];

				Undo.RecordObject(currentTarget, $"Invoke {method.Name}");

				try
				{
					object result = method.Invoke(currentTarget, null);

					if (method.ReturnType != typeof(void))
					{
						Debug.Log(
							$"[Button] {currentTarget.name}.{method.Name} returned: {FormatResult(result)}",
							currentTarget
						);
					}

					EditorUtility.SetDirty(currentTarget);
				}
				catch (TargetInvocationException exception)
				{
					Debug.LogException(exception.InnerException ?? exception, currentTarget);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, currentTarget);
				}
			}
		}

		private static string FormatResult(object result)
		{
			return result == null ? "null" : result.ToString();
		}

		private readonly struct ButtonMethod
		{
			public readonly MethodInfo Method;
			public readonly ButtonAttribute Attribute;

			public ButtonMethod(MethodInfo method, ButtonAttribute attribute)
			{
				Method = method;
				Attribute = attribute;
			}
		}
	}
}
