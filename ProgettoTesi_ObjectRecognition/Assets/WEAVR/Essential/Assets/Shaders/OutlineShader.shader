// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/WEAVROutlineEffect"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}

	}
		SubShader
	{
		Pass
		{
			Tags{ "RenderType" = "Opaque" }
			LOD 200
			ZTest Always
			ZWrite Off
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _OutlineSource;

			struct v2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_img v)
			{
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;

				return o;
			}

			float _LineThicknessX;
			float _LineThicknessY;
			int _FlipY;
			uniform float4 _MainTex_TexelSize;

			half4 frag(v2f input) : COLOR
			{
				float2 uv = input.uv;
				if (_FlipY == 1)
					uv.y = uv.y;
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					uv.y = 1 - uv.y;
				#endif

				half4 outlineSource = tex2D(_OutlineSource, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));

				return outlineSource;
			}

			ENDCG
		}

		Pass
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200
			ZTest Always
			ZWrite Off
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _OutlineSource;

			struct v2f {
			   float4 position : SV_POSITION;
			   float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_img v)
			{
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;

				return o;
			}

			float _LineThicknessX;
			float _LineThicknessY;
			float _LineIntensity;
			int _FlipY;
			int _Dark;
			float _FillAmount;
			int _CornerOutlines;
			int _CornerPrecision;
			uniform float4 _MainTex_TexelSize;

			half4 frag(v2f input) : COLOR
			{
				float2 uv = input.uv;
				if (_FlipY == 1)
					uv.y = 1 - uv.y;
				#if UNITY_UV_STARTS_AT_TOP
					if (_MainTex_TexelSize.y < 0)
						uv.y = 1 - uv.y;
				#endif

				half4 originalPixel = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(input.uv, _MainTex_ST));
				half4 outlineSource = tex2D(_OutlineSource, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));

				const float h = .95f;
				const float b = .95f;
				half4 outline = 0;
				bool hasOutline = false;

				half4 sample1 = tex2D(_OutlineSource, uv + float2(_LineThicknessX,0.0));
				half4 sample2 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX,0.0));
				half4 sample3 = tex2D(_OutlineSource, uv + float2(.0,_LineThicknessY));
				half4 sample4 = tex2D(_OutlineSource, uv + float2(.0,-_LineThicknessY));

				bool outside = outlineSource.a < h;
				bool outsideDark = outside && _Dark;

				if (outside) {
					if (sample1.a > b) {
						outline = sample1 * _LineIntensity;
						if (outsideDark)
							originalPixel *= 1 - sample1.a;
						hasOutline = true;
					}
					else if (sample2.a > b) {
						outline = sample2 * _LineIntensity;
						if (outsideDark)
							originalPixel *= 1 - sample2.a;
						hasOutline = true;
					}
					else if (sample3.a > b) {
						outline = sample3 * _LineIntensity;
						if (outsideDark)
							originalPixel *= 1 - sample3.a;
						hasOutline = true;
					}
					else if (sample4.a > b) {
						outline = sample4 * _LineIntensity;
						if (outsideDark)
							originalPixel *= 1 - sample4.a;
						hasOutline = true;
					}
					else if (_CornerOutlines) {
						sample1 = tex2D(_OutlineSource, uv + float2(_LineThicknessX, _LineThicknessY));
						sample2 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX, -_LineThicknessY));
						sample3 = tex2D(_OutlineSource, uv + float2(_LineThicknessX, -_LineThicknessY));
						sample4 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX, _LineThicknessY));

						if (sample1.a > b) {
							outline = sample1 * _LineIntensity;
							if (outsideDark)
								originalPixel *= 1 - sample1.a;
							hasOutline = true;
						}
						else if (sample2.a > b) {
							outline = sample2 * _LineIntensity;
							if (outsideDark)
								originalPixel *= 1 - sample2.a;
							hasOutline = true;
						}
						else if (sample3.a > b) {
							outline = sample3 * _LineIntensity;
							if (outsideDark)
								originalPixel *= 1 - sample3.a;
							hasOutline = true;
						}
						else if (sample4.a > b) {
							outline = sample4 * _LineIntensity;
							if (outsideDark)
								originalPixel *= 1 - sample4.a;
							hasOutline = true;
						}
						else if (_CornerPrecision) {
							sample1 = tex2D(_OutlineSource, uv + float2(_LineThicknessX * 0.5, _LineThicknessY));
							sample2 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX * 0.5, -_LineThicknessY));
							sample3 = tex2D(_OutlineSource, uv + float2(_LineThicknessX, -_LineThicknessY * 0.5));
							sample4 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX, _LineThicknessY * 0.5));

							if (sample1.a > b) {
								outline = sample1 * _LineIntensity;
								if (outsideDark)
									originalPixel *= 1 - sample1.a;
								hasOutline = true;
							}
							else if (sample2.a > b) {
								outline = sample2 * _LineIntensity;
								if (outsideDark)
									originalPixel *= 1 - sample2.a;
								hasOutline = true;
							}
							else if (sample3.a > b) {
								outline = sample3 * _LineIntensity;
								if (outsideDark)
									originalPixel *= 1 - sample3.a;
								hasOutline = true;
							}
							else if (sample4.a > b) {
								outline = sample4 * _LineIntensity;
								if (outsideDark)
									originalPixel *= 1 - sample4.a;
								hasOutline = true;
							}
							else {
								sample1 = tex2D(_OutlineSource, uv + float2(_LineThicknessX, _LineThicknessY * 0.5));
								sample2 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX, -_LineThicknessY * 0.5));
								sample3 = tex2D(_OutlineSource, uv + float2(_LineThicknessX * 0.5, -_LineThicknessY));
								sample4 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX * 0.5, _LineThicknessY));

								if (sample1.a > b) {
									outline = sample1 * _LineIntensity;
									if (outsideDark)
										originalPixel *= 1 - sample1.a;
									hasOutline = true;
								}
								else if (sample2.a > b) {
									outline = sample2 * _LineIntensity;
									if (outsideDark)
										originalPixel *= 1 - sample2.a;
									hasOutline = true;
								}
								else if (sample3.a > b) {
									outline = sample3 * _LineIntensity;
									if (outsideDark)
										originalPixel *= 1 - sample3.a;
									hasOutline = true;
								}
								else if (sample4.a > b) {
									outline = sample4 * _LineIntensity;
									if (outsideDark)
										originalPixel *= 1 - sample4.a;
									hasOutline = true;
								}
							}
						}
					}
				}
				else {
					outline = outlineSource * _LineIntensity * _FillAmount;
					hasOutline = true;
				}

				//return outlineSource;		
				if (hasOutline)
					//return outline;
					return lerp(originalPixel + outline, outline, _FillAmount);
				else
					return originalPixel;
			}

			ENDCG
		}
	}

		FallBack "Diffuse"
}