/*
struct appdata_full {
	float4 vertex : POSITION;
	float4 tangent : TANGENT;
	float3 normal : NORMAL;
	float4 texcoord : TEXCOORD0;
	float4 texcoord1 : TEXCOORD1;
	fixed4 color : COLOR;
}
*/
Shader "Snail/Marker"
{
	Properties
	{
		[KeywordEnum(Solid, Gradient, Texture)] _Mode ("Color Mode", Float) = 0
		_Color("Solid Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
		_Invisible("Invisible Length", Float) = .1
	}

	SubShader
	{
		Tags {
			"IgnoreProjector" = "True"
			"Queue"="Transparent"
			"RenderType"="Transparent"
		}
		Cull Off
		LOD 200
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma multi_compile _MODE_SOLID _MODE_GRADIENT _MODE_TEXTURE

			#pragma target 4.0
			#include "UnityCG.cginc" 

			#pragma vertex VS_Main
			#pragma geometry GS_Main
			#pragma fragment FS_Main


			appdata_full VS_Main(appdata_full v)
			{
				v.vertex = UnityObjectToClipPos(v.vertex);
				return v;
			}

			uniform float _Invisible;
			[maxvertexcount(3)]
			void GS_Main(triangle appdata_full p[3], inout TriangleStream<appdata_full> triStream)
			{
				float d = distance(p[0].vertex, p[2].vertex);
				if (d > _Invisible) return;

				triStream.Append(p[0]);
				triStream.Append(p[1]);
				triStream.Append(p[2]);
			}
			
			uniform float4 _Color;
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;

			float4 FS_Main(appdata_full input) : COLOR
			{
				#ifdef _MODE_SOLID
				return _Color;
				#endif

				#ifdef _MODE_GRADIENT
				return input.color;
				#endif

				#ifdef _MODE_TEXTURE
				return tex2D(_MainTex, TRANSFORM_TEX(input.texcoord, _MainTex));
				#endif
			}

			ENDCG
		}
	}

	CustomEditor "SnailMarkerEditor"
}
