#pragma once

#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/Common.hlsl"

#include "../include/character_common.hlsl"

struct Attributes
{
	float4	positionOS	: POSITION;
	half3	normalOS	: NORMAL;
	half4	tangentOS	: TANGENT;
	float2	uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4	positionCS	: SV_POSITION;
	float2	uv			: TEXCOORD0;
	half3	normalWS	: TEXCOORD1;
	half4	tangentWS	: TEXCOORD2;
	float3	positionWS	: TEXCOORD3;
	float	fogFactor	: TEXCOORD4;
	float	depthFade	: TEXCOORD5;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert (Attributes input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	Varyings output;
	ZERO_INITIALIZE(Varyings, output);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);

	// 法線情報
	half3   normalWS  = TransformObjectToWorldNormal(input.normalOS);
	half3   tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
	output.normalWS = normalWS;
	output.tangentWS = half4(tangentWS, input.tangentOS.w);

	output.fogFactor = ComputeFogFactor(output.positionCS.z);

#if defined(DEPTH_OFFSET_ON)
	// 正面を向いているかでフェードさせる
	float fade = GetForwardFade(output.positionWS);
	output.depthFade = fade;

	// View空間上で深度値をオフセットする
	float3 positionVS = TransformWorldToView(output.positionWS);
	positionVS.z += _DepthOffset;
	float4 depthPositionCS = TransformWViewToHClip(positionVS);
	float depth = depthPositionCS.z / depthPositionCS.w;
	output.positionCS.z = depth * output.positionCS.w;
#endif

	return output;
}

half4 frag(Varyings input, FRONT_FACE_TYPE faceType : FRONT_FACE_SEMANTIC) : SV_Target
{
	// 表裏の判定
	half facing = IS_FRONT_VFACE(faceType, 1, -1);

	// 法線取得
	half3 normalWS = SampleNormalWS(input.uv, input.normalWS, input.tangentWS.xyz, input.tangentWS.w, facing);
	half3 lightDirectionWS = GetLightDirectionWS(input.positionWS.xyz);
	half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

	// ベクトルの計算
	VectorData vectorData = ComputeVectorData(normalWS, lightDirectionWS, viewDirectionWS);

	ControlTex control = SampleControlTex(input.uv);

	// ライトの当たり具合
	half lightAtten = 1;
	if (_EnableLighting != 0) {
	#if defined(FACELIGHTING_ON)
		half sdfShadow = SampleFaceSDF(input.uv, lightDirectionWS, vectorData.NdotL);
		lightAtten = ToonStep(sdfShadow, _ShadowThreshold, _ShadowSoftness);
	#else
		lightAtten = ToonStep(vectorData.NdotL, _ShadowThreshold, _ShadowSoftness);
	#endif
	}

	// シャドウマスクで光の影響を潰す
	lightAtten *= control.shadowMask;

	half3 mainLightColor = SampleMainLight(input.positionWS, normalWS);
	half3 directLightColor = mainLightColor;
	//half3 indirectLightColor = SampleSH(normalWS);
	half3 indirectLightColor = 0;// half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

	// 日向・日陰の色
	half4 baseTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
	half3 baseColor = baseTexColor.rgb * _Color.rgb;
	half3 shadowColor = baseTexColor.rgb * _ShadingColor.rgb;
	half  alpha = baseTexColor.a;
	if (_EnableShadingMap != 0) {
		half3 shadowTexColor = SAMPLE_TEXTURE2D(_ShadingMap, sampler_ShadingMap, input.uv).rgb;
		shadowColor = shadowTexColor.rgb * _ShadingColor.rgb;
	}

	// ハイライト
	if (_EnableHighLight != 0) {
		//half3 highlightColor = _HighLightColor.rgb * control.highlightMask;
		//baseColor = BLEND_SCREEN(baseColor, highlightColor);
		baseColor = lerp(baseColor, _HighLightColor.rgb, control.highlightMask * lightAtten);
	}

	// 色決定
	half4 texColor = half4(lerp(shadowColor, baseColor, lightAtten), alpha);
	//half4 texColor = half4(highlightColor, alpha);
	half3 lightColor = (directLightColor + indirectLightColor);
	lightColor *= 1.0 - saturate(length(indirectLightColor));

#if defined(ALPHA_FROM_CONTROL)
	texColor.a = control.alphaMask;
#endif
#if defined(ALPHACLIP_ON)
	clip(texColor.a - _AlphaClip);
#endif

	if (_EnableMatCapMap != 0)
		texColor.rgb *= SampleMatCap(TEXTURE2D_ARGS(_MatCapMap, sampler_MatCapMap), normalWS);

	half4 finalColor = texColor;
	finalColor.rgb *= lightColor;
	finalColor = saturate(finalColor);

	// スペキュラ―
	if (_EnableSpecular != 0) {
		half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(_Smoothness);
		half roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
		half specular = D_GGXNoPI(vectorData.NdotH, roughness);
		//half specular = D_GGX(vectorData.NdotH, roughness);
		//half specular = G_MaskingSmithGGX(vectorData.NdotH, roughness);
		finalColor.rgb += texColor.rgb * specular * control.specularMask;
	}


	// リムライト
	//if (_EnableRimLight != 0) {
	//	// 元の色を乗せて加算する
	//	finalColor.rgb +=  RimLight(input.positionWS.xyz, normalWS, lightAtten) * control.rimMask;
	//}

	// 霧を適用
	finalColor.rgb = MixFogWithAlpha(finalColor.rgb, input.fogFactor);

#if defined(DEPTH_OFFSET_ON)
	finalColor.a *= input.depthFade;
#endif

	return finalColor;
}
