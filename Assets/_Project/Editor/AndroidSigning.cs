#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

namespace PlanetIO.Editor
{
    public static class AndroidSigning
    {
        public const string KeystorePathVariable = "PLANETIO_KEYSTORE_PATH";
        public const string KeystorePasswordVariable = "PLANETIO_KEYSTORE_PASS";
        public const string KeyAliasVariable = "PLANETIO_KEYALIAS_NAME";
        public const string KeyAliasPasswordVariable = "PLANETIO_KEYALIAS_PASS";

        public const string PasswordPreferenceKey = "PlanetIO.Android.KeystorePass";

        private const string RepositoryKeystorePath = "Keystore/planetio-upload.keystore";
        private const string DefaultKeyAlias = "planetio-upload";

        public static bool HasStoredPassword => EditorPrefs.HasKey(PasswordPreferenceKey);

        public static void StorePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return;
            }

            EditorPrefs.SetString(PasswordPreferenceKey, password);
        }

        public static void ClearStoredPassword()
        {
            EditorPrefs.DeleteKey(PasswordPreferenceKey);
        }

        public static string ResolveKeystorePassword(string environmentValue, string storedValue)
        {
            return string.IsNullOrEmpty(environmentValue) ? storedValue ?? string.Empty : environmentValue;
        }

        public static void ApplyFromEnvironment()
        {
            string path = Environment.GetEnvironmentVariable(KeystorePathVariable);
            if (string.IsNullOrEmpty(path) && File.Exists(RepositoryKeystorePath))
            {
                path = RepositoryKeystorePath;
            }

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string keystorePassword = ResolveKeystorePassword(
                Environment.GetEnvironmentVariable(KeystorePasswordVariable),
                EditorPrefs.GetString(PasswordPreferenceKey, string.Empty));
            string alias = Environment.GetEnvironmentVariable(KeyAliasVariable);
            string aliasPassword = Environment.GetEnvironmentVariable(KeyAliasPasswordVariable);

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = string.IsNullOrEmpty(alias) ? DefaultKeyAlias : alias;
            PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(aliasPassword) ? keystorePassword : aliasPassword;
        }

        public static string GetValidationError()
        {
            if (!PlayerSettings.Android.useCustomKeystore)
            {
                return $"Android signing is not configured. Use Planet IO > Android > Set signing password (the keystore " +
                       $"is read from {RepositoryKeystorePath}), set {KeystorePasswordVariable}, or use Player Settings > " +
                       "Publishing Settings.";
            }

            if (!File.Exists(PlayerSettings.Android.keystoreName))
            {
                return $"Keystore file not found: {PlayerSettings.Android.keystoreName}";
            }

            if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
            {
                return "Keystore alias is empty.";
            }

            if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) ||
                string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            {
                return "Keystore passwords are empty. Use Planet IO > Android > Set signing password, enter them in " +
                       "Player Settings, or set the PLANETIO_* variables.";
            }

            return string.Empty;
        }
    }
}
#endif
