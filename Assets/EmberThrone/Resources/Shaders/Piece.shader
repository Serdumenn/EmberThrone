Shader "EmberThrone/Piece"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Flash ("Flash", Range(0,1)) = 0
        _ShineStrength ("Shine strength", Float) = 0.32
        _ShineWidth ("Shine width", Float) = 0.2
        _ShineSpeed ("Shine speed", Float) = 7
        _ShinePeriod ("Shine period", Float) = 34
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            struct PieceV2F
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 world : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Flash;
            float _ShineStrength;
            float _ShineWidth;
            float _ShineSpeed;
            float _ShinePeriod;

            PieceV2F Vert(appdata_t IN)
            {
                PieceV2F OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 local = UnityFlipSprite(IN.vertex, _Flip);
                OUT.world = mul(unity_ObjectToWorld, local).xy;
                OUT.vertex = UnityObjectToClipPos(local);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 Frag(PieceV2F IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                float along = (IN.world.x + IN.world.y) * 0.7071;
                float phase = frac((along - _Time.y * _ShineSpeed) / _ShinePeriod);
                float band = 1.0 - smoothstep(0.0, _ShineWidth, abs(phase - 0.5) * _ShinePeriod);
                c.rgb = lerp(c.rgb, fixed3(1, 1, 1), saturate(_Flash));
                c.rgb += band * band * _ShineStrength;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
