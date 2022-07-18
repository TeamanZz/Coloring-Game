Shader "Custom/ShaderSquareToCircleSquare"
{
	Properties
	{
		_Color("main color", Color) = (1,1,1,1)
				 _MainTex("Main Tex", 2D) = "white" {}
				 _CircleSuqareRadius("round radius", Range(0,0.5)) = 0.25 // half of uv
	}
		SubShader
				 {
					 Tags { "RenderType" = "Opaque" }
					 LOD 200

				 pass {
					 CGPROGRAM

					#pragma vertex vert
					#pragma fragment frag
					#include "unitycg.cginc"

					sampler2D _MainTex;
					fixed _CircleSuqareRadius;
					fixed4 _Color;

					struct v2f {

					 float4 pos:SV_POSITION;
					  float2 srcUV:TEXCOORD0; // the original uv
					  float2 adaptUV:TEXCOORD1; // Used to adjust the uv that is convenient for calculation

					};


					v2f vert(appdata_base v) {
						 v2f o;
						 o.pos = UnityObjectToClipPos(v.vertex);
						 o.srcUV = v.texcoord;

						 // Adjust uv (originally 0,1) to -0.5 to 0.5, so the origin is at the center of the graph (unadjusted is the lower left corner)
						o.adaptUV = v.texcoord - float2(0.5,0.5);

						return o;
				   }

				   fixed4 frag(v2f i) :COLOR
				   {
						fixed4 col = fixed4(0,0,0,0);

				   // First draw the middle part (in the set fillet radius) (adaptUV x y absolute value is less than 0.5-the area within the fillet radius)
				  if (abs(i.adaptUV).x < (0.5 - _CircleSuqareRadius) || abs(i.adaptUV).y < (0.5 - _CircleSuqareRadius)) {
					  col = tex2D(_MainTex,i.srcUV);

				  }
	  else {
					  // The next four rounded parts (equivalent to (0.5-round corner radius, 0.5-round corner radius) as the center of the circle, draw uv in the rounded radius uv)
					  // Ignore the excess
					 if (length(abs(i.adaptUV) - float2(0.5 - _CircleSuqareRadius,0.5 - _CircleSuqareRadius)) < _CircleSuqareRadius) {

						 col = tex2D(_MainTex,i.srcUV);

					 }
	 else {

		discard;
	}
}

				  // Mix the main colors and return
				 return col * _Color;
			}

			 ENDCG
		 }
				 }
					 FallBack "Diffuse"
}