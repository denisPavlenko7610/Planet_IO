using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TLN.Editor.Utilities
{
	public static class RuntimeCodeDumpMenu
	{
		private const string MenuPath = "Tools/CreateCodeFile";
		private const string SourceFolder = "Assets/_Project/Runtime";
		private const string LegacyOutputFile = "Assets/RuntimeCodeDump.txt";
		private const string OutputFileNamePrefix = "RuntimeCodeDump_";
		private const string OutputFileExtension = ".txt";
		private const int MaxEstimatedTokensPerFile = 900000;
		private const int EstimatedCharsPerToken = 4;
		private const bool CompactOutput = true;
		private static readonly string[] SupportedExtensions =
		{
			".cs",
			".uxml",
			".uss",
			".ucss"
		};

		[MenuItem(MenuPath)]
		private static void CreateCodeFile()
		{
			string projectRoot = GetProjectRoot();
			if (string.IsNullOrEmpty(projectRoot))
			{
				Debug.LogError("Cannot resolve Unity project root.");
				return;
			}

			string sourcePath = Path.Combine(projectRoot, SourceFolder);
			if (!Directory.Exists(sourcePath))
			{
				Debug.LogError($"Runtime source folder was not found: {SourceFolder}");
				return;
			}

			string[] files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
				.Where(IsSupportedCodeFile)
				.OrderBy(ToAssetPath, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			ClearPreviousDumpFiles();

			List<string> createdFiles = new();
			int partIndex = 1;
			int filesInCurrentPart = 0;
			StringBuilder builder = CreatePartBuilder(partIndex, files.Length);
			int currentEstimatedTokens = EstimateTokenCount(builder.ToString());

			foreach (string file in files)
			{
				string assetPath = ToAssetPath(file);
				string content = File.ReadAllText(file);
				string block = BuildFileBlock(assetPath, CompactOutput ? CompactContent(content) : content);
				int blockEstimatedTokens = EstimateTokenCount(block);

				if (filesInCurrentPart > 0 && currentEstimatedTokens + blockEstimatedTokens > MaxEstimatedTokensPerFile)
				{
					createdFiles.Add(WritePartFile(projectRoot, partIndex, builder));
					partIndex++;
					filesInCurrentPart = 0;
					builder = CreatePartBuilder(partIndex, files.Length);
					currentEstimatedTokens = EstimateTokenCount(builder.ToString());
				}

				if (blockEstimatedTokens > MaxEstimatedTokensPerFile)
				{
					Debug.LogWarning(
						$"Single file exceeds token limit estimate and will be written as one block:" +
						$" {assetPath} ({blockEstimatedTokens} tokens)"
					);
				}

				builder.Append(block);
				currentEstimatedTokens += blockEstimatedTokens;
				filesInCurrentPart++;
			}

			if (filesInCurrentPart > 0 || files.Length == 0)
			{
				createdFiles.Add(WritePartFile(projectRoot, partIndex, builder));
			}

			AssetDatabase.Refresh();

			Debug.Log($"Created runtime code dump: {createdFiles.Count} file(s), {files.Length} source file(s)");
		}

		private static StringBuilder CreatePartBuilder(int partIndex, int totalFiles)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"// Source folder: {SourceFolder}");
			builder.AppendLine($"// Supported extensions: {string.Join(", ", SupportedExtensions)}");
			builder.AppendLine($"// Part: {partIndex}");
			builder.AppendLine($"// Total source files: {totalFiles}");
			builder.AppendLine($"// Max estimated tokens per dump file: {MaxEstimatedTokensPerFile}");
			builder.AppendLine($"// Estimated chars per token: {EstimatedCharsPerToken}");
			builder.AppendLine($"// Compact output: {CompactOutput}");
			builder.AppendLine();

			return builder;
		}

		private static string BuildFileBlock(string assetPath, string content)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine("// ============================================================================");
			builder.AppendLine($"// File: {assetPath}");
			builder.AppendLine("// ============================================================================");
			builder.AppendLine(content);
			builder.AppendLine();

			return builder.ToString();
		}

		private static string CompactContent(string content)
		{
			StringBuilder builder = new StringBuilder(content.Length);

			using StringReader reader = new StringReader(content);
			while (reader.ReadLine() is { } line)
			{
				string trimmedLine = line.Trim();
				if (trimmedLine.Length == 0)
				{
					continue;
				}

				builder.AppendLine(trimmedLine);
			}

			return builder.ToString();
		}

		private static int EstimateTokenCount(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return 0;
			}

			return Mathf.CeilToInt((float)text.Length / EstimatedCharsPerToken);
		}

		private static string WritePartFile(string projectRoot, int partIndex, StringBuilder builder)
		{
			string outputAssetPath = $"Assets/{OutputFileNamePrefix}{partIndex:000}{OutputFileExtension}";
			string outputPath = Path.Combine(projectRoot, outputAssetPath);
			File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
			AssetDatabase.ImportAsset(outputAssetPath);

			return outputAssetPath;
		}

		private static void ClearPreviousDumpFiles()
		{
			DeleteAssetAndMeta(LegacyOutputFile);

			foreach (string filePath in Directory.GetFiles(UnityEngine.Application.dataPath,
					$"{OutputFileNamePrefix}*{OutputFileExtension}",
					SearchOption.TopDirectoryOnly))
			{
				DeleteAssetAndMeta(ToAssetPath(filePath));
			}
		}

		private static void DeleteAssetAndMeta(string assetPath)
		{
			string projectRoot = GetProjectRoot();
			if (string.IsNullOrEmpty(projectRoot))
			{
				return;
			}

			string fullPath = Path.Combine(projectRoot, assetPath);
			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
			}

			string metaPath = $"{fullPath}.meta";
			if (File.Exists(metaPath))
			{
				File.Delete(metaPath);
			}
		}

		private static string GetProjectRoot()
		{
			DirectoryInfo projectRoot = Directory.GetParent(UnityEngine.Application.dataPath);

			return projectRoot == null ? string.Empty : projectRoot.FullName;
		}

		private static string ToAssetPath(string path)
		{
			string normalizedPath = path.Replace('\\', '/');
			int assetsIndex = normalizedPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);

			return assetsIndex >= 0 ? normalizedPath[assetsIndex..] : normalizedPath;
		}

		private static bool IsSupportedCodeFile(string path)
		{
			string extension = Path.GetExtension(path);

			return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
		}
	}
}
