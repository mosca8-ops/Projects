Shader "Outline/WEAVROutlineBase"
{
	Properties
	{
		_Color("OutlineColor", Color) = (0, 0.8334823, 1, 1)
		_ScaleFactor("ScaleFactor", Range(0, 2)) = 1
		_MinRangeOutline("MinRangeOutline", Range(0, 1)) = 0.5
		_TimeRate("TimeRate", Float) = 3
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
				surface.Color = (_Property_25BAC21C_Out_0.xyz);
				surface.Alpha = 1;
				surface.AlphaClipThreshold = 0.5;
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
					};

					struct SurfaceDescription
					{
						float Alpha;
						float AlphaClipThreshold;
					};

					SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
					{
						SurfaceDescription surface = (SurfaceDescription)0;
						surface.Alpha = 1;
						surface.AlphaClipThreshold = 0.5;
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
							};

							struct SurfaceDescription
							{
								float Alpha;
								float AlphaClipThreshold;
							};

							SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
							{
								SurfaceDescription surface = (SurfaceDescription)0;
								surface.Alpha = 1;
								surface.AlphaClipThreshold = 0.5;
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
