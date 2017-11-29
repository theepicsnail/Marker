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
		_Color("Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Pass
		{
			Tags{ "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
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
			


			float4 _Color;
			[maxvertexcount(3)]
			void GS_Main(triangle appdata_full p[3], inout TriangleStream<appdata_full> triStream)
			{
				/*// Vector between each of the 3 points and the camera.
				float3 p1 = _WorldSpaceCameraPos.xyz - p[0].vertex;
				float3 p2 = _WorldSpaceCameraPos.xyz - p[1].vertex;
				float3 p3 = _WorldSpaceCameraPos.xyz - p[2].vertex;

				// Max distance(squared) from camera to a point on the triangle
				float maxD2 = max(max(dot(p1, p1), dot(p2, p2)), dot(p3, p3));

				// Skip the triangle if it's too far away.
				if (maxD2 > 100) // 100=10^2 (jump of more than 10m) 
					return;
					*/
				float tmp = length(p[0].vertex - p[1].vertex);
				if (tmp > 10) return;
				tmp = length(p[0].vertex - p[2].vertex);
				if (tmp > 10) return;

				triStream.Append(p[0]);
				triStream.Append(p[1]);
				triStream.Append(p[2]);
			}
			
			float4 FS_Main(appdata_full input) : COLOR
			{
				return _Color;// input.color;
			}

			ENDCG
		}
	}
}
