Shader "Custom/ZombieDissolveURPLit"
{
    Properties
    {
        // Base Map (Texture and Color)
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        // Normal Map
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Map Strength", Float) = 1.0

        // Dissolve Effect Properties
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0.0
        _EdgeColor ("Edge Color", Color) = (1, 0.3, 0, 1)
        _EdgeThickness ("Edge Thickness", Range(0.01, 0.2)) = 0.05
        
        // Debug properties
        [Toggle] _UseNormals ("Use Normal Map", Float) = 1
        [Toggle] _UseAmbient ("Use Ambient Light", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        
        // Render both sides of triangles to debug
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };

            // Texture samplers
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            // Parameters
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _DissolveAmount;
                float4 _EdgeColor;
                float _EdgeThickness;
                float _NormalScale;
                float _UseNormals;
                float _UseAmbient;
            CBUFFER_END

            // Vertex shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // Transform positions
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                
                // Transform normal
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = normalInputs.normalWS;
                OUT.tangentWS = float4(normalInputs.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                
                // Calculate view direction
                OUT.viewDirWS = GetWorldSpaceViewDir(OUT.positionWS);
                
                // Pass texture coordinates
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                
                return OUT;
            }

            // Fragment shader
            half4 frag(Varyings IN) : SV_Target
            {
                // Dissolve Effect
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv).r;
                if (noise < _DissolveAmount)
                    discard;

                // Edge blending
                float edge = smoothstep(_DissolveAmount, _DissolveAmount + _EdgeThickness, noise);

                // Base color
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                
                // Calculate normal
                float3 normalWS = normalize(IN.normalWS);
                
                if (_UseNormals > 0.5)
                {
                    // Unpack normal map and transform to world space
                    float3 normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalScale);
                    
                    float sgn = IN.tangentWS.w;
                    float3 tangentWS = normalize(IN.tangentWS.xyz);
                    float3 bitangentWS = sgn * cross(normalWS, tangentWS);
                    
                    normalWS = normalize(
                        tangentWS * normalTS.x + 
                        bitangentWS * normalTS.y + 
                        normalWS * normalTS.z
                    );
                }

                // Get main light
                Light mainLight = GetMainLight();
                
                // Calculate direct lighting (Specular lighting included)
                float NdotL = max(0.0, dot(normalWS, mainLight.direction));
                float3 directLighting = mainLight.color * NdotL;

                // Add ambient lighting if enabled
                float3 ambient = float3(0, 0, 0);
                if (_UseAmbient > 0.5)
                {
                    ambient = SampleSH(normalWS) * 0.3;
                }

                // Final lighting with specular highlights
                float3 lighting = directLighting + ambient;
                
                // Apply base color and lighting
                float3 finalColor = baseColor.rgb * lighting;
                
                // Apply dissolve edge effect
                finalColor = lerp(_EdgeColor.rgb, finalColor, edge);
                
                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
        
        // Add shadow pass for receiving shadows
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    Fallback "Universal Render Pipeline/Lit"
}
