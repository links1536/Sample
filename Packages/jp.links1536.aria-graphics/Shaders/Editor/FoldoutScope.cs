using System;
using UnityEditor;

namespace AriaEditor
{
	public struct FoldoutScope : IDisposable
	{
		bool m_Open;

		public bool Open
			=> m_Open;

		public FoldoutScope(bool open, string label)
		{
			m_Open = EditorGUILayoutUtils.Foldout(open, label);
			EditorGUI.indentLevel++;
		}

		public void Dispose()
		{
			EditorGUI.indentLevel--;
		}
	}
}
