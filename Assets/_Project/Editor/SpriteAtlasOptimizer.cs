#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace PlanetIO.Editor
{
	public static class SpriteAtlasOptimizer
	{
		private const string AtlasDirectory = "Assets/_Project/Atlases";

		public static void RebuildSpriteAtlases()
		{
			EnsureDirectory();
			EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
			ConfigureAtlas(
				$"{AtlasDirectory}/Gameplay.spriteatlasv2",
				new[] { "Assets/_Project/Sprites/Planets", "Assets/_Project/Sprites/Foods" },
				2048,
				true
			);

			ConfigureAtlas(
				$"{AtlasDirectory}/Controls.spriteatlasv2",
				new[] { "Assets/_Project/Sprites/Buttons" },
				1024,
				false
			);

			AssetDatabase.SaveAssets();
			Debug.Log($"Planet IO sprite atlases rebuilt. Packer mode: {EditorSettings.spritePackerMode}.");
		}

		private static void ConfigureAtlas(
			string atlasPath,
			string[] sourceFolderPaths,
			int maximumTextureSize,
			bool tightPacking
		)
		{
			SpriteAtlasAsset atlas;
			if (File.Exists(atlasPath))
			{
				atlas = SpriteAtlasAsset.Load(atlasPath);
			}
			else
			{
				atlas = new SpriteAtlasAsset();
			}

			// Preserve custom packables while adding folders introduced after atlas creation.
			SpriteAtlas imported = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
			HashSet<Object> packables = imported != null
				? new HashSet<Object>(imported.GetPackables())
				: new HashSet<Object>();
			string legacyPath = Path.ChangeExtension(atlasPath, ".spriteatlas");
			SpriteAtlas legacy = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(legacyPath);
			if (legacy != null)
			{
				foreach (Object packable in legacy.GetPackables())
				{
					if (packables.Add(packable))
					{
						atlas.Add(new[] { packable });
					}
				}
			}

			foreach (string sourceFolderPath in sourceFolderPaths)
			{
				Object sourceFolder = AssetDatabase.LoadAssetAtPath<Object>(sourceFolderPath);
				if (sourceFolder == null)
				{
					throw new DirectoryNotFoundException(sourceFolderPath);
				}

				if (packables.Add(sourceFolder))
				{
					atlas.Add(new[] { sourceFolder });
				}
			}

			SpriteAtlasAsset.Save(atlas, atlasPath);
			AssetDatabase.ImportAsset(atlasPath);
			var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(atlasPath);

			SpriteAtlasPackingSettings packingSettings = importer.packingSettings;
			packingSettings.enableRotation = false;
			packingSettings.enableTightPacking = tightPacking;
			packingSettings.padding = 4;
			importer.packingSettings = packingSettings;

			SpriteAtlasTextureSettings textureSettings = importer.textureSettings;
			textureSettings.generateMipMaps = false;
			textureSettings.readable = false;
			textureSettings.sRGB = true;
			textureSettings.filterMode = FilterMode.Bilinear;
			importer.textureSettings = textureSettings;

			TextureImporterPlatformSettings androidSettings = importer.GetPlatformSettings("Android");
			androidSettings.overridden = true;
			androidSettings.maxTextureSize = maximumTextureSize;
			androidSettings.format = TextureImporterFormat.ETC2_RGBA8;
			androidSettings.compressionQuality = 50;
			importer.SetPlatformSettings(androidSettings);
			importer.includeInBuild = true;
			importer.SaveAndReimport();

			// Retire the V1 asset only after the replacement has imported successfully.
			// These built-in atlases are discovered automatically and have no direct asset references.
			if (legacy != null && !AssetDatabase.DeleteAsset(legacyPath))
			{
				throw new IOException($"Could not remove replaced atlas: {legacyPath}");
			}
		}

		private static void EnsureDirectory()
		{
			if (!AssetDatabase.IsValidFolder(AtlasDirectory))
			{
				AssetDatabase.CreateFolder("Assets/_Project", "Atlases");
			}
		}
	}

	public sealed class SpriteAtlasBuildProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => -100;

		public void OnPreprocessBuild(BuildReport report)
		{
			SpriteAtlasOptimizer.RebuildSpriteAtlases();
		}
	}
}
#endif