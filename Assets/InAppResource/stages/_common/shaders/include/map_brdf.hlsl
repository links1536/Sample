#pragma once


inline void InitializeMapBRDFData(SURFACE_DATA surfaceData, out BRDFData outBRDFData)
{
	outBRDFData = (BRDFData)0;

	half3 albedo = surfaceData.albedo;
	half metallic = surfaceData.metallic;
	half smoothness = surfaceData.smoothness;

	half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
	half reflectivity = half(1.0) - oneMinusReflectivity;
	half3 brdfDiffuse = albedo * oneMinusReflectivity;
	half3 brdfSpecular = lerp(kDielectricSpec.rgb, albedo, metallic);

	InitializeBRDFDataDirect(albedo, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, smoothness, surfaceData.alpha, outBRDFData);
}

half3 NormalizedPhong(half RdotL, half power)
{
	half norm = (power + 1.0) * INV_TWO_PI;
	return pow(RdotL, power) * norm;
}

half3 NormalizedBlinnPhong(half NdotH, half smoothness)
{
	half power = exp2(10 * smoothness + 1);
	half norm = (power + 2.0) * INV_TWO_PI;
	return pow(NdotH, power) * norm;
}
