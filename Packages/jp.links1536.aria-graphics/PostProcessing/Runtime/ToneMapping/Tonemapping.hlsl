#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/PostProcessing/EncodeHDR.hlsl"

static const float e = 2.71828;

float W_f(float x, float e0, float e1)
{
	float a = (x - e0) / (e1 - e0);
	return (x <= e0) ? 0
		: (x >= e1) ? 1
		: a * a * (3 - 2 * a);
}

float H_f(float x, float e0, float e1)
{
	return (x <= e0) ? 0
		: (x >= e1) ? e1
		: (x - e0) / max(0.0001, e1 - e0);
}

float GranTurismoTonemapper(float x)
{
	float P = 1;
	float a = 1;
	float m = 0.22;
	float l = 0.4;
	float c = 1.33;
	float b = 0;
	float l0 = (P - m) * l / a;
	float L0 = m - m / a;
	float L1 = m + (1 - m) / a;
	float L_x = m + a * (x - m);
	float T_x = m * pow(abs(x / m), c) + b;
	float S0 = m + l0;
	float S1 = m + a * l0;
	float C2 = a * P / (P - S1);
	float S_x = P - (P - S1) * pow(abs(e), -(C2 * (x - S0) / P));
	float w0_x = 1 - W_f(x, 0, m);
	float w2_x = H_f(x, m + l0, m + l0);
	float w1_x = 1 - w0_x - w2_x;
	float f_x = T_x * w0_x + L_x * w1_x + S_x * w2_x;
	return f_x;
}
