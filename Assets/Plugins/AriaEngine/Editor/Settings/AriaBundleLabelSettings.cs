using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Aria.AssetManagement;
using AriaEditor.AssetManagement;
using UnityEditor;
using UnityEngine;

namespace AriaEditor.AssetManagement
{
	[FilePath("ProjectSettings/AriaResourceLabelSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	public class AriaBundleLabelSettings : ScriptableSingleton<AriaBundleLabelSettings>
	{
		[System.Serializable]
		public class BuildTarget
		{
			public string LabelRule;
			public string[] Types;
		}

		static string[] DefaultTypes = new string[]
		{
			"prefab",
			"texture",
			"material",
			"shader",
			"scene",
			"animatorcontroller",
			"animationclip",
			"timelineasset",
		};

		public BuildTarget[] BuildTargets = new BuildTarget[]
		{
			new BuildTarget() { LabelRule = "*/*", },
		};

		public FrozenDictionary<string, HashSet<string>> CreateBundleAssetMap(string directory)
		{
			var findDirectory = new string[] { directory };
			var rootDirectory = directory.TrimEnd('/');

			var labels = new HashSet<string>(System.StringComparer.CurrentCultureIgnoreCase);
			var assetToLabels = new Dictionary<string, string?>(100, System.StringComparer.CurrentCultureIgnoreCase);
			foreach (var setting in BuildTargets)
			{
				var types = setting.Types != null && setting.Types.Length > 0
					? setting.Types
					: DefaultTypes;
				string filter = string.Join(" ", types.Select(x => $"t:{x}"));

				// ラベル用パス抽出用
				var regex = CreateLabelRegex(rootDirectory, setting.LabelRule);

				var guids = AssetDatabase.FindAssetGUIDs(filter, findDirectory);
				foreach (var guid in guids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);

					// すでにラベルが決まっているならスキップ
					if (assetToLabels.ContainsKey(assetPath))
						continue;
					var label = GetLabel(regex, assetPath);
					if (string.IsNullOrEmpty(label))
						continue;
					labels.Add(label);
					assetToLabels[assetPath] = label;
				}
			}

			var processCount = System.Environment.ProcessorCount;
			var defaultCapacity = 31;
			var bundleAssetMap = new ConcurrentDictionary<string, HashSet<string>>(processCount, defaultCapacity, System.StringComparer.CurrentCultureIgnoreCase);
			foreach (var pair in assetToLabels)
			{
				// バンドル名が空なら暗黙的な依存ファイル限定
				if (string.IsNullOrEmpty(pair.Value))
					continue;

				string bundleName = pair.Value;
				string assetPath = pair.Key;

				var assetList = bundleAssetMap.GetOrAdd(bundleName, bundleName => new HashSet<string>(System.StringComparer.CurrentCultureIgnoreCase));
				assetList.Add(assetPath);
			}
			return bundleAssetMap.ToFrozenDictionary();
		}

		Regex CreateLabelRegex(string rootDirectory, string labelRule)
		{
			var escapedRootDirectory = Regex.Escape(rootDirectory);
			var rule = labelRule.Trim('/').Replace(".*", "[^/]+");
			return new Regex($"^{escapedRootDirectory}/({rule})(/.+)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}

		string? GetLabel(Regex regex, string assetPath)
		{
			var directory = System.IO.Path.GetDirectoryName(assetPath).Replace(@"\", @"/");

			var match = regex.Match(directory);
			if (!match.Success || match.Groups.Count <= 1)
				return null;
			return match.Groups[1].Value;
		}

		public void Save()
		{
			Save(true);
		}
	}
}
