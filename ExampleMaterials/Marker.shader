Shader "Skuld/Skuld's Marker"
{
    Properties
    {
		[HDR]_Color("Color",Color) = (.5,0,1,1)
		[KeywordEnum(TrailColor,SolidColor,Rainbow)] _Mode("Mode:", Int) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		cull Off

        Pass
        {
            CGPROGRAM	
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"
			#include "UnityPBSLighting.cginc"

			#pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile

            #include "UnityCG.cginc"
			float4 _Color;
			int _Mode;
			sampler2D _MainTex;


            struct appdata
            {
                float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 color : VCOLOR;
			};

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
                return o;
            }

			float3 shiftColor(float3 inColor, float shift)
			{
				float r = shift * 0.01745329251994329576923690768489;
				float u = cos(r);
				float w = sin(r);
				float3 ret;
				ret.r = (.299 + .701 * u + .168 * w)*inColor.r
					+ (.587 - .587 * u + .330 * w)*inColor.g
					+ (.114 - .114 * u - .497 * w)*inColor.b;
				ret.g = (.299 - .299 * u - .328 * w)*inColor.r
					+ (.587 + .413 * u + .035 * w)*inColor.g
					+ (.114 - .114 * u + .292 * w)*inColor.b;
				ret.b = (.299 - .3 * u + 1.25 * w)*inColor.r
					+ (.587 - .588 * u - 1.05 * w)*inColor.g
					+ (.114 + .886 * u - .203 * w)*inColor.b;
				return ret;
			}

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
				float4 col = _Color;
				switch (_Mode) {
					case 0:
						col.rgb = i.color;
						break;
					case 1:
						col = _Color;
						break;
					case 2:
						col.rgb = float3(1, 0, 0);
						float n = (i.uv.x * 2000.0f) + (_Time.z * 25.0f);
						col.rgb = shiftColor(col.rgb, n);
						break;
				}

                return col;
            }
            ENDCG
        }
    }
}
