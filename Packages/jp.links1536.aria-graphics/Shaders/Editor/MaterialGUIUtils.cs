using UnityEditor;

namespace AriaEditor.Shaders
{
	public class MaterialGUIUtils
	{
		public static void ShaderProperty(MaterialEditor materialEditor, MaterialProperty property)
		{
			if (property == null)
				return;
			materialEditor.ShaderProperty(property, property.displayName);
		}
	}
}
