#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Editor
{
	public sealed class AndroidSigningPasswordWindow : EditorWindow
	{
		private string _password = string.Empty;

		[MenuItem("Planet IO/Android/Set signing password")]
		public static void Open()
		{
			var window = GetWindow<AndroidSigningPasswordWindow>(true, "Android signing password", true);
			window.minSize = new Vector2(380f, 160f);
			window.maxSize = new Vector2(380f, 160f);
			window.Show();
		}

		public static void Clear()
		{
			AndroidSigning.ClearStoredPassword();
			Debug.Log("Stored Android signing password cleared.");
		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox(
				AndroidSigning.HasStoredPassword
					? "A password is stored in this machine's Unity EditorPrefs. Saving a new one replaces it."
					: "The password is stored in this machine's Unity EditorPrefs, outside the repository.",
				MessageType.Info
			);

			_password = EditorGUILayout.PasswordField("Keystore password", _password);

			using (new EditorGUI.DisabledScope(!AndroidSigning.HasStoredPassword))
			{
				if (GUILayout.Button("Clear stored password"))
				{
					Clear();
					_password = string.Empty;
				}
			}

			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_password)))
			{
				if (GUILayout.Button("Save"))
				{
					AndroidSigning.StorePassword(_password);
					_password = string.Empty;
					Close();
				}
			}
		}
	}
}
#endif