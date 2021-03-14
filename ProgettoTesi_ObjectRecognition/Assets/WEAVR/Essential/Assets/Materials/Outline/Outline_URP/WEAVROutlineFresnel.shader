Shader "Unlit/WEAVROutlineFresnel"
{
	Properties
	{
		_Color("OutlineColor", Color) = (0, 0.8334823, 1, 1)
		_ScaleFactor("ScaleFactor", Range(0, 2)) = 0.5
		_MinRangeOutline("MinOutlineScalePerc", Range(0, 1)) = 0.5
		_TimeRate("TimeRate", Float) = 3
		_AlphaThreshold("AlphaThreshold", Range(-0.1, 1)) = 0
		_FresnelPower("FresnelPower", Float) = 1
	}
		SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent+0"
		}

		Pass
		{
			Name "Pass"
			Tags
			{
		// LightMode: <None>
	}

	// Render State
	Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
	Cull Front
	ZTest LEqual
	ZWrite Off
		// ColorMask: <None>


		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag

		// Debug
		// <None>

		// --------------------------------------------------
		// Pass

		// Pragmas
		#pragma prefer_hlslcc gles
		#pragma exclude_renderers d3d11_9x
		#pragma target 2.0
		#pragma multi_compile_fog
		#pragma multi_compile_instancing

		// Keywords
		#pragma multi_compile _ LIGHTMAP_ON
		#pragma multi_compile _ DIRLIGHTMAP_COMBINED
		#pragma shader_feature _ _SAMPLE_GI
		// GraphKeywords: <None>

		// Defines
		#define _SURFACE_TYPE_TRANSPARENT 1
		#define _AlphaClip 1
		#define ATTRIBUTES_NEED_NORMAL
		#define ATTRIBUTES_NEED_TANGENT
		#define VARYINGS_NEED_NORMAL_WS
		#define VARYINGS_NEED_VIEWDIRECTION_WS
		#define FEATURES_GRAPH_VERTEX
		#define SHADERPASS_UNLIT

		// Includes
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

		// --------------------------------------------------
		// Graph

		// Graph Properties
		CBUFFER_START(UnityPerMaterial)
		float4 _Color;
		float _ScaleFactor;
		float _MinRangeOutline;
		float _TimeRate;
		float _AlphaThreshold;
		float _FresnelPower;
		CBUFFER_END

			// Graph Functions

			void Unity_Divide_float(float A, float B, out float Out)
			{
				Out = A / B;
			}

			void Unity_Multiply_float(float A, float B, out float Out)
			{
				Out = A * B;
			}

			void Unity_Sine_float(float In, out float Out)
			{
				Out = sin(In);
			}

			void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
			{
				Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
			}

			void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
			{
				Out = A * B;
			}

			void Unity_Add_float3(float3 A, float3 B, out float3 Out)
			{
				Out = A + B;
			}

			void Unity_InvertColors_float3(float3 In, float3 InvertColors, out float3 Out)
			{
				Out = abs(InvertColors - In);
			}

			void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
			{
				Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
			}

			// Graph Vertex
			struct VertexDescriptionInputs
			{
				float3 ObjectSpaceNormal;
				float3 ObjectSpaceTangent;
				float3 ObjectSpacePosition;
				float3 TimeParameters;
			};

			struct VertexDescription
			{
				float3 VertexPosition;
				float3 VertexNormal;
				float3 VertexTangent;
			};

			VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				float _Property_593E0AEE_Out_0 = _ScaleFactor;
				float _Divide_3F46DC63_Out_2;
				Unity_Divide_float(_Property_593E0AEE_Out_0, 100, _Divide_3F46DC63_Out_2);
				float _Property_49DC9932_Out_0 = _TimeRate;
				float _Vector1_A2503412_Out_0 = _Property_49DC9932_Out_0;
				float _Multiply_895424B1_Out_2;
				Unity_Multiply_float(IN.TimeParameters.x, _Vector1_A2503412_Out_0, _Multiply_895424B1_Out_2);
				float _Sine_C93A939E_Out_1;
				Unity_Sine_float(_Multiply_895424B1_Out_2, _Sine_C93A939E_Out_1);
				float _Property_A80C5127_Out_0 = _MinRangeOutline;
				float2 _Vector2_12BCA4D6_Out_0 = float2(_Property_A80C5127_Out_0, 1);
				float _Remap_DD603FD5_Out_3;
				Unity_Remap_float(_Sine_C93A939E_Out_1, float2 (-1, 1), _Vector2_12BCA4D6_Out_0, _Remap_DD603FD5_Out_3);
				float _Multiply_5B283A78_Out_2;
				Unity_Multiply_float(_Divide_3F46DC63_Out_2, _Remap_DD603FD5_Out_3, _Multiply_5B283A78_Out_2);
				float3 _Multiply_67A1EB1F_Out_2;
				Unity_Multiply_float(IN.ObjectSpaceNormal, (_Multiply_5B283A78_Out_2.xxx), _Multiply_67A1EB1F_Out_2);
				float3 _Add_D20BB7A1_Out_2;
				Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_67A1EB1F_Out_2, _Add_D20BB7A1_Out_2);
				description.VertexPosition = _Add_D20BB7A1_Out_2;
				description.VertexNormal = IN.ObjectSpaceNormal;
				description.VertexTangent = IN.ObjectSpaceTangent;
				return description;
			}

			// Graph Pixel
			struct SurfaceDescriptionInputs
			{
				float3 ObjectSpaceNormal;
				float3 WorldSpaceNormal;
				float3 ObjectSpaceViewDirection;
				float3 WorldSpaceViewDirection;
			};

			struct SurfaceDescription
			{
				float3 Color;
				float Alpha;
				float AlphaClipThreshold;
			};

			SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
				float4 _Property_25BAC21C_Out_0 = _Color;
				float3 _InvertColors_BB498680_Out_1;
				float3 _InvertColors_BB498680_InvertColors = float3 (0
			, 0, 0);    Unity_InvertColors_float3(IN.ObjectSpaceNormal, _InvertColors_BB498680_InvertColors, _InvertColors_BB498680_Out_1);
				float _Property_67FE6B44_Out_0 = _FresnelPower;
				float _FresnelEffect_D6FC9FEE_Out_3;
				Unity_FresnelEffect_float(_InvertColors_BB498680_Out_1, IN.ObjectSpaceViewDirection, _Property_67FE6B44_Out_0, _FresnelEffect_D6FC9FEE_Out_3);
				float _Property_6FE5E7CC_Out_0 = _AlphaThreshold;
				surface.Color = (_Property_25BAC21C_Out_0.xyz);
				surface.Alpha = _FresnelEffect_D6FC9FEE_Out_3;
				surface.AlphaClipThreshold = _Property_6FE5E7CC_Out_0;
				return surface;
			}

			// --------------------------------------------------
			// Structs and Packing

			// Generated Type: Attributes
			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				#if UNITY_ANY_INSTANCING_ENABLED
				uint instanceID : INSTANCEID_SEMANTIC;
				#endif
			};

			// Generated Type: Varyings
			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS;
				float3 viewDirectionWS;
				#if UNITY_ANY_INSTANCING_ENABLED
				uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
				uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
				uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
				FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			// Generated Type: PackedVaryings
			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				#if UNITY_ANY_INSTANCING_ENABLED
				uint instanceID : CUSTOM_INSTANCE_ID;
				#endif
				float3 interp00 : TEXCOORD0;
				float3 interp01 : TEXCOORD1;
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
				uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
				uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
				FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

			// Packed Type: Varyings
			PackedVaryings PackVaryings(Varyings input)
			{
				PackedVaryings output = (PackedVaryings)0;
				output.positionCS = input.positionCS;
				output.interp00.xyz = input.normalWS;
				output.interp01.xyz = input.viewDirectionWS;
				#if UNITY_ANY_INSTANCING_ENABLED
				output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
				output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
				output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
				output.cullFace = input.cullFace;
				#endif
				return output;
			}

			// Unpacked Type: Varyings
			Varyings UnpackVaryings(PackedVaryings input)
			{
				Varyings output = (Varyings)0;
				output.positionCS = input.positionCS;
				output.normalWS = input.interp00.xyz;
				output.viewDirectionWS = input.interp01.xyz;
				#if UNITY_ANY_INSTANCING_ENABLED
				output.instanceID = input.instanceID;
				#endif
				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
				output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
				output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
				output.cullFace = input.cullFace;
				#endif
				return output;
			}

			// --------------------------------------------------
			// Build Graph Inputs

			VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
			{
				VertexDescriptionInputs output;
				ZERO_INITIALIZE(VertexDescriptionInputs, output);

				output.ObjectSpaceNormal = input.normalOS;
				output.ObjectSpaceTangent = input.tangentOS;
				output.ObjectSpacePosition = input.positionOS;
				output.TimeParameters = _TimeParameters.xyz;

				return output;
			}

			SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
			{
				SurfaceDescriptionInputs output;
				ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

				// must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
				float3 unnormalizedNormalWS = input.normalWS;
				const float renormFactor = 1.0 / length(unnormalizedNormalWS);


				output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;		// we want a unit length Normal Vector node in shader graph
				output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale


				output.WorldSpaceViewDirection = input.viewDirectionWS; //TODO: by default normalized in HD, but not in universal
				output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
			#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
			#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
			#else
			#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
			#endif
			#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

				return output;
			}


			// --------------------------------------------------
			// Main

			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

				// Render State
				Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
				Cull Back
				ZTest LEqual
				ZWrite On
				// ColorMask: <None>


				HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				// Debug
				// <None>

				// --------------------------------------------------
				// Pass

				// Pragmas
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x
				#pragma target 2.0
				#pragma multi_compile_instancing

				// Keywords
				#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
				// GraphKeywords: <None>

				// Defines
				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _AlphaClip 1
				#define ATTRIBUTES_NEED_NORMAL
				#define ATTRIBUTES_NEED_TANGENT
				#define VARYINGS_NEED_NORMAL_WS
				#define VARYINGS_NEED_VIEWDIRECTION_WS
				#define FEATURES_GRAPH_VERTEX
				#define SHADERPASS_SHADOWCASTER

				// Includes
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
				#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

				// --------------------------------------------------
				// Graph

				// Graph Properties
				CBUFFER_START(UnityPerMaterial)
				float4 _Color;
				float _ScaleFactor;
				float _MinRangeOutline;
				float _TimeRate;
				float _AlphaThreshold;
				float _FresnelPower;
				CBUFFER_END

					// Graph Functions

					void Unity_Divide_float(float A, float B, out float Out)
					{
						Out = A / B;
					}

					void Unity_Multiply_float(float A, float B, out float Out)
					{
						Out = A * B;
					}

					void Unity_Sine_float(float In, out float Out)
					{
						Out = sin(In);
					}

					void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
					{
						Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
					}

					void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
					{
						Out = A * B;
					}

					void Unity_Add_float3(float3 A, float3 B, out float3 Out)
					{
						Out = A + B;
					}

					void Unity_InvertColors_float3(float3 In, float3 InvertColors, out float3 Out)
					{
						Out = abs(InvertColors - In);
					}

					void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
					{
						Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
					}

					// Graph Vertex
					struct VertexDescriptionInputs
					{
						float3 ObjectSpaceNormal;
						float3 ObjectSpaceTangent;
						float3 ObjectSpacePosition;
						float3 TimeParameters;
					};

					struct VertexDescription
					{
						float3 VertexPosition;
						float3 VertexNormal;
						float3 VertexTangent;
					};

					VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
					{
						VertexDescription description = (VertexDescription)0;
						float _Property_593E0AEE_Out_0 = _ScaleFactor;
						float _Divide_3F46DC63_Out_2;
						Unity_Divide_float(_Property_593E0AEE_Out_0, 100, _Divide_3F46DC63_Out_2);
						float _Property_49DC9932_Out_0 = _TimeRate;
						float _Vector1_A2503412_Out_0 = _Property_49DC9932_Out_0;
						float _Multiply_895424B1_Out_2;
						Unity_Multiply_float(IN.TimeParameters.x, _Vector1_A2503412_Out_0, _Multiply_895424B1_Out_2);
						float _Sine_C93A939E_Out_1;
						Unity_Sine_float(_Multiply_895424B1_Out_2, _Sine_C93A939E_Out_1);
						float _Property_A80C5127_Out_0 = _MinRangeOutline;
						float2 _Vector2_12BCA4D6_Out_0 = float2(_Property_A80C5127_Out_0, 1);
						float _Remap_DD603FD5_Out_3;
						Unity_Remap_float(_Sine_C93A939E_Out_1, float2 (-1, 1), _Vector2_12BCA4D6_Out_0, _Remap_DD603FD5_Out_3);
						float _Multiply_5B283A78_Out_2;
						Unity_Multiply_float(_Divide_3F46DC63_Out_2, _Remap_DD603FD5_Out_3, _Multiply_5B283A78_Out_2);
						float3 _Multiply_67A1EB1F_Out_2;
						Unity_Multiply_float(IN.ObjectSpaceNormal, (_Multiply_5B283A78_Out_2.xxx), _Multiply_67A1EB1F_Out_2);
						float3 _Add_D20BB7A1_Out_2;
						Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_67A1EB1F_Out_2, _Add_D20BB7A1_Out_2);
						description.VertexPosition = _Add_D20BB7A1_Out_2;
						description.VertexNormal = IN.ObjectSpaceNormal;
						description.VertexTangent = IN.ObjectSpaceTangent;
						return description;
					}

					// Graph Pixel
					struct SurfaceDescriptionInputs
					{
						float3 ObjectSpaceNormal;
						float3 WorldSpaceNormal;
						float3 ObjectSpaceViewDirection;
						float3 WorldSpaceViewDirection;
					};

					struct SurfaceDescription
					{
						float Alpha;
						float AlphaClipThreshold;
					};

					SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
					{
						SurfaceDescription surface = (SurfaceDescription)0;
						float3 _InvertColors_BB498680_Out_1;
						float3 _InvertColors_BB498680_InvertColors = float3 (0
					, 0, 0);    Unity_InvertColors_float3(IN.ObjectSpaceNormal, _InvertColors_BB498680_InvertColors, _InvertColors_BB498680_Out_1);
						float _Property_67FE6B44_Out_0 = _FresnelPower;
						float _FresnelEffect_D6FC9FEE_Out_3;
						Unity_FresnelEffect_float(_InvertColors_BB498680_Out_1, IN.ObjectSpaceViewDirection, _Property_67FE6B44_Out_0, _FresnelEffect_D6FC9FEE_Out_3);
						float _Property_6FE5E7CC_Out_0 = _AlphaThreshold;
						surface.Alpha = _FresnelEffect_D6FC9FEE_Out_3;
						surface.AlphaClipThreshold = _Property_6FE5E7CC_Out_0;
						return surface;
					}

					// --------------------------------------------------
					// Structs and Packing

					// Generated Type: Attributes
					struct Attributes
					{
						float3 positionOS : POSITION;
						float3 normalOS : NORMAL;
						float4 tangentOS : TANGENT;
						#if UNITY_ANY_INSTANCING_ENABLED
						uint instanceID : INSTANCEID_SEMANTIC;
						#endif
					};

					// Generated Type: Varyings
					struct Varyings
					{
						float4 positionCS : SV_POSITION;
						float3 normalWS;
						float3 viewDirectionWS;
						#if UNITY_ANY_INSTANCING_ENABLED
						uint instanceID : CUSTOM_INSTANCE_ID;
						#endif
						#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
						uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
						#endif
						#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
						uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
						#endif
						#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
						FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
						#endif
					};

					// Generated Type: PackedVaryings
					struct PackedVaryings
					{
						float4 positionCS : SV_POSITION;
						#if UNITY_ANY_INSTANCING_ENABLED
						uint instanceID : CUSTOM_INSTANCE_ID;
						#endif
						float3 interp00 : TEXCOORD0;
						float3 interp01 : TEXCOORD1;
						#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
						uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
						#endif
						#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
						uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
						#endif
						#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
						FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
						#endif
					};

					// Packed Type: Varyings
					PackedVaryings PackVaryings(Varyings input)
					{
						PackedVaryings output = (PackedVaryings)0;
						output.positionCS = input.positionCS;
						output.interp00.xyz = input.normalWS;
						output.interp01.xyz = input.viewDirectionWS;
						#if UNITY_ANY_INSTANCING_ENABLED
						output.instanceID = input.instanceID;
						#endif
						#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
						output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
						#endif
						#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
						output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
						#endif
						#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
						output.cullFace = input.cullFace;
						#endif
						return output;
					}

					// Unpacked Type: Varyings
					Varyings UnpackVaryings(PackedVaryings input)
					{
						Varyings output = (Varyings)0;
						output.positionCS = input.positionCS;
						output.normalWS = input.interp00.xyz;
						output.viewDirectionWS = input.interp01.xyz;
						#if UNITY_ANY_INSTANCING_ENABLED
						output.instanceID = input.instanceID;
						#endif
						#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
						output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
						#endif
						#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
						output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
						#endif
						#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
						output.cullFace = input.cullFace;
						#endif
						return output;
					}

					// --------------------------------------------------
					// Build Graph Inputs

					VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
					{
						VertexDescriptionInputs output;
						ZERO_INITIALIZE(VertexDescriptionInputs, output);

						output.ObjectSpaceNormal = input.normalOS;
						output.ObjectSpaceTangent = input.tangentOS;
						output.ObjectSpacePosition = input.positionOS;
						output.TimeParameters = _TimeParameters.xyz;

						return output;
					}

					SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
					{
						SurfaceDescriptionInputs output;
						ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

						// must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
						float3 unnormalizedNormalWS = input.normalWS;
						const float renormFactor = 1.0 / length(unnormalizedNormalWS);


						output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;		// we want a unit length Normal Vector node in shader graph
						output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale


						output.WorldSpaceViewDirection = input.viewDirectionWS; //TODO: by default normalized in HD, but not in universal
						output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
					#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
					#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
					#else
					#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
					#endif
					#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

						return output;
					}


					// --------------------------------------------------
					// Main

					#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

					ENDHLSL
				}

				Pass
				{
					Name "DepthOnly"
					Tags
					{
						"LightMode" = "DepthOnly"
					}

						// Render State
						Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
						Cull Back
						ZTest LEqual
						ZWrite On
						ColorMask 0


						HLSLPROGRAM
						#pragma vertex vert
						#pragma fragment frag

						// Debug
						// <None>

						// --------------------------------------------------
						// Pass

						// Pragmas
						#pragma prefer_hlslcc gles
						#pragma exclude_renderers d3d11_9x
						#pragma target 2.0
						#pragma multi_compile_instancing

						// Keywords
						// PassKeywords: <None>
						// GraphKeywords: <None>

						// Defines
						#define _SURFACE_TYPE_TRANSPARENT 1
						#define _AlphaClip 1
						#define ATTRIBUTES_NEED_NORMAL
						#define ATTRIBUTES_NEED_TANGENT
						#define VARYINGS_NEED_NORMAL_WS
						#define VARYINGS_NEED_VIEWDIRECTION_WS
						#define FEATURES_GRAPH_VERTEX
						#define SHADERPASS_DEPTHONLY

						// Includes
						#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
						#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
						#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
						#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
						#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

						// --------------------------------------------------
						// Graph

						// Graph Properties
						CBUFFER_START(UnityPerMaterial)
						float4 _Color;
						float _ScaleFactor;
						float _MinRangeOutline;
						float _TimeRate;
						float _AlphaThreshold;
						float _FresnelPower;
						CBUFFER_END

							// Graph Functions

							void Unity_Divide_float(float A, float B, out float Out)
							{
								Out = A / B;
							}

							void Unity_Multiply_float(float A, float B, out float Out)
							{
								Out = A * B;
							}

							void Unity_Sine_float(float In, out float Out)
							{
								Out = sin(In);
							}

							void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
							{
								Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
							}

							void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
							{
								Out = A * B;
							}

							void Unity_Add_float3(float3 A, float3 B, out float3 Out)
							{
								Out = A + B;
							}

							void Unity_InvertColors_float3(float3 In, float3 InvertColors, out float3 Out)
							{
								Out = abs(InvertColors - In);
							}

							void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
							{
								Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
							}

							// Graph Vertex
							struct VertexDescriptionInputs
							{
								float3 ObjectSpaceNormal;
								float3 ObjectSpaceTangent;
								float3 ObjectSpacePosition;
								float3 TimeParameters;
							};

							struct VertexDescription
							{
								float3 VertexPosition;
								float3 VertexNormal;
								float3 VertexTangent;
							};

							VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
							{
								VertexDescription description = (VertexDescription)0;
								float _Property_593E0AEE_Out_0 = _ScaleFactor;
								float _Divide_3F46DC63_Out_2;
								Unity_Divide_float(_Property_593E0AEE_Out_0, 100, _Divide_3F46DC63_Out_2);
								float _Property_49DC9932_Out_0 = _TimeRate;
								float _Vector1_A2503412_Out_0 = _Property_49DC9932_Out_0;
								float _Multiply_895424B1_Out_2;
								Unity_Multiply_float(IN.TimeParameters.x, _Vector1_A2503412_Out_0, _Multiply_895424B1_Out_2);
								float _Sine_C93A939E_Out_1;
								Unity_Sine_float(_Multiply_895424B1_Out_2, _Sine_C93A939E_Out_1);
								float _Property_A80C5127_Out_0 = _MinRangeOutline;
								float2 _Vector2_12BCA4D6_Out_0 = float2(_Property_A80C5127_Out_0, 1);
								float _Remap_DD603FD5_Out_3;
								Unity_Remap_float(_Sine_C93A939E_Out_1, float2 (-1, 1), _Vector2_12BCA4D6_Out_0, _Remap_DD603FD5_Out_3);
								float _Multiply_5B283A78_Out_2;
								Unity_Multiply_float(_Divide_3F46DC63_Out_2, _Remap_DD603FD5_Out_3, _Multiply_5B283A78_Out_2);
								float3 _Multiply_67A1EB1F_Out_2;
								Unity_Multiply_float(IN.ObjectSpaceNormal, (_Multiply_5B283A78_Out_2.xxx), _Multiply_67A1EB1F_Out_2);
								float3 _Add_D20BB7A1_Out_2;
								Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_67A1EB1F_Out_2, _Add_D20BB7A1_Out_2);
								description.VertexPosition = _Add_D20BB7A1_Out_2;
								description.VertexNormal = IN.ObjectSpaceNormal;
								description.VertexTangent = IN.ObjectSpaceTangent;
								return description;
							}

							// Graph Pixel
							struct SurfaceDescriptionInputs
							{
								float3 ObjectSpaceNormal;
								float3 WorldSpaceNormal;
								float3 ObjectSpaceViewDirection;
								float3 WorldSpaceViewDirection;
							};

							struct SurfaceDescription
							{
								float Alpha;
								float AlphaClipThreshold;
							};

							SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
							{
								SurfaceDescription surface = (SurfaceDescription)0;
								float3 _InvertColors_BB498680_Out_1;
								float3 _InvertColors_BB498680_InvertColors = float3 (0
							, 0, 0);    Unity_InvertColors_float3(IN.ObjectSpaceNormal, _InvertColors_BB498680_InvertColors, _InvertColors_BB498680_Out_1);
								float _Property_67FE6B44_Out_0 = _FresnelPower;
								float _FresnelEffect_D6FC9FEE_Out_3;
								Unity_FresnelEffect_float(_InvertColors_BB498680_Out_1, IN.ObjectSpaceViewDirection, _Property_67FE6B44_Out_0, _FresnelEffect_D6FC9FEE_Out_3);
								float _Property_6FE5E7CC_Out_0 = _AlphaThreshold;
								surface.Alpha = _FresnelEffect_D6FC9FEE_Out_3;
								surface.AlphaClipThreshold = _Property_6FE5E7CC_Out_0;
								return surface;
							}

							// --------------------------------------------------
							// Structs and Packing

							// Generated Type: Attributes
							struct Attributes
							{
								float3 positionOS : POSITION;
								float3 normalOS : NORMAL;
								float4 tangentOS : TANGENT;
								#if UNITY_ANY_INSTANCING_ENABLED
								uint instanceID : INSTANCEID_SEMANTIC;
								#endif
							};

							// Generated Type: Varyings
							struct Varyings
							{
								float4 positionCS : SV_POSITION;
								float3 normalWS;
								float3 viewDirectionWS;
								#if UNITY_ANY_INSTANCING_ENABLED
								uint instanceID : CUSTOM_INSTANCE_ID;
								#endif
								#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
								uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
								#endif
								#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
								uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
								#endif
								#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
								FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
								#endif
							};

							// Generated Type: PackedVaryings
							struct PackedVaryings
							{
								float4 positionCS : SV_POSITION;
								#if UNITY_ANY_INSTANCING_ENABLED
								uint instanceID : CUSTOM_INSTANCE_ID;
								#endif
								float3 interp00 : TEXCOORD0;
								float3 interp01 : TEXCOORD1;
								#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
								uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
								#endif
								#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
								uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
								#endif
								#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
								FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
								#endif
							};

							// Packed Type: Varyings
							PackedVaryings PackVaryings(Varyings input)
							{
								PackedVaryings output = (PackedVaryings)0;
								output.positionCS = input.positionCS;
								output.interp00.xyz = input.normalWS;
								output.interp01.xyz = input.viewDirectionWS;
								#if UNITY_ANY_INSTANCING_ENABLED
								output.instanceID = input.instanceID;
								#endif
								#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
								output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
								#endif
								#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
								output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
								#endif
								#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
								output.cullFace = input.cullFace;
								#endif
								return output;
							}

							// Unpacked Type: Varyings
							Varyings UnpackVaryings(PackedVaryings input)
							{
								Varyings output = (Varyings)0;
								output.positionCS = input.positionCS;
								output.normalWS = input.interp00.xyz;
								output.viewDirectionWS = input.interp01.xyz;
								#if UNITY_ANY_INSTANCING_ENABLED
								output.instanceID = input.instanceID;
								#endif
								#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
								output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
								#endif
								#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
								output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
								#endif
								#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
								output.cullFace = input.cullFace;
								#endif
								return output;
							}

							// --------------------------------------------------
							// Build Graph Inputs

							VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
							{
								VertexDescriptionInputs output;
								ZERO_INITIALIZE(VertexDescriptionInputs, output);

								output.ObjectSpaceNormal = input.normalOS;
								output.ObjectSpaceTangent = input.tangentOS;
								output.ObjectSpacePosition = input.positionOS;
								output.TimeParameters = _TimeParameters.xyz;

								return output;
							}

							SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
							{
								SurfaceDescriptionInputs output;
								ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

								// must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
								float3 unnormalizedNormalWS = input.normalWS;
								const float renormFactor = 1.0 / length(unnormalizedNormalWS);


								output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;		// we want a unit length Normal Vector node in shader graph
								output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale


								output.WorldSpaceViewDirection = input.viewDirectionWS; //TODO: by default normalized in HD, but not in universal
								output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
							#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
							#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
							#else
							#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
							#endif
							#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

								return output;
							}


							// --------------------------------------------------
							// Main

							#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
							#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

							ENDHLSL
						}

	}
		FallBack "Hidden/Shader Graph/FallbackError"
}
