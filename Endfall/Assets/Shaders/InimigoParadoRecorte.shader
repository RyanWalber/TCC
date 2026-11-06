Shader "EndFall/InimigoParadoRecorte"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura DragonBones", 2D) = "white" {}
        _Color ("Cor", Color) = (1,1,1,1)
        _GroundY ("Altura do chao", Float) = -100000
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _GroundY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Mantem opacidade total acima do chao: sem faixa de
                // transparencia de varios metros ou borda irregular.
                float distChao = i.worldPos.y - _GroundY;
                clip(distChao);

                // Suaviza apenas a borda de um pixel, sem apagar o corpo.
                float pixel = max(fwidth(i.worldPos.y), 0.0001);
                col.a *= saturate(distChao / pixel);
                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
