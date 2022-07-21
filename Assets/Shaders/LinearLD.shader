// https://forum.unity.com/threads/temperature-map-shader.190030/
Shader "Example/LinearLD"
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

            #define M_PI 3.14159265359
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
            //
            // uniform half4 temperature;
            uniform float temperature;
            uniform float3 cameraLookDirection;
            uniform float u;
            uniform float a;
            uniform float b;
            uniform int linearDarkening;

            VertexOutputs vert(VertexInputs IN)
            {
                VertexOutputs OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }



            float random (float2 uv) {
                return frac(sin(dot(uv.xy, float2(12.9898,78.233))) * 43758.5453123 * _Time.y*.1);
            }

            float3 mod289(float3 x) {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float4 mod289(float4 x) {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float4 permute(float4 x) {
                return mod289(((x*34.0)+1.0)*x);
            }

            float4 taylorInvSqrt(float4 r)
            {
                return 1.79284291400159 - 0.85373472095314 * r;
            }


            float snoise(float3 v)
            {
                const float2  C = float2(1.0/6.0, 1.0/3.0) ;
                const float4  D = float4(0.0, 0.5, 1.0, 2.0);

                // First corner
                float3 i  = floor(v + dot(v, C.yyy) );
                float3 x0 =   v - i + dot(i, C.xxx) ;

                // Other corners
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min( g.xyz, l.zxy );
                float3 i2 = max( g.xyz, l.zxy );

                //   x0 = x0 - 0.0 + 0.0 * C.xxx;
                //   x1 = x0 - i1  + 1.0 * C.xxx;
                //   x2 = x0 - i2  + 2.0 * C.xxx;
                //   x3 = x0 - 1.0 + 3.0 * C.xxx;
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
                float3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

                // Permutations
                i = mod289(i);
                float4 p =
                permute
                (
                permute
                (
                permute
                (
                i.z + float4(0.0, i1.z, i2.z, 1.000)
                )
                + i.y + float4(0.0, i1.y, i2.y, 1.0 )
                )
                + i.x + float4(0.0, i1.x, i2.x, 1.0 )
                );

                // Gradients: 7x7 points over a square, mapped onto an octahedron.
                // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
                float n_ = 0.142857142857; // 1.0/7.0
                float3  ns = n_ * D.wyz - D.xzx;

                float4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

                float4 x = x_ *ns.x + ns.yyyy;
                float4 y = y_ *ns.x + ns.yyyy;
                float4 h = 1. - abs(x) - abs(y);

                float4 b0 = float4( x.xy, y.xy );
                float4 b1 = float4( x.zw, y.zw );

                //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
                // float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
                float4 s0 = floor(b0)*2.0 + 1.0;
                float4 s1 = floor(b1)*2.0 + 1.0;
                float4 sh = -step(h, 0.0);

                float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
                float4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

                float3 p0 = float3(a0.xy,h.x);
                float3 p1 = float3(a0.zw,h.y);
                float3 p2 = float3(a1.xy,h.z);
                float3 p3 = float3(a1.zw,h.w);

                //Normalise gradients
                float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;

                // lerp final noise value
                float4 m = max(.6 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.1);
                m = m * m;
                return 42.0 * dot( m*m, float4( dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3) ) );
            }

            // p: position
            // o: how many layers
            // f: frequency
            // lac: how fast frequency changes between layers
            // r: how fast amplitude changes between layers
            float fbm4(float3 p, float theta, float f, float lac, float r)
            {
                float3x3 mtx = float3x3(
                cos(theta*29.), -sin(theta*29.), 0.0,
                sin(theta*29.), cos(theta*29.), 0.0,
                0.0, 0.0, 1.0);

                float frequency = f;
                float lacunarity = lac;
                float roughness = r;
                float amp = 1.0;
                float total_amp = .0;

                float accum = 0.1;
                float3 X = p * frequency;

                for(int i = 0; i < 4; i++)
                {
                    accum += amp * snoise(X);
                    X *= (lacunarity + (snoise(X) + .1) * 0.0006);
                    X = mtx * float3x1(X);

                    total_amp += amp;
                    amp *= roughness;
                }

                return accum / total_amp;
            }

            float turbulence(float val)
            {
                float n = 1. - abs(val);
                return n * n*n;
            }

            float granule(in float3 p, inout float3 q, inout float3 r)
            {
                q.x = fbm4( p + 0.0, .6, .1, .01, 0.3 );
                q.y = fbm4( p + 6.0, .6, .1, .01, 0.3 );

                r.x = fbm4( p + q - 2.4, 2.0, 1.0, 2.0, 0.5 );
                r.y = fbm4( p + q + 8.2, .0, 1.0, 2.0, 0.5 );

                q.x = turbulence( q.x );
                q.y = turbulence( q.y );

                float f = fbm4( p + (1.0 * r), 0.0, 0., 2.0, 0.5);

                return f;
            }


            float saturate(float v) { return clamp(v, 0.0, 1.0);       }
            float2  saturate(float2  v) { return clamp(v, (0.0), (1.0)); }
            float3  saturate(float3  v) { return clamp(v, (0.0), (1.0)); }
            float4  saturate(float4  v) { return clamp(v, (0.0), (1.0)); }

            float3 ColorTemperatureToRGB(float temperatureInKelvins)
            {
                float3 retColor;
                float factor =2.1;

                temperatureInKelvins = clamp(temperatureInKelvins, 1000.0, 40000.0) / 100.0;

                if (temperatureInKelvins <= 66.0 )
                {
                    retColor.r = 1.0;
                    retColor.g = saturate(0.39008157876901960784 * log(temperatureInKelvins) - 0.63184144378862745098);
                }
                else
                {
                    float t = temperatureInKelvins - 60.0;
                    retColor.r = saturate(1.29293618606274509804 * pow(t, -0.1332047592));
                    retColor.g = saturate(1.12989086089529411765 * pow(t, -0.0755148492));
                }

                if (temperatureInKelvins >= 66.0 )
                retColor.b = 1.0;
                else if(temperatureInKelvins <= 19.0 )
                retColor.b = 0.0;
                else
                retColor.b = saturate(0.54320678911019607843 * log(temperatureInKelvins - 10.0) - 1.19625408914);

                return retColor;
            }

            half4 frag(VertexOutputs OUT) : SV_Target
            {

                float2 uv = ((OUT.uv));

                float x = uv.x;
                float y = uv.y;

                float ya = (y-.5)*M_PI;
                uv.x = (x-.5)*(sin(ya)*tan(M_PI/2. - ya));

                float t = _Time.y* 0.9;

                float3 spectrum[4];
                // spectrum[0] = float3(1.00, 1.00, 0.00);
                // spectrum[1] = float3(0.50, 0.00, 0.00);
                // spectrum[2] = float3(1.00, 0.40, 0.20);
                // spectrum[3] = float3(1.0, .60, 0.0500)*1.8;

                // spectrum[0] = lerp(float3(1.00, 1.00, 0.00), darkening.xyz, .5);
                // spectrum[1] = lerp(float3(0.50, 0.00, 0.00), darkening.xyz, .5);
                // spectrum[2] = lerp(float3(1.00, 0.40, 0.20), darkening.xyz, .5);
                // spectrum[3] = lerp(float3(1.00, .60, 0.050), darkening.xyz, .5);

                // spectrum[1] = float3(0.50, 0.00, 0.00);
                // spectrum[2] = float3(1.00, 0.40, 0.20);
                // spectrum[3] = float3(1.0, .60, 0.0500);

                float temp1 = temperature - 600; // cold
                float temp0 = temperature; // hot

                float i0 = 1;
                float i1 =pow(temp1/temp0, 4);

                spectrum[1] = ColorTemperatureToRGB(temp0);
                spectrum[2] = ColorTemperatureToRGB(temp1);

                uv *= 1000;

                float3 p = float3(uv.x, uv.y, t);
                float3 q = float3(0.000995,0.00193,0.00590);
                float3 r = float3(0.000120,0.00205,0.00027);

                granule(p, q, r);

                float3 color2 = 0.0;
                // todo: instead of interpolating between colors, we should interpolate between the temperatures
                // then use the result for... [what?]
                color2 = lerp(spectrum[1]*i0, spectrum[2]*i1, clamp(pow(length(q), 2), 0, 1));


                // color2 = pow(color2, 2.0);
                float4 granule =  float4( (color2 ), 1.0);

                float cosTheta = dot(cameraLookDirection * -1, OUT.normal);
                float4 darkening = granule * (1-u*(1-abs(cosTheta )));



                // return temperature;
                // return float4(ColorTemperatureToRGB(1000).xyz, 1.);

                // return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, OUT.uv) * granule * darkening;
                return  darkening * i1 * 6.;

            }
            ENDHLSL
        }


        //-------------------------


        // Pass
        // {
            //     HLSLPROGRAM
            //     #pragma vertex vert
            //     #pragma fragment frag

            //     #define M_PI 3.14159265359
            //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //     struct VertexInputs
            //     {
                //         float4 positionOS   : POSITION;
                //         float3 normal        : NORMAL;
                //         float2 uv           : TEXCOORD0;
            //     };

            //     struct VertexOutputs
            //     {
                //         float4 positionHCS  : SV_POSITION;
                //         float3 normal        : NORMAL;
                //         float2 uv           : TEXCOORD0;
            //     };

            //     TEXTURE2D(_BaseMap);
            //     SAMPLER(sampler_BaseMap);

            //     CBUFFER_START(UnityPerMaterial)
            //         float4 _BaseMap_ST;
            //         float _Emission;
            //     CBUFFER_END
            //
            // uniform half4 temperature;
            //     uniform float3 cameraLookDirection;
            //     uniform float u;
            //     uniform float a;
            //     uniform float b;
            //     uniform int linearDarkening;

            //     VertexOutputs vert(VertexInputs IN)
            //     {
                //         VertexOutputs OUT;
                //         OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                //         OUT.normal = TransformObjectToWorldNormal(IN.normal);
                //         OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                //         return OUT;
            //     }


            //     half4 frag(VertexOutputs OUT) : SV_Target
            //     {
                // float cosTheta = dot(cameraLookDirection * -1, OUT.normal);
                // half4 darkening = temperature * (1-u*(1-abs(cosTheta)));

                //         return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, OUT.uv) * darkening;

            //     }
            //     ENDHLSL
        // }
    }
}
