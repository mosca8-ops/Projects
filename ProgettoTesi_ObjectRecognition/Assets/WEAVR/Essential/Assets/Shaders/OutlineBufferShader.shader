﻿Shader "Hidden/WEAVROutlineBufferEffect" {
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
	}

	SubShader
	{ 
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		// Change this stuff in OutlineEffect.cs instead!
		//ZWrite Off
		//Blend One OneMinusSrcAlpha
		Cull [_Culling]
		Lighting Off
			
		CGPROGRAM

		#pragma surface surf Lambert vertex:vert nofog noshadow noambient nolightmap novertexlights noshadowmask nometa //keepalpha
		#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
		fixed4 _Color;
		float _OutlineAlphaCutoff;

		struct Input
		{
			float2 uv_MainTex;
			//fixed4 color;
		};

		void vert(inout appdata_full v, out Input o)
		{
			#if defined(PIXELSNAP_ON)
			v.vertex = UnityPixelSnap(v.vertex);
			#endif

			UNITY_INITIALIZE_OUTPUT(Input, o);
			//o.color = v.color;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);// * IN.color;
			if (c.a < _OutlineAlphaCutoff) discard;

			o.Normal = fixed3(0, 0, 0);
			o.Albedo = _Color;
			o.Alpha = c.a;
			o.Emission = o.Albedo;
		}

		ENDCG		
	}

	Fallback "Transparent/VertexLit"
}
