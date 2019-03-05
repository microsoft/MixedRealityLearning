Shader "Custom/HandGlow" {
    Properties{
        _ColorTint("Base Color", Color) = (1,1,1,1)
        _RimColor("Glow Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimPower("Rim Power", Range(1.0, 6.0)) = 3.0
    }

    SubShader{
        Tags {"RenderType" = "Opaque"}

        ZWrite On

        CGPROGRAM
            #pragma surface surf Lambert

            fixed4 _RimColor;
            float _RimPower;
            float4 _ColorTint;

            struct Input {
                float3 viewDir;
                float3 worldNormal;
            };

            void surf(Input IN, inout SurfaceOutput o) {
                o.Albedo = _ColorTint;
                half rim = 1.0 - saturate(dot(normalize(IN.viewDir), IN.worldNormal));
                o.Emission = _RimColor.rgb * pow(rim, _RimPower);
            }
        ENDCG
    }
    FallBack "Diffuse"

}
