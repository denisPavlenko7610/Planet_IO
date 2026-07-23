using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BayatGames.Utilities.Editor
{

	/// <summary>
	/// Scene switcher window, an editor window for switching between scenes.
	/// </summary>
	public class SceneSwitcherWindow : EditorWindow
	{

		private const float MinWindowWidth = 250f;
		private const float MinWindowHeight = 200f;

		public enum ScenesSource
		{
			Assets,
			BuildSettings
		}

		protected Vector2 scrollPosition;
		protected ScenesSource scenesSource = ScenesSource.Assets;
		protected OpenSceneMode openSceneMode = OpenSceneMode.Single;
		protected int selectedTab = 0;
		protected string[] tabs = new string[]
		{
			"Scenes",
			"Settings"
		};

		[MenuItem("Tools/Scene Switcher")]
		public static void Init()
		{
			SceneSwitcherWindow window = GetWindow<SceneSwitcherWindow>("Scene Switcher");
			window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
			window.Show();
		}

		protected virtual void OnEnable()
		{
			scenesSource = (ScenesSource)EditorPrefs.GetInt("SceneSwitcher.scenesSource", (int)ScenesSource.Assets);
			openSceneMode = (OpenSceneMode)EditorPrefs.GetInt(
				"SceneSwitcher.openSceneMode",
				(int)OpenSceneMode.Single
			);
		}

		protected virtual void OnDisable()
		{
			EditorPrefs.SetInt("SceneSwitcher.scenesSource", (int)scenesSource);
			EditorPrefs.SetInt("SceneSwitcher.openSceneMode", (int)openSceneMode);
		}

		protected virtual void OnGUI()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			selectedTab = GUILayout.Toolbar(selectedTab, tabs, EditorStyles.toolbarButton);
			EditorGUILayout.EndHorizontal();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			EditorGUILayout.BeginVertical();
			switch (selectedTab)
			{
				case 0:
					ScenesTabGUI();
					break;

				case 1:
					SettingsTabGUI();
					break;
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			GUILayout.Label("Made with ❤️ by Bayat Games", EditorStyles.centeredGreyMiniLabel);
		}

		protected virtual void SettingsTabGUI()
		{
			scenesSource = (ScenesSource)EditorGUILayout.EnumPopup("Scenes Source", scenesSource);
			openSceneMode = (OpenSceneMode)EditorGUILayout.EnumPopup("Open Scene Mode", openSceneMode);
		}

		protected virtual void ScenesTabGUI()
		{
			List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
			string[] guids = AssetDatabase.FindAssets("t:Scene");
			if (guids.Length == 0)
			{
				GUILayout.Label("No Scenes Found", EditorStyles.centeredGreyMiniLabel);
				GUILayout.Label("Create New Scenes", EditorStyles.centeredGreyMiniLabel);
				GUILayout.Label("And Switch Between them here", EditorStyles.centeredGreyMiniLabel);
			}
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
				EditorBuildSettingsScene buildScene = buildScenes.Find((editorBuildScene) =>
					{
						return editorBuildScene.path == path;
					}
				);
				Scene scene = SceneManager.GetSceneByPath(path);
				bool isOpen = scene.IsValid() && scene.isLoaded;
				EditorGUI.BeginDisabledGroup(isOpen);
				if (scenesSource == ScenesSource.Assets)
				{
					if (GUILayout.Button(sceneAsset.name))
					{
						Open(path);
					}
				}
				else
				{
					if (buildScene != null)
					{
						if (GUILayout.Button(sceneAsset.name))
						{
							Open(path);
						}
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			if (GUILayout.Button("Create New Scene"))
			{
				Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
				EditorSceneManager.SaveScene(newScene);
			}
		}

		public virtual void Open(string path)
		{
			if (EditorSceneManager.EnsureUntitledSceneHasBeenSaved(
					"You don't have saved the Untitled Scene, Do you want to leave?"
				))
			{
				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
				EditorSceneManager.OpenScene(path, openSceneMode);
			}
		}

	}

}