#pragma once

inline float RandomNoise_2_1_Float(float2 texCoord, float Seed)
{
	return frac(sin(dot(texCoord.xy, float2(12.9898, 78.233)) + Seed) * 43758.5453);
}

inline float3 RandomNoise_3_3_Float(float3 st)
{
	st = float3(
		dot(st, float3(127.1, 311.7, 192.9)),
		dot(st, float3(269.5, 183.3, 235.1)),
		dot(st, float3(166.8, 98.8, 54.6))
	);
	return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
}

float PerlinNoise_3_1_Float(float3 st)
{
	float3 p = floor(st);
	float3 f = frac(st);
	float3 u = f * f * (3.0 - 2.0 * f);

	float3 v000 = RandomNoise_3_3_Float(p + float3(0, 0, 0));
	float3 v100 = RandomNoise_3_3_Float(p + float3(1, 0, 0));
	float3 v010 = RandomNoise_3_3_Float(p + float3(0, 1, 0));
	float3 v110 = RandomNoise_3_3_Float(p + float3(1, 1, 0));

	float3 v001 = RandomNoise_3_3_Float(p + float3(0, 0, 1));
	float3 v101 = RandomNoise_3_3_Float(p + float3(1, 0, 1));
	float3 v011 = RandomNoise_3_3_Float(p + float3(0, 1, 1));
	float3 v111 = RandomNoise_3_3_Float(p + float3(1, 1, 1));

	float noise1 = lerp(
		lerp(dot(v000, f - float3(0, 0, 0)), dot(v100, f - float3(1, 0, 0)), u.x),
		lerp(dot(v010, f - float3(0, 1, 0)), dot(v110, f - float3(1, 1, 0)), u.x),
		u.y
	);
	float noise2 = lerp(
		lerp(dot(v001, f - float3(0, 0, 1)), dot(v101, f - float3(1, 0, 1)), u.x),
		lerp(dot(v011, f - float3(0, 1, 1)), dot(v111, f - float3(1, 1, 1)), u.x),
		u.y
	);
	return lerp(noise1, noise2, u.z) + 0.5f;
}

inline float3 mod289(float3 x)
{
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}

inline float4 mod289(float4 x)
{
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}

inline float4 permute(float4 x)
{
	return mod289(((x * 34.0) + 1.0) * x);
}

inline float4 taylorInvSqrt(float4 r)
{
	return 1.79284291400159 - 0.85373472095314 * r;
}

float SimplexNoise_3_1_Float(float3 v)
{
	const float2  C = float2(1.0 / 6.0, 1.0 / 3.0);
	const float4  D = float4(0.0, 0.5, 1.0, 2.0);

	// First corner
	float3 i = floor(v + dot(v, C.yyy));
	float3 x0 = v - i + dot(i, C.xxx);

	// Other corners
	float3 g = step(x0.yzx, x0.xyz);
	float3 l = 1.0 - g;
	float3 i1 = min(g.xyz, l.zxy);
	float3 i2 = max(g.xyz, l.zxy);

	//   x0 = x0 - 0.0 + 0.0 * C.xxx;
	//   x1 = x0 - i1  + 1.0 * C.xxx;
	//   x2 = x0 - i2  + 2.0 * C.xxx;
	//   x3 = x0 - 1.0 + 3.0 * C.xxx;
	float3 x1 = x0 - i1 + C.xxx;
	float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
	float3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

	// Permutations
	i = mod289(i);
	float4 p = permute(permute(permute(
		i.z + float4(0.0, i1.z, i2.z, 1.0))
		+ i.y + float4(0.0, i1.y, i2.y, 1.0))
		+ i.x + float4(0.0, i1.x, i2.x, 1.0));

	// Gradients: 7x7 points over a square, mapped onto an octahedron.
	// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
	float n_ = 0.142857142857; // 1.0/7.0
	float3  ns = n_ * D.wyz - D.xzx;

	float4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

	float4 x_ = floor(j * ns.z);
	float4 y_ = floor(j - 7.0 * x_);    // mod(j,N)

	float4 x = x_ * ns.x + ns.yyyy;
	float4 y = y_ * ns.x + ns.yyyy;
	float4 h = 1.0 - abs(x) - abs(y);

	float4 b0 = float4(x.xy, y.xy);
	float4 b1 = float4(x.zw, y.zw);

	//float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
	//float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
	float4 s0 = floor(b0) * 2.0 + 1.0;
	float4 s1 = floor(b1) * 2.0 + 1.0;
	float4 sh = -step(h, 0.0);

	float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
	float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

	float3 p0 = float3(a0.xy, h.x);
	float3 p1 = float3(a0.zw, h.y);
	float3 p2 = float3(a1.xy, h.z);
	float3 p3 = float3(a1.zw, h.w);

	//Normalise gradients
	float4 norm = taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
	p0 *= norm.x;
	p1 *= norm.y;
	p2 *= norm.z;
	p3 *= norm.w;

	// Mix final noise value
	float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
	m = m * m;
	return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1),
		dot(p2, x2), dot(p3, x3)));
}
