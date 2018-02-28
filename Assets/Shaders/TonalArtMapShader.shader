Shader "Custom/TonalArtMapShader" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_TonalArtMap ("Tonal Art Map", 2DArray) = "" {}
		_ColorTint ("Tint", Color) = (1.0, 0.6, 0.6, 1.0)
		_Test ("Test", Range(0, 10)) = 0
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }

		CGPROGRAM

		#pragma surface surf Lambert finalcolor:apply_tam

		struct Input {
			float2 uv_MainTex;
			float2 uv_TonalArtMap;
		};

		float _Test;
		fixed4 _ColorTint;
		sampler2D _MainTex;
		//float4 _MainTex_ST;
		UNITY_DECLARE_TEX2DARRAY(_TonalArtMap);

		// yeah, yeah, i know, something something gamma. i don't care
		// at the moment.
		float luma(fixed4 color) {
			const fixed4 luma_vec = float4(0.2126, 0.7152, 0.0722, 1.0);
			return dot(color, luma_vec);
		}

		void do_nothing(Input IN, SurfaceOutput o, inout fixed4 color)
		{
		}

		void apply_tam(Input IN, SurfaceOutput o, inout fixed4 color)
		{
			//fixed l = pow(luma(color), 2.2);
			fixed l = luma(color);
			fixed texI = (1 - l) * 8.0;

			fixed4 col1 = UNITY_SAMPLE_TEX2DARRAY(_TonalArtMap, float3(IN.uv_TonalArtMap, floor(texI)));
			fixed4 col2 = UNITY_SAMPLE_TEX2DARRAY(_TonalArtMap, float3(IN.uv_TonalArtMap, ceil(texI)));

			color = lerp(col1, col2, texI - floor(texI));
			//color = col1;
			//color = pow(l, 1.0/2.2);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = _ColorTint * tex2D(_MainTex, IN.uv_MainTex);
		}

		ENDCG
	}
	Fallback "Diffuse"
}
