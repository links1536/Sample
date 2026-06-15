using UnityEditor;
using UnityEngine;

namespace AriaEditor
{
	public static class EditorGUILayoutUtils
	{
		static GUIStyle m_Foldout;

		static EditorGUILayoutUtils()
		{
			// ParticleSystemのUIが分かりやすいので流用する
			m_Foldout = new GUIStyle("ShurikenModuleTitle")
			{
				font = EditorStyles.label.font,
				fontSize = EditorStyles.label.fontSize,
				fontStyle = EditorStyles.label.fontStyle,
				border = new RectOffset(15, 7, 4, 4),
				contentOffset = new Vector2(20f, -2f),
				fixedHeight = 22
			};
		}

		public static bool Foldout(bool foldout, string label)
		{
			label = (foldout ? "▼" : "▶") + label;
			if (GUILayout.Button(label, m_Foldout))
				foldout = !foldout;
			return foldout;
		}

		public static void HeaderField(string label)
		{
			EditorGUILayout.GetControlRect(false, 2);
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
		}
	}
}
