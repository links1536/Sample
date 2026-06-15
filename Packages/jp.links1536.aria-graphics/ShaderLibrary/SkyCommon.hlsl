#pragma once

#define MIE_G (-0.990)
#define MIE_G2 (0.9801)

float GetMiePhase(float eyeCos, float eyeCos2, half sunSize)
{
	float temp = 1.0 + MIE_G2 - 2.0 * MIE_G * eyeCos;
	temp = pow(temp, pow(sunSize, 0.65) * 10);
	temp = max(temp, 1.0e-4);
	temp = 1.5 * ((1.0 - MIE_G2) / (2.0 + MIE_G2)) * (1.0 + eyeCos2) / temp;
	return temp;
}

half SunAttenuation(float3 lightRay, float3 viewRay, float sunSize, float sunCoverage)
{
	float focusedEyeCos = pow(saturate(dot(lightRay, viewRay)), sunCoverage * 10);
	return GetMiePhase(-focusedEyeCos, focusedEyeCos * focusedEyeCos, sunSize);
}
