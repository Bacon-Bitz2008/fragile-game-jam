Shader "Custom/URP/CrackLit3D"
{
    // NOTE: this shader reads crack data that CrackShatter3D bakes into the mesh
    // (UV3 = pattern position, UV4 = growth order). Use it together with that script.
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Cracks)]
        _Damage ("Damage", Range(0,1)) = 0
        _CrackColor ("Crack Color", Color) = (0,0,0,1)
        [HDR] _CrackEmission ("Crack Glow", Color) = (0,0,0,0)
        _CrackScale ("Crack Density (cells across the model)", Float) = 8
        _CrackWidth ("Crack Width (at first hit)", Range(0.005, 0.2)) = 0.04
        _CrackWidthGrow ("Extra Width At Full Damage", Range(0, 0.3)) = 0.08

        [Header(Optional crack texture)]
        [Toggle] _UseCrackTexture ("Use Crack Texture Instead", Float) = 0
        _CrackTex ("Crack Texture (R = growth order, A = line mask)", 2D) = "black" {}
        _Spread ("Texture: Spread From Impact", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_CrackTex); SAMPLER(sampler_CrackTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _CrackColor;
                half4  _CrackEmission;
                float  _CrackScale;
                float  _CrackWidth;
                float  _CrackWidthGrow;
                float  _Damage;
                float  _Spread;
                float  _UseCrackTexture;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float3 patternPos : TEXCOORD2;   // baked: position normalised to the model's size
                float2 orderData  : TEXCOORD3;   // baked: x = growth order (0 at impact, 1 farthest)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 normalOS   : TEXCOORD3;
                float3 patternPos : TEXCOORD4;
                float  order      : TEXCOORD5;
            };

            // ---- 3D Voronoi: distances to the nearest two feature points + their cell ids
            float3 hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float2 Voronoi(float3 x, out float3 id1, out float3 id2)
            {
                float3 n = floor(x);
                float3 f = frac(x);
                float f1 = 8.0, f2 = 8.0;
                id1 = n; id2 = n;

                for (int k = -1; k <= 1; k++)
                for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    float3 g = float3(i, j, k);
                    float3 o = hash33(n + g);
                    float3 r = g + o - f;
                    float d = dot(r, r);
                    if (d < f1)      { f2 = f1; id2 = id1; f1 = d; id1 = n + g; }
                    else if (d < f2) { f2 = d;  id2 = n + g; }
                }
                return float2(sqrt(f1), sqrt(f2));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.normalOS   = IN.normalOS;
                OUT.patternPos = IN.patternPos;
                OUT.order      = IN.orderData.x;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float lineMask;
                float order;   // 0..1: the damage value at which this spot gets cracked

                if (_UseCrackTexture > 0.5)
                {
                    float3 aN = abs(IN.normalOS);
                    float3 p = IN.patternPos * _CrackScale;
                    float2 crackUV = (aN.x > aN.y && aN.x > aN.z) ? p.zy : ((aN.y > aN.z) ? p.xz : p.xy);
                    half4 crack = SAMPLE_TEXTURE2D_LOD(_CrackTex, sampler_CrackTex, crackUV, 0);
                    lineMask = smoothstep(0.2, 0.6, crack.a);
                    order = lerp(crack.r, IN.order, _Spread);
                }
                else
                {
                    // Procedural cracks along Voronoi cell borders
                    float3 id1, id2;
                    float2 f = Voronoi(IN.patternPos * _CrackScale, id1, id2);
                    float edge = f.y - f.x;                                  // ~0 on a border
                    float width = _CrackWidth + _CrackWidthGrow * _Damage;   // cracks get wider as damage rises
                    lineMask = 1.0 - smoothstep(width * 0.5, width, edge);

                    // Spreads outward from the impact; per-segment jitter makes it look like it's creeping
                    float jitter = (hash33(id1).x + hash33(id2).x) * 0.5;
                    order = saturate(IN.order * 0.8 + jitter * 0.2);
                }

                float visible = step(order, _Damage) * step(0.001, _Damage);   // nothing at damage 0
                float amount = lineMask * visible;

                half3 albedo = lerp(baseCol.rgb, _CrackColor.rgb, amount);

                // Simple lighting: main light + shadows + ambient
                float3 n = normalize(IN.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = saturate(dot(n, mainLight.direction));
                half3 ambient = SampleSH(n);

                half3 col = albedo * (mainLight.color * ndl * mainLight.shadowAttenuation + ambient);
                col += _CrackEmission.rgb * amount;
                return half4(col, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
    }
}
