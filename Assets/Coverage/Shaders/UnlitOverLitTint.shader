Shader "KT/Unlit over Lit Tint" {
	Properties {
		_MainTex ("Lit Base (RGB)", 2D) = "white" {}
		_LitTint ("Lit Tint", Color) = (1, 1, 1, 1)
		_UnlitTex ("Unlit Base (RGB) Trans (A)", 2D) = "white" {}
		_UnlitTint ("Unlit Tint", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 150

		CGPROGRAM
		#pragma surface surf LitMixUnlit

		sampler2D _MainTex;
		sampler2D _UnlitTex;
		half3 _LitTint;
		half4 _UnlitTint;

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		struct SurfaceOutputCustom {
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Specular;
			half Gloss;
			half Alpha;
			half4 Unlit;
		};

		void surf(Input IN, inout SurfaceOutputCustom o) {
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _LitTint.rgb;
			o.Alpha = IN.color.a;
			o.Unlit = tex2D(_UnlitTex, IN.uv_MainTex) * _UnlitTint;
		}

		half4 LightingLitMixUnlit(SurfaceOutputCustom s, half3 lightDir, half atten) {
			fixed diff = max(0, dot (s.Normal, lightDir));
			fixed3 unlit = s.Unlit.rgb * s.Alpha * s.Unlit.a;
			fixed3 lit = s.Albedo * _LightColor0.rgb * (diff * atten) * (1 - s.Unlit.a);
			fixed4 res;
			res.rgb = lit + unlit;
			res.a = 1;
			return res;
		}

		ENDCG
	}
	Fallback "Mobile/Diffuse"
}
