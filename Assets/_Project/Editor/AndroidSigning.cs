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

        private const string RepositoryKeystorePath = "Keystore/planetio-upload.keystore";
        private const string DefaultKeyAlias = "planetio-upload";

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

            string keystorePassword = Environment.GetEnvironmentVariable(KeystorePasswordVariable);
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
                return $"Android signing is not configured. Set {KeystorePasswordVariable} (the keystore is read from " +
                       $"{RepositoryKeystorePath}), or use Player Settings > Publishing Settings.";
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
                return "Keystore passwords are empty. Enter them in Player Settings or set the PLANETIO_* variables.";
            }

            return string.Empty;
        }
    }
}
#endif
