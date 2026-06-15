using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AriaEditor.Shaders.Characters
{
	class ToonShaderEditor : UnityEditor.ShaderGUI
	{
		public enum RenderMode
		{
			Opaque,
			Cutout,
			Transparent
		}

		private enum FallbackMode
		{
			Toon,
			Standard,
		}

		// 各タブを開いているかはすべてのマテリアルで共有させる
		static bool m_FoldMain = true;
		static bool m_FoldLighting;
		static bool m_FoldNormalMap;
		static bool m_FoldControl1Map;
		static bool m_FoldControl2Map;

		Material[] m_Materials;
		MaterialProperty[] m_Properties;

		MaterialProperty m_MainMap;
		MaterialProperty m_MainColor;
		MaterialProperty m_EnableLighting;
		MaterialProperty m_EnableShadingMap;
		MaterialProperty m_ShadingMap;
		MaterialProperty m_ShadingColor;

		MaterialProperty m_NormalMap;
		MaterialProperty m_NormalScale;

		MaterialProperty m_EnableControlMap1;
		MaterialProperty m_ControlMap1;
		MaterialProperty m_EnableControlMap2;
		MaterialProperty m_ControlMap2;

		MaterialProperty m_EnableMatCapMap;
		MaterialProperty m_MatCapMap;
		MaterialProperty m_ShadowSoftness;
		MaterialProperty m_ShadowThreshold;
		MaterialProperty m_FaceShadowMaskTexture;

		MaterialProperty m_EnableSpecular;
		MaterialProperty m_Smoothness;

		MaterialProperty m_EnableRimLight;
		MaterialProperty m_RimIntensity;
		MaterialProperty m_RimPower;
		MaterialProperty m_RimSoftness;
		MaterialProperty m_RimThreshold;

		MaterialProperty m_EnableHighLight;
		MaterialProperty m_HighLightColor;

		MaterialProperty m_EnableOutline;
		MaterialProperty m_OutlineWidth;
		MaterialProperty m_OutlineColor;

		MaterialProperty m_RenderingMode;
		MaterialProperty m_SourceBlend;
		MaterialProperty m_DestinationBlend;
		MaterialProperty m_ZWrite;
		MaterialProperty m_AlphaCutoff;

		MaterialProperty m_DepthOffset;

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

			m_EnableLighting = FindProperty("_EnableLighting");
			m_EnableShadingMap = FindProperty("_EnableShadingMap");
			m_ShadingMap = FindProperty("_ShadingMap");
			m_ShadingColor = FindProperty("_ShadingColor");

			m_NormalMap = FindProperty("_NormalMap");
			m_NormalScale = FindProperty("_NormalScale");

			m_EnableMatCapMap = FindProperty("_EnableMatCapMap");
			m_MatCapMap = FindProperty("_MatCapMap");
			m_ShadowSoftness = FindProperty("_ShadowSoftness");
			m_ShadowThreshold = FindProperty("_ShadowThreshold");
			m_FaceShadowMaskTexture = FindProperty("_FaceShadowMaskTexture");

			m_EnableControlMap1 = FindProperty("_EnableControlMap1");
			m_ControlMap1 = FindProperty("_ControlMap1");
			m_EnableControlMap2 = FindProperty("_EnableControlMap2");
			m_ControlMap2 = FindProperty("_ControlMap2");

			m_EnableSpecular = FindProperty("_EnableSpecular");
			m_Smoothness = FindProperty("_Smoothness");

			m_EnableRimLight = FindProperty("_EnableRimLight");
			m_RimIntensity = FindProperty("_RimIntensity");
			m_RimPower = FindProperty("_RimPower");
			m_RimSoftness = FindProperty("_RimSoftness");
			m_RimThreshold = FindProperty("_RimThreshold");

			m_EnableHighLight = FindProperty("_EnableHighLight");
			m_HighLightColor = FindProperty("_HighLightColor");

			m_AlphaCutoff = FindProperty("_AlphaClip");
			m_EnableOutline = FindProperty("_EnableOutline");
			m_OutlineWidth = FindProperty("_OutlineWidth");
			m_OutlineColor = FindProperty("_OutlineColor");

			m_RenderingMode = FindProperty("_RenderingMode");
			m_SourceBlend = FindProperty("_SrcBlend");
			m_DestinationBlend = FindProperty("_DstBlend");
			m_ZWrite = FindProperty("_ZWrite");

			m_DepthOffset = FindProperty("_DepthOffset");

			// レンダリングモード
			RenderingModeField();
			RenderingBlendMode(m_SourceBlend);
			RenderingBlendMode(m_DestinationBlend);
			ZWriteMode(m_ZWrite);

			// メインカラー情報
			using (var foldout = new FoldoutScope(m_FoldMain, "Main"))
			{
				if (foldout.Open)
				{
					EditorGUILayoutUtils.HeaderField("Main");
					TextureField(m_MainMap);
					PropertyField(m_MainColor);
					if ((RenderMode)m_RenderingMode.floatValue == RenderMode.Cutout)
						PropertyField(m_AlphaCutoff);

					EditorGUILayoutUtils.HeaderField("Shading");
					TextureWithFlagValueField(m_ShadingMap, m_EnableShadingMap);
					PropertyField(m_ShadingColor);

				}
				m_FoldMain = foldout.Open;
			}

			// ライティング情報
			using (var foldout = new FoldoutScope(m_FoldLighting, "Lighting"))
			{
				if (foldout.Open)
				{
					if (ToggleFlagValueField("Lighting", m_EnableLighting))
					{
						if(ToggleKeywordField("Face Mode", "FACELIGHTING_ON"))
						{
							TextureField(m_FaceShadowMaskTexture);
						}

						PropertyField(m_ShadowSoftness);
						PropertyField(m_ShadowThreshold);
					}

					EditorGUILayoutUtils.HeaderField("Matcap");
					TextureWithFlagValueField(m_MatCapMap, m_EnableMatCapMap);
				}
				m_FoldLighting = foldout.Open;
			}

			// 法線情報
			using (var foldout = new FoldoutScope(m_FoldNormalMap, "Normal Map"))
			{
				if (foldout.Open)
				{
					TextureWithKeywordField(m_NormalMap, "NORMALMAP_ON");
					PropertyField(m_NormalScale);
				}
				m_FoldNormalMap = foldout.Open;
			}

			//コントロール1
			using (var foldout = new FoldoutScope(m_FoldControl1Map, "Control1"))
			{
				if (foldout.Open)
				{
					TextureWithFlagValueField(m_ControlMap1, m_EnableControlMap1);

					// スペキュラー
					EditorGUILayout.Space();
					//EditorGUILayoutUtils.HeaderField("Specular");
					if (ToggleFlagValueField("Specular", m_EnableSpecular, EditorStyles.boldLabel))
					{
						PropertyField(m_Smoothness);
					}

					// リムライト
					EditorGUILayout.Space();
					//EditorGUILayoutUtils.HeaderField("Rim Light");
					if (ToggleFlagValueField("Rim Light", m_EnableRimLight, EditorStyles.boldLabel))
					{
						PropertyField(m_RimIntensity);
						PropertyField(m_RimPower);
						PropertyField(m_RimSoftness);
						PropertyField(m_RimThreshold);
					}

					// ハイライト
					EditorGUILayout.Space();
					if (ToggleFlagValueField("High Light", m_EnableHighLight, EditorStyles.boldLabel))
					{
						PropertyField(m_HighLightColor);
					}
				}
				m_FoldControl1Map = foldout.Open;
			}
			// コントロール2
			using (var foldout = new FoldoutScope(m_FoldControl2Map, "Control2"))
			{
				if (foldout.Open)
				{
					TextureWithFlagValueField(m_ControlMap2, m_EnableControlMap2);

					// アルファチャンネルはレンダリングモードで切り替える
					ToggleKeywordField("Alpha from Control2", "ALPHA_FROM_CONTROL");

					EditorGUILayout.Space();
					//EditorGUILayoutUtils.HeaderField("Outline");
					if (TogglePassField("Outline", m_EnableOutline, "Outline", EditorStyles.boldLabel))
					{
						PropertyField(m_OutlineWidth);
						PropertyField(m_OutlineColor);
					}
				}
				m_FoldControl2Map = foldout.Open;
			}

			// レンダリング順番
			m_MaterialEditor.RenderQueueField();


			if (ToggleKeywordField("Depth Offset", "DEPTH_OFFSET_ON"))
			{
				PropertyField(m_DepthOffset);
			}
		}

		void TextureField(MaterialProperty property)
		{
			m_MaterialEditor.TextureProperty(property, property.displayName);
		}

		void PropertyField(MaterialProperty property)
		{
			if (property != null)
				m_MaterialEditor.ShaderProperty(property, property.displayName);
		}

		void TextureWithKeywordField(MaterialProperty property, string keyword)
		{
			// テクスチャがアタッチされていたらキーワードをオンにする
			TextureField(property);
			foreach (var material in m_Materials)
			{
				bool hasTexture = material.GetTexture(property.name) != null;
				if (hasTexture != material.IsKeywordEnabled(keyword))
				{
					if (hasTexture)
						material.EnableKeyword(keyword);
					else
						material.DisableKeyword(keyword);
				}
			}
		}

		void TextureWithFlagValueField(MaterialProperty textureProperty, MaterialProperty flagValue)
		{
			// テクスチャがアタッチされていたらキーワードをオンにする
			TextureField(textureProperty);

			foreach (var material in m_Materials)
			{
				float value = material.GetTexture(textureProperty.name) != null ? 1 : 0;
				material.SetFloat(flagValue.name, value);
			}
		}

		bool ToggleOnOff(string label, bool value, GUIStyle style)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(label, style);
				if (GUILayout.Button(EditorGUI.showMixedValue ? "―" : value ? "On" : "Off"))
					value = !value;
				return value;
			}
		}

		bool ToggleFlagValueField(string label, MaterialProperty property)
			=> ToggleFlagValueField(label, property, EditorStyles.label);

		bool ToggleFlagValueField(string label, MaterialProperty property, GUIStyle style)
		{
			if (property == null)
			{
				Debug.LogError($"Null Property: {label}");
				return false;
			}
			EditorGUI.showMixedValue = property.hasMixedValue;
			bool oldEnable = property.floatValue != 0;
			bool enable = ToggleOnOff(label, oldEnable, style);
			if (enable != oldEnable)
				property.floatValue = enable ? 1 : 0;
			EditorGUI.showMixedValue = false;
			return enable;
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
			if (enable)
				foreach (var material in m_Materials)
					material.EnableKeyword(keyword);
			else
				foreach (var material in m_Materials)
					material.DisableKeyword(keyword);
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

		void ZWriteMode(MaterialProperty zWriteMode)
		{
			var oldZWriteModeFlag = zWriteMode.floatValue > 0;
			EditorGUI.showMixedValue = zWriteMode.hasMixedValue;
			bool zWriteModeFlag = ToggleOnOff(zWriteMode.displayName, oldZWriteModeFlag, EditorStyles.label);
			EditorGUI.showMixedValue = false;
			if (zWriteModeFlag == oldZWriteModeFlag)
				return;
			zWriteMode.floatValue = zWriteModeFlag ? 1 : 0;
		}
	}
}
