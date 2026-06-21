#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_CharacterMaskTexture);
SAMPLER(sampler_CharacterMaskTexture);
float4 _CharacterMaskParams;

// R・・・スペキュラ
// G・・・リムライト
// B・・・肌
// A・・・キャラマスク
struct CharacterMask
{
	half characterMask;
	half specularMask;
	half rimLightMask;
	half skinMask;
};

CharacterMask SampleCharacterMask(float2 uv)
{
	half4 mask = SAMPLE_TEXTURE2D(_CharacterMaskTexture, sampler_CharacterMaskTexture, uv);

	CharacterMask result = (CharacterMask)0;
	result.specularMask = mask.r * _CharacterMaskParams.r;
	result.rimLightMask = mask.g * _CharacterMaskParams.g;
	result.skinMask = mask.b * _CharacterMaskParams.b;
	result.characterMask = mask.a * _CharacterMaskParams.a;

	return result;
}
