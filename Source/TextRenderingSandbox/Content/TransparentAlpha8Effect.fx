#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

float4 TransparentAlpha8(VertexShaderInput input) : COLOR
{
	float4 pixel = tex2D(SpriteTextureSampler, input.TexCoord);
	return float4(1, 1, 1, pixel.x) * input.Color;
}

technique Main
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL TransparentAlpha8();
	}
};