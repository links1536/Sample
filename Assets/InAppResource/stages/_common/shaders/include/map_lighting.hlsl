#pragma once

half3 MapLightingPhysicallyBased(InputData inputData, SURFACE_DATA surfaceData, Light light)
{
	BRDFData brdfData = (BRDFData)0;
	InitializeMapBRDFData(surfaceData, brdfData);

	half3 normalWS = inputData.normalWS;
	half3 viewDirWS = normalize(inputData.viewDirectionWS);
	half3 lightDirWS = normalize(light.direction.xyz);
	half3 halfDirWS = normalize(viewDirWS + lightDirWS);

	half NdotL = saturate(dot(normalWS, lightDirWS));
	half NdotV = saturate(dot(normalWS, viewDirWS));
	half NdotH = saturate(dot(normalWS, halfDirWS));
	half LdotH = saturate(dot(lightDirWS, halfDirWS));

	half3 radiance = light.color * (light.distanceAttenuation * light.shadowAttenuation);

	half3 diffuse = brdfData.diffuse * DisneyDiffuse(NdotV, NdotL, LdotH, brdfData.perceptualRoughness);

#if !defined(SPECULAR_OFF)
	//half3 specular = brdfData.specular * NormalizedBlinnPhong(NdotH, surfaceData.smoothness);
	half3 specular = brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirWS, viewDirWS);
#else
	half3 specular = 0;
#endif

	return (diffuse + specular) * radiance * NdotL;
}

LightingData CreateMapLightingData(InputData inputData, SURFACE_DATA surfaceData, half occlusion)
{
	LightingData lightingData;

	lightingData.giColor = inputData.bakedGI * occlusion;
	lightingData.emissionColor = surfaceData.emission;
	lightingData.vertexLightingColor = 0;
	lightingData.mainLightColor = 0;
	lightingData.additionalLightsColor = 0;

	return lightingData;
}

half4 CalculateMapFinalColor(LightingData lightingData, SURFACE_DATA surfaceData, float fogCoord)
{
	half3 lightingColor = CalculateLightingColor(lightingData, surfaceData.albedo.rgb);

	half3 finalColor = MixFogWithAlpha(lightingColor, fogCoord, surfaceData.fogIntensity);
	return half4(finalColor, surfaceData.alpha);
}

half4 CalculateMapFinalColorUnlit(SURFACE_DATA surfaceData, float fogCoord)
{
	half3 lightingColor = surfaceData.albedo.rgb;
	lightingColor.rgb += surfaceData.emission;

	half3 finalColor = MixFogWithAlpha(lightingColor, fogCoord, surfaceData.fogIntensity);
	return half4(finalColor, surfaceData.alpha);
}

half4 FragmentLighting(InputData inputData, SURFACE_DATA surfaceData)
{
	uint meshRenderingLayers = GetMeshRenderingLayer();
	half4 shadowMask = CalculateShadowMask(inputData);
	AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, surfaceData.occlusion);
	Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

	// BRDFDataを初期化
	BRDFData brdfData = (BRDFData)0;
	InitializeMapBRDFData(surfaceData, brdfData);

	// 別の処理でAOの計算をするのでここでは渡さない
	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

	// ライト情報作成
	LightingData lightingData = CreateMapLightingData(inputData, surfaceData, aoFactor.indirectAmbientOcclusion);

#ifdef _LIGHT_LAYERS
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
	{
		lightingData.mainLightColor = MapLightingPhysicallyBased(inputData, surfaceData, mainLight);
	}

#if defined(_ADDITIONAL_LIGHTS)
	uint pixelLightCount = GetAdditionalLightsCount();

	// Forward+
	#if USE_CLUSTER_LIGHT_LOOP
		[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
		{
			CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
			Light additionalLight = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

		#ifdef _LIGHT_LAYERS
			if (IsMatchingLightLayer(additionalLight.layerMask, meshRenderingLayers))
		#endif
			{
				lightingData.additionalLightsColor += MapLightingPhysicallyBased(inputData, surfaceData, additionalLight);
			}
		}
	#endif

	// 通常の加算ライト
	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light additionalLight = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

	#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(additionalLight.layerMask, meshRenderingLayers))
	#endif
		{
			lightingData.additionalLightsColor += MapLightingPhysicallyBased(inputData, surfaceData, additionalLight);
		}
	LIGHT_LOOP_END
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
		lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
#endif

	return CalculateMapFinalColor(lightingData, surfaceData, inputData.fogCoord);
}


half4 FragmentUnlighting(InputData inputData, SURFACE_DATA surfaceData)
{
	return CalculateMapFinalColorUnlit(surfaceData, inputData.fogCoord);
}