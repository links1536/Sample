using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AriaEditor.Shaders.Map
{
	class MapShaderEditor : UnityEditor.ShaderGUI
	{
		public enum RenderMode
		{
			Opaque,
			Cutout,
			Transparent
		}

		// 各タブを開いているかはすべてのマテリアルで共有させる
		static bool m_FoldMain = true;
		static bool m_FoldLighting;

		Material[] m_Materials;
		MaterialProperty[] m_Properties;

		MaterialProperty m_MainMap;
		MaterialProperty m_MainColor;

		MaterialProperty m_GlowMap;
		MaterialProperty m_GlowColor;

		MaterialProperty m_NormalMap;
		MaterialProperty m_NormalScale;

		MaterialProperty m_ControlMap1;
		MaterialProperty m_Metallic;
		MaterialProperty m_Smoothness;
		MaterialProperty m_Occlusion;

		MaterialProperty m_MatCapMap;

		MaterialProperty m_RenderingMode;
		MaterialProperty m_CullMode;
		MaterialProperty m_SourceBlend;
		MaterialProperty m_DestinationBlend;
		MaterialProperty m_AlphaCutoff;
		MaterialProperty m_FogFactor;

		MaterialEditor m_MaterialEditor;

		MaterialProperty FindProperty(string propertyName)
			=> m_Properties.FirstOrDefault(x => x.name == propertyName);

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			//base.OnGUI(materialEditor, properties);

			m_Properties = properties;
			m_MaterialEditor = materialEditor;
			m_Materials = m_MaterialEditor.targets?.OfType<Material>()?.ToArray();
			if (m_Materials == null || m_Materials.Length <= 0)
			{
				EditorGUILayout.HelpBox("ターゲットがマテリアルではありません", MessageType.Error);
				return;
			}
			ShaderFields();
		}

		void ShaderFields()
		{
			m_MainMap = FindProperty("_MainTex");
			m_MainColor = FindProperty("_Color");
			m_AlphaCutoff = FindProperty("_AlphaClip");

			m_GlowMap = FindProperty("_GlowTex");
			m_GlowColor = FindProperty("_GlowColor");

			m_NormalMap = FindProperty("_NormalMap");
			m_NormalScale = FindProperty("_NormalScale");
			m_MatCapMap = FindProperty("_MatCapMap");

			m_ControlMap1 = FindProperty("_ControlMap1");
			m_Metallic = FindProperty("_Metallic");
			m_Smoothness = FindProperty("_Smoothness");
			m_Occlusion = FindProperty("_Occlusion");

			m_RenderingMode = FindProperty("_RenderingMode");
			m_CullMode = FindProperty("_CullMode");
			m_SourceBlend = FindProperty("_SrcBlend");
			m_DestinationBlend = FindProperty("_DstBlend");

			m_FogFactor = FindProperty("_FogFactor");

			// レンダリングモード
			RenderingModeField();
			RenderingCullMode(m_CullMode);
			RenderingBlendMode(m_SourceBlend);
			RenderingBlendMode(m_DestinationBlend);

			// メインカラー情報
			using (var foldout = new FoldoutScope(m_FoldMain, "Main"))
			{
				if (foldout.Open)
				{
					EditorGUILayoutUtils.HeaderField("Main");
					TextureField(m_MainMap);
					MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_MainColor);
					if ((RenderMode)m_RenderingMode.floatValue == RenderMode.Cutout)
						MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_AlphaCutoff);

					// 発光表現
					EditorGUILayoutUtils.HeaderField("Glow");
					TextureWithKeywordField(m_GlowMap, "GLOWMAP_ON");
					MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_GlowColor);
				}
				m_FoldMain = foldout.Open;
			}

			// ライティング情報
			using (var foldout = new FoldoutScope(m_FoldLighting, "Lighting"))
			{
				if (foldout.Open)
				{
					ToggleKeywordFieldOff("Lighting On", "LIGHTING_OFF");

					EditorGUILayoutUtils.HeaderField("Normal");
					TextureWithKeywordField(m_NormalMap, "NORMALMAP_ON");
					MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_NormalScale);

					EditorGUILayoutUtils.HeaderField("Matcap");
					TextureWithKeywordField(m_MatCapMap, "MATCAP_ON");

					EditorGUILayoutUtils.HeaderField("Control");
					TextureWithKeywordField(m_ControlMap1, "CONTROLMAP_1_ON");

					// スペキュラー
					EditorGUILayout.Space();
					if (ToggleKeywordFieldOff("Specular", "SPECULAR_OFF", EditorStyles.boldLabel))
					{
						MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_Metallic);
						MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_Smoothness);
					}
					MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_Occlusion);
				}
				m_FoldLighting = foldout.Open;
			}

			//ToggleKeywordField("Disable Fog", "FOG_OFF");
			if (m_FogFactor != null)
				MaterialGUIUtils.ShaderProperty(m_MaterialEditor, m_FogFactor);

			// レンダリング順番
			m_MaterialEditor.RenderQueueField();
			m_MaterialEditor.EnableInstancingField();
		}

		void TextureField(MaterialProperty property)
		{
			MaterialGUIUtils.ShaderProperty(m_MaterialEditor, property);
		}

		void TextureWithKeywordField(MaterialProperty property, string keyword)
		{
			// テクスチャがアタッチされていたらキーワードをオンにする
			TextureField(property);
			foreach (var material in m_Materials)
			{
				if (property.textureValue != null && !material.IsKeywordEnabled(keyword))
					material.EnableKeyword(keyword);
				if (property.textureValue == null && material.IsKeywordEnabled(keyword))
					material.DisableKeyword(keyword);
			}
		}

		bool ToggleOnOff(string label, bool value, GUIStyle style)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(label, style);
				if (GUILayout.Button(value ? "On" : "Off"))
					value = !value;
				return value;
			}
		}

		void ToggleKeyword(string keyword, bool enable)
		{
			if (enable)
				foreach (var material in m_Materials)
					material.EnableKeyword(keyword);
			else
				foreach (var material in m_Materials)
					material.DisableKeyword(keyword);
		}

		bool ToggleKeywordField(string label, string keyword)
			=> ToggleKeywordField(label, keyword, EditorStyles.label);

		bool ToggleKeywordField(string label, string keyword, GUIStyle style)
		{
			bool oldEnable = m_Materials[0].IsKeywordEnabled(keyword);
			bool showMixedValue = !(m_Materials.All(x => x.IsKeywordEnabled(keyword)) || m_Materials.All(x => !x.IsKeywordEnabled(keyword)));
			EditorGUI.showMixedValue = showMixedValue;
			bool enable = ToggleOnOff(label, oldEnable, style);
			EditorGUI.showMixedValue = false;
			if (enable == oldEnable)
				return showMixedValue ? true : oldEnable;
			ToggleKeyword(keyword, enable);
			return enable;
		}

		bool ToggleKeywordFieldOff(string label, string keyword)
			=> ToggleKeywordFieldOff(label, keyword, EditorStyles.label);

		bool ToggleKeywordFieldOff(string label, string keyword, GUIStyle style)
		{
			bool oldEnable = !m_Materials[0].IsKeywordEnabled(keyword);
			bool showMixedValue = !(m_Materials.All(x => x.IsKeywordEnabled(keyword)) || m_Materials.All(x => !x.IsKeywordEnabled(keyword)));
			EditorGUI.showMixedValue = showMixedValue;
			bool enable = ToggleOnOff(label, oldEnable, style);
			EditorGUI.showMixedValue = false;
			if (enable == oldEnable)
				return showMixedValue ? true : oldEnable;
			ToggleKeyword(keyword, !enable);
			return enable;
		}

		bool TogglePassField(string label, MaterialProperty property, string passName)
			=> TogglePassField(label, property, passName, EditorStyles.label);

		bool TogglePassField(string label, MaterialProperty property, string passName, GUIStyle style)
		{
			bool oldEnable = property.floatValue != 0;
			EditorGUI.showMixedValue = property.hasMixedValue;
			bool enable = ToggleOnOff(label, oldEnable, style);
			EditorGUI.showMixedValue = false;
			if (enable == oldEnable)
				return property.hasMixedValue ? true : oldEnable;
			property.floatValue = enable ? 1 : 0;
			foreach (var material in m_Materials)
				material.SetShaderPassEnabled(passName, enable);
			return enable;
		}

		void RenderingModeField()
		{
			var renderingMode = (RenderMode)m_RenderingMode.floatValue;
			EditorGUI.showMixedValue = m_RenderingMode.hasMixedValue;
			renderingMode = (RenderMode)EditorGUILayout.EnumPopup(m_RenderingMode.displayName, renderingMode);
			EditorGUI.showMixedValue = false;
			if (m_RenderingMode.floatValue == (float)renderingMode)
				return;
			m_RenderingMode.floatValue = (float)renderingMode;
			string alphaClipOn = "ALPHACLIP_ON";
			string transparentOn = "TRANSPARENT_ON";
			foreach (var material in m_Materials)
			{
				switch (renderingMode)
				{
					case RenderMode.Transparent:
						// 半透明
						material.SetOverrideTag("RenderType", "Transparent");
						material.renderQueue = 3000;
						if (material.IsKeywordEnabled(alphaClipOn))
							material.DisableKeyword(alphaClipOn);
						if (!material.IsKeywordEnabled(transparentOn))
							material.EnableKeyword(transparentOn);
						m_SourceBlend.floatValue = (float)BlendMode.One;
						m_DestinationBlend.floatValue = (float)BlendMode.OneMinusSrcAlpha;
						break;
					case RenderMode.Cutout:
						// アルファクリップ
						material.SetOverrideTag("RenderType", "TransparentCutout");
						material.renderQueue = 2450;
						if (!material.IsKeywordEnabled(alphaClipOn))
							material.EnableKeyword(alphaClipOn);
						if (material.IsKeywordEnabled(transparentOn))
							material.DisableKeyword(transparentOn);
						m_SourceBlend.floatValue = (float)BlendMode.One;
						m_DestinationBlend.floatValue = (float)BlendMode.Zero;
						break;

					case RenderMode.Opaque:
					default:
						// 不透明
						material.SetOverrideTag("RenderType", "");
						material.renderQueue = -1;
						if (material.IsKeywordEnabled(alphaClipOn))
							material.DisableKeyword(alphaClipOn);
						if (material.IsKeywordEnabled(transparentOn))
							material.DisableKeyword(transparentOn);
						m_SourceBlend.floatValue = (float)BlendMode.One;
						m_DestinationBlend.floatValue = (float)BlendMode.Zero;
						break;
				}
			}
		}

		void RenderingBlendMode(MaterialProperty blendModeProperty)
		{
			var blendMode = (BlendMode)blendModeProperty.floatValue;
			EditorGUI.showMixedValue = blendModeProperty.hasMixedValue;
			blendMode = (BlendMode)EditorGUILayout.EnumPopup(blendModeProperty.displayName, blendMode);
			EditorGUI.showMixedValue = false;
			if (blendModeProperty.floatValue == (float)blendMode)
				return;
			blendModeProperty.floatValue = (float)blendMode;
		}

		void RenderingCullMode(MaterialProperty cullModeProperty)
		{
			var blendMode = (CullMode)cullModeProperty.floatValue;
			EditorGUI.showMixedValue = cullModeProperty.hasMixedValue;
			blendMode = (CullMode)EditorGUILayout.EnumPopup(cullModeProperty.displayName, blendMode);
			EditorGUI.showMixedValue = false;
			if (cullModeProperty.floatValue == (float)blendMode)
				return;
			cullModeProperty.floatValue = (float)blendMode;
		}
	}
}
