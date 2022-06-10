// https://forum.unity.com/threads/temperature-map-shader.190030/
Shader "Example/QuadraticLD"
{
    Properties
    {
        _Emission("Emissiom", float) = 0
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct VertexInputs
            {
                float4 positionOS   : POSITION;
                float3 normal        : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct VertexOutputs
            {
                float4 positionHCS  : SV_POSITION;
                float3 normal        : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Emission;
            CBUFFER_END

            uniform half4 colorTemperature;
            uniform float3 cameraLookDirection;
            uniform float u;
            uniform float a;
            uniform float b;
            uniform int linearDarkening;

            VertexOutputs vert(VertexInputs IN)
            {
                VertexOutputs OUT;
                float3 offset = abs(cos(_Time.z)*.1);
                OUT.positionHCS = TransformObjectToHClip(sin(IN.positionOS.xyz*_Time.y)*.33 - cos(IN.positionOS.xyz*_Time.y)*.33);
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                return OUT;
            }

            half4 frag(VertexOutputs OUT) : SV_Target
            {
                // float cosTheta = dot(cameraLookDirection * -1, OUT.normal);
                // half4 color= colorTemperature * (1-a*(1-abs(cosTheta)) - b*pow(1-abs(cosTheta), 2));


                float r = pow(abs(OUT.uv.x-.5), 2.) + pow(abs(OUT.uv.y-.5), 2.);
                r=r/.06;
                half4 color = half4(r, 0., .0, 1.);

                return  color;

            }
            ENDHLSL
        }
    }
}
