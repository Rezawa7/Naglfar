Shader "Custom/GerstnerOcean"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlobalSpeed ("Global Speed", Float) = 1

        // wave 1
        _Dir1   ("Dir1 (XY)", Vector) = (1,0,0,0)
        _Amp1   ("Amp1", Float)       = 1
        _Wave1  ("Wavelength1", Float)= 10
        _Steep1 ("Steepness1", Float) = 0.5
        _Phase1 ("PhaseOff1", Float)  = 0

        // wave 2
        _Dir2   ("Dir2 (XY)", Vector) = (0,1,0,0)
        _Amp2   ("Amp2", Float)       = 0.5
        _Wave2  ("Wavelength2", Float)= 7
        _Steep2 ("Steepness2", Float) = 0.3
        _Phase2 ("PhaseOff2", Float)  = 1

        // wave 3
        _Dir3   ("Dir3 (XY)", Vector) = (-1,0,0,0)
        _Amp3   ("Amp3", Float)       = 0.8
        _Wave3  ("Wavelength3", Float)= 12
        _Steep3 ("Steepness3", Float) = 0.4
        _Phase3 ("PhaseOff3", Float)  = 2

        // wave 4
        _Dir4   ("Dir4 (XY)", Vector) = (0,-1,0,0)
        _Amp4   ("Amp4", Float)       = 0.6
        _Wave4  ("Wavelength4", Float)= 8
        _Steep4 ("Steepness4", Float) = 0.2
        _Phase4 ("PhaseOff4", Float)  = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _GlobalSpeed;

            float2 _Dir1; float _Amp1; float _Wave1; float _Steep1; float _Phase1;
            float2 _Dir2; float _Amp2; float _Wave2; float _Steep2; float _Phase2;
            float2 _Dir3; float _Amp3; float _Wave3; float _Steep3; float _Phase3;
            float2 _Dir4; float _Amp4; float _Wave4; float _Steep4; float _Phase4;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos  : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float3 norm : NORMAL;
            };

            // Gerstner function
            void Gerstner(float3 worldPos, float2 dir, float A, float L, float S, float phaseOff,
                          float time, inout float3 pos, inout float3 normal)
            {
                float k = UNITY_PI * 2 / L;
                float Qi = S * A * k;
                float2 d = normalize(dir);
                float phase = dot(worldPos.xz, d) * k - time * k * _GlobalSpeed + phaseOff;
                float c = cos(phase), s = sin(phase);

                // displace
                pos.x += Qi * d.x * c;
                pos.y += A * s;
                pos.z += Qi * d.y * c;

                // accumulate normal (approx)
                normal.x += -Qi * d.x * s;
                normal.y += 1;
                normal.z += -Qi * d.y * s;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos4 = mul(unity_ObjectToWorld, v.vertex);
                float3 worldPos  = worldPos4.xyz;
                float3 displaced = worldPos;
                float3 normal    = float3(0,0,0);

                float t = _Time.y; // Unity’s time

                // sum N Gerstner waves
                Gerstner(worldPos, _Dir1, _Amp1, _Wave1, _Steep1, _Phase1, t, displaced, normal);
                Gerstner(worldPos, _Dir2, _Amp2, _Wave2, _Steep2, _Phase2, t, displaced, normal);
                Gerstner(worldPos, _Dir3, _Amp3, _Wave3, _Steep3, _Phase3, t, displaced, normal);
                Gerstner(worldPos, _Dir4, _Amp4, _Wave4, _Steep4, _Phase4, t, displaced, normal);

                // finalize normal
                normal = normalize(mul((float3x3)unity_WorldToObject, normal));

                // output
                o.pos  = mul(UNITY_MATRIX_VP, float4(displaced,1));
                o.uv   = TRANSFORM_TEX(v.uv, _MainTex);
                o.norm = normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // simple lighting
                float nl = saturate(dot(i.norm, normalize(float3(0.3,1,0.5))));
                return col * (0.3 + nl * 0.7);
            }
            ENDCG
        }
    }
}
