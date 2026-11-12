#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace PlanetIO.Editor
{
    public static class AndroidAppIcons
    {
        private const string IconDirectory = "Assets/_Project/Art/AppIcon/";
        private const string BackgroundPath = IconDirectory + "AppIcon_Background.png";
        private const string ForegroundPath = IconDirectory + "AppIcon_Foreground.png";
        private const string MonochromePath = IconDirectory + "AppIcon_Monochrome.png";
        private const string DefaultIconPath = IconDirectory + "AppIcon_Legacy.png";

        [MenuItem("Planet IO/Android/Apply app icon")]
        public static void Apply()
        {
            Texture2D background = Load(BackgroundPath);
            Texture2D foreground = Load(ForegroundPath);
            Texture2D monochrome = Load(MonochromePath);
            Texture2D defaultIcon = Load(DefaultIconPath);

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { defaultIcon }, IconKind.Application);
            AssignLargest(AndroidPlatformIconKind.Adaptive, background, foreground, monochrome);
            AssetDatabase.SaveAssets();
        }

        public static string GetValidationError()
        {
            if (!IsAssigned(AndroidPlatformIconKind.Adaptive))
            {
                return "Android adaptive app icon is not assigned. Run Planet IO/Android/Apply app icon.";
            }

            Texture2D[] defaultIcons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
            if (defaultIcons.All(texture => texture == null) ||
                AssetDatabase.GetAssetPath(defaultIcons.First(texture => texture != null)) != DefaultIconPath)
            {
                return "Default app icon is not the Planet IO icon. Run Planet IO/Android/Apply app icon.";
            }

            return string.Empty;
        }

        private static bool IsAssigned(PlatformIconKind kind)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            return icons.Any(icon => icon.GetTextures().Any(texture => texture != null));
        }

        private static void AssignLargest(PlatformIconKind kind, params Texture2D[] textures)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (PlatformIcon icon in icons)
            {
                icon.SetTextures(new Texture2D[icon.GetTextures().Length]);
            }

            PlatformIcon largest = icons.OrderByDescending(icon => icon.width).First();
            largest.SetTextures(textures);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }

        private static Texture2D Load(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.textureType != TextureImporterType.Default || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new BuildFailedException($"App icon texture is missing: {path}");
            }

            return texture;
        }
    }
}
#endif
