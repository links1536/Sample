using System.Collections.Generic;
using System.IO;
using Aria.AssetManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AriaEditor.AssetManagement
{
	public class AreaResourceSettingsProvider : SettingsProvider
	{
		const string SettingPath = "Project/Aria/AssetManagement Setting";

		static Editor m_Editor;

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
			=> new AreaResourceSettingsProvider(SettingPath, SettingsScope.Project, null)
			{
				guiHandler = (searchContext) => m_Editor?.OnInspectorGUI(),
			};

		public AreaResourceSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords)
			: base(path, scopes, keywords)
		{
		}


		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			if (AriaResourceSettings.TryGetInstance(out var instance))
				Editor.CreateCachedEditor(instance, typeof(AriaResourceSettingsEditor), ref m_Editor);
		}

		public override void OnGUI(string searchContext)
		{
			if (!AriaResourceSettings.TryGetInstance(out var instance))
				CreateSettings();
			if (m_Editor == null)
				Editor.CreateCachedEditor(instance, typeof(AriaResourceSettingsEditor), ref m_Editor);

			if (m_Editor == null)
				return;
			m_Editor.OnInspectorGUI();
		}


		/// <summary>
		/// 設定ファイル生成
		/// </summary>
		static void CreateSettings()
		{
			var config = ScriptableObject.CreateInstance<AriaResourceSettings>();
			var parent = "Assets/Resources";
			if (AssetDatabase.IsValidFolder(parent) == false)
			{
				// Resourcesフォルダが無いことを考慮
				AssetDatabase.CreateFolder("Assets", "Resources");
			}

			var assetPath = Path.Combine(parent, Path.ChangeExtension(nameof(AriaResourceSettings), ".asset"));
			AssetDatabase.CreateAsset(config, assetPath);
		}
	}
}
