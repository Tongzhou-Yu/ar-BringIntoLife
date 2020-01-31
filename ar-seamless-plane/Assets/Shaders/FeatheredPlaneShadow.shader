// (c) 2020 Tongzhou Yu
// Ref. https://forum.unity.com/threads/transparent-shader-receive-shadows.325877/

Shader "Custom/FeatheredPlaneShadow"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_TexTintColor("Texture Tint Color", Color) = (1,1,1,1)
		_PlaneColor("Plane Color", Color) = (1,1,1,1)
		_ShadowIntensity("Shadow Intensity", Range(0, 1)) = 0.6
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Geometry" }
			LOD 100
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			
			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float3 uv2 : TEXCOORD1;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float3 uv2 : TEXCOORD1;
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				fixed4 _TexTintColor;
				fixed4 _PlaneColor;
				float _ShortestUVMapping;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					o.uv2 = v.uv2;
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					fixed4 col = tex2D(_MainTex, i.uv) * _TexTintColor;
					col = lerp(_PlaneColor, col, col.a);
					// Fade out from as we pass the edge.
					// uv2.x stores a mapped UV that will be "1" at the beginning of the feathering.
					// We fade until we reach at the edge of the shortest UV mapping.
					// This is the remmaped UV value at the vertex.
					// We choose the shorted one so that ll edges will fade out completely.
					// See ARFeatheredPlaneMeshVisualizer.cs for more details.
					col.a *= 1 - smoothstep(1, _ShortestUVMapping, i.uv2.x);
					return col;
				}
				ENDCG
			}
			Pass
			{
			Tags {"LightMode" = "ForwardBase" }
			Cull Back
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			uniform fixed4  _PlaneColor;
			uniform float _ShadowIntensity;

			struct v2f
			{
				float4 pos : SV_POSITION;
				LIGHTING_COORDS(0,1)
			};
			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			}
			fixed4 frag(v2f i) : COLOR
			{
				float attenuation = LIGHT_ATTENUATION(i);
				return fixed4(0,0,0,(1 - attenuation)*_ShadowIntensity) * _PlaneColor;
			}
			ENDCG
		}

		}
}

