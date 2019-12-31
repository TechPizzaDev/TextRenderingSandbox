#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D DistanceFieldTexture;
sampler2D DistanceFieldTextureSampler = sampler_state
{
	Texture = <DistanceFieldTexture>;
};

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

uniform float Smoothing;

float4 Main(VertexShaderInput input) : COLOR
{
	float distance = tex2D(DistanceFieldTextureSampler, input.TexCoord).x;
	float alpha = smoothstep(0.5 - Smoothing, 0.5 + Smoothing, distance);
	return float4(input.Color.xyz, input.Color.w * alpha);
}

technique Main
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL Main();
	}
};