using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTemplates.SceneFlow
{
	[CreateAssetMenu(fileName = "SceneCatalog", menuName = "Unity Templates/Scene Flow/Scene Catalog")]
	public sealed class SceneCatalog : ScriptableObject
	{
		[SerializeField] private List<SceneEntry> _scenes = new();

		private Dictionary<string, string> _pathById;

		public string GetPath(string sceneId)
		{
			if (string.IsNullOrWhiteSpace(sceneId))
			{
				throw new ArgumentException("A scene ID is required.", nameof(sceneId));
			}

			_pathById ??= BuildLookup();

			string id = sceneId.Trim();
			return _pathById.TryGetValue(id, out string path)
				? path
				: throw new KeyNotFoundException($"Scene '{id}' is not registered.");
		}

		[ContextMenu("Validate")]
		public void ThrowIfInvalid()
		{
			HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
			HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);

			foreach (SceneEntry scene in _scenes)
			{
				if (scene == null)
				{
					throw new InvalidOperationException("SceneCatalog contains an empty entry.");
				}

				if (string.IsNullOrWhiteSpace(scene.Id))
				{
					throw new InvalidOperationException("SceneCatalog contains an empty scene ID.");
				}

				if (string.IsNullOrWhiteSpace(scene.Path))
				{
					throw new InvalidOperationException($"Scene '{scene.Id}' has no scene assigned.");
				}

				if (!ids.Add(scene.Id))
				{
					throw new InvalidOperationException($"Scene ID '{scene.Id}' is registered more than once.");
				}

				if (!paths.Add(scene.Path))
				{
					throw new InvalidOperationException($"Scene '{scene.Path}' is registered more than once.");
				}
			}
		}

		private Dictionary<string, string> BuildLookup()
		{
			ThrowIfInvalid();

			Dictionary<string, string> lookup = new(_scenes.Count, StringComparer.OrdinalIgnoreCase);

			foreach (SceneEntry scene in _scenes)
			{
				lookup.Add(scene.Id, scene.Path);
			}

			return lookup;
		}

		private void OnEnable()
		{
			_pathById = null;
		}

		private void OnValidate()
		{
			_pathById = null;
		}

		[Serializable]
		private sealed class SceneEntry
		{
			[SerializeField] private string _id;
			[SerializeField, ScenePath] private string _path;

			public string Id => _id?.Trim() ?? string.Empty;

			public string Path => _path?.Trim() ?? string.Empty;
		}
	}
}
