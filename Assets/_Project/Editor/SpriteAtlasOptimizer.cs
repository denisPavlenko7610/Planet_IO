#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace PlanetIO.Editor
{
    public static class SpriteAtlasOptimizer
    {
        private const string AtlasDirectory = "Assets/Atlases";

        [MenuItem("Planet IO/Assets/Rebuild sprite atlases")]
        public static void RebuildSpriteAtlases()
        {
            EnsureDirectory();
            ConfigureAtlas(
                $"{AtlasDirectory}/Gameplay.spriteatlas",
                "Assets/Sprites/Planets",
                2048);

            ConfigureAtlas(
                $"{AtlasDirectory}/Controls.spriteatlas",
                "Assets/Sprites/Buttons",
                1024);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Planet IO sprite atlases rebuilt.");
        }

        private static void ConfigureAtlas(
            string atlasPath,
            string sourceFolderPath,
            int maximumTextureSize)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            bool isNewAtlas = atlas == null;
            if (isNewAtlas)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();
            packingSettings.enableRotation = false;
            packingSettings.enableTightPacking = true;
            packingSettings.padding = 4;
            atlas.SetPackingSettings(packingSettings);

            SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings();
            textureSettings.generateMipMaps = false;
            textureSettings.readable = false;
            textureSettings.sRGB = true;
            textureSettings.filterMode = FilterMode.Bilinear;
            atlas.SetTextureSettings(textureSettings);

            TextureImporterPlatformSettings androidSettings = atlas.GetPlatformSettings("Android");
            androidSettings.overridden = true;
            androidSettings.maxTextureSize = maximumTextureSize;
            androidSettings.format = TextureImporterFormat.ETC2_RGBA8;
            androidSettings.compressionQuality = 50;
            atlas.SetPlatformSettings(androidSettings);

            if (isNewAtlas)
            {
                Object sourceFolder = AssetDatabase.LoadAssetAtPath<Object>(sourceFolderPath);
                if (sourceFolder == null)
                {
                    throw new DirectoryNotFoundException(sourceFolderPath);
                }

                atlas.Add(new[] { sourceFolder });
            }

            SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
            EditorUtility.SetDirty(atlas);
        }

        private static void EnsureDirectory()
        {
            if (!AssetDatabase.IsValidFolder(AtlasDirectory))
            {
                AssetDatabase.CreateFolder("Assets", "Atlases");
            }
        }
    }
}
#endif
