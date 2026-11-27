Shader "PlanetIO/Background Vector Gradient"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[Enum(UnityEngine.Rendering.BlendMode)] _DestinationBlend ("Destination Blend", Float) = 10
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
		Cull Off
		ZWrite Off
		Blend One [_DestinationBlend], One OneMinusSrcAlpha
		Pass
		{
			Tags { "LightMode" = "SRPDefaultUnlit" }
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			// Use the Vector Graphics atlas decoder from a URP HLSL pass.
			#define fixed4 float4
			#include "Packages/com.unity.vectorgraphics/Runtime/Shaders/VectorGradient.cginc"
			#undef fixed4

			struct Attributes
			{
				float3 positionOS : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float2 settingIndex : TEXCOORD2;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float settingIndex : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float4 _Color;

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS);
				output.color = input.color;
				#ifndef UNITY_COLORSPACE_GAMMA
				output.color.rgb = SRGBToLinear(output.color.rgb);
				#endif
				output.uv = input.uv;
				output.settingIndex = input.settingIndex.x;
				return output;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float4 gradient = EvaluateGradient(input.settingIndex, input.uv, _MainTex, _MainTex_TexelSize.xy);
				#ifndef UNITY_COLORSPACE_GAMMA
				gradient.rgb = SRGBToLinear(gradient.rgb);
				#endif
				float4 color = gradient * input.color * _Color;
				return float4(color.rgb * color.a, color.a);
			}
			ENDHLSL
		}
	}
}
