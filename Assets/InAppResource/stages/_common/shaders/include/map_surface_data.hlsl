#pragma once

struct MapSurfaceData
{
	half3 albedo;
	half3 normalTS;
	half3 emission;

	half  metallic;
	half  smoothness;
	half  occlusion;

	half  alpha;

	half fogIntensity;
};

//#define SURFACE_DATA SurfaceData
#define SURFACE_DATA MapSurfaceData
