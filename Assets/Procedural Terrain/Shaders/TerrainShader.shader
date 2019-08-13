Shader "ProceduralTerrain/TerrainShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_GrassTex("Grass Texture", 2D) = "white" {}
        _RockTex ("Rock Texture", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader
		{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard vertex:vert fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _GrassTex;
        sampler2D _RockTex;

        struct Input
        {
            float2 uv_GrassTex : TEXCOORD0;
			float3 localNormal : NORMAL;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.uv_GrassTex = v.texcoord.xy;
			data.localNormal = v.normal.xyz;
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
			float slope = 1.0f - abs(IN.localNormal.y);

			float blendAmount = 0.0f;
			if (slope > 0.6f)
			{
				blendAmount = (slope - 0.6f) / 0.4f;
			}

            fixed4 grassColor = tex2D (_GrassTex, IN.uv_GrassTex) * _Color;
			fixed4 rockColor = tex2D(_RockTex, IN.uv_GrassTex) * _Color;

			fixed4 color = lerp(grassColor, rockColor, blendAmount);
            o.Albedo = color.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0f;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
