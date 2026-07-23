using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Editor
{
    internal static class NetworkPrefabHashValidator
    {
        private const string GlobalObjectIdentifierHashProperty =
            "GlobalObjectIdHash";

        private static readonly Regex DirectHashPattern = new(
            @"^\s*GlobalObjectIdHash:\s*(\d+)\s*$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex VariantHashPattern = new(
            @"propertyPath:\s*GlobalObjectIdHash\s*\r?\n\s*value:\s*(\d+)",
            RegexOptions.Compiled);

        private static readonly MethodInfo HashMethod =
            typeof(NetworkObject).Assembly
                .GetType("Unity.Netcode.XXHash")
                ?.GetMethod(
                    "Hash32",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null);

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += RepairInvalidNetworkPrefabHashes;
        }

        [MenuItem("Planet IO/Validate Network Prefab Hashes %#h")]
        internal static void RepairInvalidNetworkPrefabHashes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Debug.Log("Validating NetworkObject prefab hashes.");

            if (HashMethod == null)
            {
                Debug.LogError(
                    "Unable to locate the Netcode XXHash implementation.");
                return;
            }

            int repairedPrefabCount = 0;
            int savedPrefabCount = 0;
            int networkPrefabCount = 0;
            Dictionary<uint, string> prefabPathsByHash = new();

            foreach (string assetIdentifier in AssetDatabase.FindAssets(
                         "t:Prefab",
                         new[] { "Assets/Prefabs" }))
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(assetIdentifier);
                GameObject networkPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                NetworkObject networkObject =
                    networkPrefab != null
                        ? networkPrefab.GetComponent<NetworkObject>()
                        : null;

                if (networkObject == null)
                {
                    continue;
                }

                networkPrefabCount++;
                uint expectedHash = CalculateExpectedHash(networkObject);
                SerializedObject serializedNetworkObject =
                    new SerializedObject(networkObject);
                SerializedProperty hashProperty =
                    serializedNetworkObject.FindProperty(
                        GlobalObjectIdentifierHashProperty);

                if (hashProperty == null)
                {
                    Debug.LogError(
                        $"Unable to inspect NetworkObject hash for {prefabPath}.",
                        networkPrefab);
                    continue;
                }

                bool hashIsPersisted =
                    IsExpectedHashPersisted(prefabPath, expectedHash);
                if (!hashIsPersisted)
                {
                    hashProperty.longValue = expectedHash;
                    serializedNetworkObject.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(
                        networkObject);
                    repairedPrefabCount++;
                    EditorUtility.SetDirty(networkObject);
                    PrefabUtility.SavePrefabAsset(networkPrefab);
                    savedPrefabCount++;
                }

                if (prefabPathsByHash.TryGetValue(
                        expectedHash,
                        out string existingPrefabPath))
                {
                    Debug.LogError(
                        $"Network prefab hash collision: {existingPrefabPath} and " +
                        $"{prefabPath} both use {expectedHash}.");
                }
                else
                {
                    prefabPathsByHash.Add(expectedHash, prefabPath);
                }
            }

            if (savedPrefabCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                $"Validated {networkPrefabCount} network prefabs. " +
                $"Repaired {repairedPrefabCount} and saved " +
                $"{savedPrefabCount} NetworkObject hashes.");
        }

        private static uint CalculateExpectedHash(NetworkObject networkObject)
        {
            string globalObjectIdentifier =
                GlobalObjectId.GetGlobalObjectIdSlow(networkObject).ToString();

            return (uint)HashMethod.Invoke(
                null,
                new object[] { globalObjectIdentifier });
        }

        private static bool IsExpectedHashPersisted(
            string prefabPath,
            uint expectedHash)
        {
            string serializedPrefab = File.ReadAllText(prefabPath);
            Match hashMatch = DirectHashPattern.Match(serializedPrefab);

            if (!hashMatch.Success)
            {
                hashMatch = VariantHashPattern.Match(serializedPrefab);
            }

            return hashMatch.Success &&
                   uint.TryParse(
                       hashMatch.Groups[1].Value,
                       NumberStyles.None,
                       CultureInfo.InvariantCulture,
                       out uint persistedHash) &&
                   persistedHash == expectedHash;
        }
    }
}
