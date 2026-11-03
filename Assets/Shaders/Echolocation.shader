Shader "Custom/Echolocation"
{
    Properties
    {
        _BaseColor ("Base Fill Color", Color) = (0, 0, 0, 1)
        _EdgeColor ("Edge & Wave Color", Color) = (1, 1, 1, 1)
        _EdgeWidth ("Edge Thickness (Pixels)", Range(0.5, 5.0)) = 1.65
        _ShapeMode ("Shape Mode (0=Box/Plane, 1=Tri/Ramp, 2=Sphere, 3=Cylinder)", Float) = 0
        _AlwaysVisibleEdge ("Always Visible Edge", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "EcholocationForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_PULSES 32

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _EdgeWidth;
                float _ShapeMode;
                float _AlwaysVisibleEdge;
            CBUFFER_END

            // Global echolocation pulse arrays updated by EcholocationManager
            int _PulseCount;
            float4 _PulsePositions[MAX_PULSES]; // xyz = world origin, w = current radius
            float4 _PulseParams[MAX_PULSES];    // x = maxRadius, y = ringWidth, z = alpha, w = interiorReveal
            float4 _PulseColors[MAX_PULSES];    // rgb = pulse color, a = unused

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 uv2        : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 uv2        : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                float3 normalOS   : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = normInputs.normalWS;
                OUT.uv         = IN.uv;
                OUT.uv2        = IN.uv2;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalOS   = IN.normalOS;
                return OUT;
            }

            float ComputeGeometryDistancePx(Varyings IN)
            {
                float distPx = 1000.0;

                if (_ShapeMode < 0.5)
                {
                    // Mode 0: Box / Plane / Quad (UV border in [0,1]^2)
                    float2 uvDist = min(IN.uv, 1.0 - IN.uv);
                    float2 uvDeriv = max(fwidth(IN.uv), 1e-5);
                    float2 edgePx2 = uvDist / uvDeriv;
                    distPx = min(edgePx2.x, edgePx2.y);
                }
                else if (_ShapeMode < 1.5)
                {
                    // Mode 1: Triangle / Ramp / Pyramid
                    if (IN.uv2.w > 0.5)
                    {
                        float3 baryDist = IN.uv2.xyz;
                        float3 baryDeriv = max(fwidth(baryDist), 1e-5);
                        float3 edgePx3 = baryDist / baryDeriv;
                        distPx = min(edgePx3.x, min(edgePx3.y, edgePx3.z));
                    }
                    else
                    {
                        float2 uvDist = min(IN.uv, 1.0 - IN.uv);
                        float2 uvDeriv = max(fwidth(IN.uv), 1e-5);
                        float2 edgePx2 = uvDist / uvDeriv;
                        distPx = min(edgePx2.x, edgePx2.y);
                    }
                }
                else if (_ShapeMode < 2.5)
                {
                    // Mode 2: Sphere / Curved surface (Silhouette outline)
                    float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                    float3 N = normalize(IN.normalWS);
                    float ndv = abs(dot(N, V));
                    float ndvDeriv = max(fwidth(ndv), 1e-4);
                    distPx = ndv / (1.85 * ndvDeriv);
                }
                else
                {
                    // Mode 3: Cylinder (Top/Bottom circular caps + Silhouette sides)
                    if (abs(IN.normalOS.y) > 0.5)
                    {
                        float r = length(IN.positionOS.xz);
                        float rDeriv = max(fwidth(r), 1e-5);
                        distPx = abs(0.5 - r) / rDeriv;
                    }
                    else
                    {
                        float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                        float3 N = normalize(IN.normalWS);
                        float ndv = abs(dot(N, V));
                        float ndvDeriv = max(fwidth(ndv), 1e-4);
                        float sideSilhouettePx = ndv / (1.85 * ndvDeriv);

                        float yDist = 1.0 - abs(IN.positionOS.y);
                        float yDeriv = max(fwidth(IN.positionOS.y), 1e-5);
                        float capRimPx = yDist / yDeriv;

                        distPx = min(sideSilhouettePx, capRimPx);
                    }
                }

                return distPx;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Compute screen-space derivatives once outside the loop
                float geomDistPx = ComputeGeometryDistancePx(IN);
                float worldPosDeriv = max(length(fwidth(IN.positionWS)) * 0.7, 0.002);

                float baseEdgeFactor = 1.0 - smoothstep(max(0.1, _EdgeWidth - 0.55), _EdgeWidth + 0.65, geomDistPx);
                float3 finalColor = _EdgeColor.rgb * (baseEdgeFactor * _AlwaysVisibleEdge);

                int count = min(_PulseCount, MAX_PULSES);
                for (int i = 0; i < count; i++)
                {
                    float3 pulseOrigin = _PulsePositions[i].xyz;
                    float currentRadius = _PulsePositions[i].w;
                    float maxRadius = max(_PulseParams[i].x, 0.01);
                    float ringWidth = _PulseParams[i].y;
                    float alpha = _PulseParams[i].z;
                    float interiorReveal = _PulseParams[i].w;
                    float3 pulseCol = _PulseColors[i].rgb * _EdgeColor.rgb;

                    float d = distance(IN.positionWS, pulseOrigin);
                    if (d > maxRadius) continue;

                    // Distance attenuation: semakin jauh dari pusat echolocation, efek semakin kecil dan akhirnya hilang
                    float distRatio = saturate(d / maxRadius);
                    float radiusRatio = saturate(currentRadius / maxRadius);
                    float farFade = pow(1.0 - distRatio, 1.45);
                    float waveExpandFade = pow(1.0 - radiusRatio, 1.15);

                    // 1. Expanding Wavefront Ring (mengecil ketebalannya & meredup saat semakin jauh)
                    float shrunkRingWidth = ringWidth * lerp(1.0, 0.25, radiusRatio);
                    float halfRing = max(shrunkRingWidth * 0.5, worldPosDeriv * lerp(1.75, 0.75, radiusRatio));
                    float signedDist = d - currentRadius;
                    float normalizedRingDist = (signedDist >= 0.0)
                        ? (signedDist / halfRing)
                        : (-signedDist / (halfRing * 1.35));
                    float ring = (1.0 - smoothstep(0.15, 1.0, normalizedRingDist)) * alpha * farFade * waveExpandFade;

                    // 2. Interior Object Edge Reveal (semakin jauh dari pusat & belakang gelombang, semakin tipis & hilang)
                    float insideSphere = 1.0 - smoothstep(currentRadius - 0.05, currentRadius + halfRing, d);
                    float trailSpan = max(maxRadius * 0.55, 2.5);
                    float wakeFade = smoothstep(currentRadius - trailSpan, currentRadius, d);
                    float scaledWidth = _EdgeWidth * lerp(0.55, 1.05, farFade);
                    float pulseEdgeFactor = 1.0 - smoothstep(max(0.1, scaledWidth - 0.55), scaledWidth + 0.65, geomDistPx);

                    float scanIntensity = insideSphere * farFade * lerp(0.35, 1.0, wakeFade) * interiorReveal * alpha;

                    float pulseContribution = saturate(ring + pulseEdgeFactor * scanIntensity);
                    if (pulseContribution > 0.001)
                    {
                        finalColor = max(finalColor, pulseCol * pulseContribution);
                    }
                }

                float3 outRGB = max(_BaseColor.rgb, finalColor);
                return half4(outRGB, 1.0);
            }
            ENDHLSL
        }
    }
}
