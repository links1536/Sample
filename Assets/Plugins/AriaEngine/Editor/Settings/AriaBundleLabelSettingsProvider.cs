using Aria.AssetManagement;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AriaEditor.AssetManagement
{
	public class AriaBundleLabelSettingsProvider : SettingsProvider
	{
		const string SettingPath = "Project/Aria/LabelRule Setting";

		Editor? m_Editor;

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
			=> new AriaBundleLabelSettingsProvider(SettingPath, SettingsScope.Project, null);

		public AriaBundleLabelSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords)
			: base(path, scopes, keywords)
		{
		}


		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			var setting = AriaBundleLabelSettings.instance;
			setting.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
			Editor.CreateCachedEditor(setting, null, ref m_Editor);
		}

		public override void OnGUI(string searchContext)
		{
			if (m_Editor == null)
				return;
			EditorGUI.BeginChangeCheck();
			m_Editor.OnInspectorGUI();
			if (EditorGUI.EndChangeCheck())
			{
				// 差分があったら保存
				AriaBundleLabelSettings.instance.Save();
			}
		}
	}
}
