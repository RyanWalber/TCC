Shader "EndFall/InimigoParadoRecorte"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura DragonBones", 2D) = "white" {}
        _Color ("Cor", Color) = (1,1,1,1)
        _GroundY ("Altura do chao", Float) = -100000
        _FadeHeight ("Suavidade da borda", Float) = 5
        _EdgeIrregularity ("Irregularidade da terra", Float) = 1.5
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
            float _FadeHeight;
            float _EdgeIrregularity;

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

                // Pequena ondulacao deterministica da linha de terra.
                // Sempre desloca a borda para CIMA, nunca revela pixels
                // que ainda estao abaixo do topo da plataforma.
                float ondulacao = 0.5
                    + 0.32 * sin(i.worldPos.x * 0.27)
                    + 0.18 * sin(i.worldPos.x * 0.69 + 1.7);
                float inicioFade = _GroundY + max(0.0, _EdgeIrregularity) * ondulacao;
                float alturaFade = max(0.05, _FadeHeight);
                float aparicao = smoothstep(inicioFade, inicioFade + alturaFade, i.worldPos.y);

                // Transparencia gradual ao emergir, sem uma borda
                // reta e dura entre o corpo e a plataforma.
                col.a *= aparicao;
                clip(col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
