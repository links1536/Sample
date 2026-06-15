using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AriaEditor.Shaders.Characters
{
	class ShaderConverter : EditorWindow
	{
		[MenuItem("Assets/Aria/To Toon Shader")]
		static void Convert()
		{
			var materials = Selection.objects.OfType<Material>().ToArray();
			if (materials == null || materials.Length <= 0)
				return;
			var shader = Shader.Find("Aria/Character/Basic");
			foreach (var material in materials)
			{
				if (material.shader == shader)
				{
					int propertyCount = shader.GetPropertyCount();
					for (int i = 0; i < propertyCount; ++i)
					{
						int id = shader.GetPropertyNameId(i);
						if (material.HasProperty(id))
						{
							material.RevertPropertyOverride(id);
						}
					}
					continue;
				}
				var mainTex = material.GetTexture("_MainTex");
				var color = material.GetColor("_Color");
				material.shader = shader;
				material.SetTexture("_BaseMap", mainTex);
				material.SetColor("_BaseColor", color);
			}
		}
	}
}
