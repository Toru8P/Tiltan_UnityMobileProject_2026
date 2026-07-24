// URP port of "Polytope Studio/ PT_Medieval Armors Shader PBR".
//
// The original (PT_Armors_Shader_PBR.shader) is an Amplify-generated Built-in RP surface shader
// (CGPROGRAM + "#pragma surface surf Standard"), which renders magenta under URP. This file keeps
// the exact same property names and blend maths so an existing material's saved values carry over
// unchanged - only the shader reference in the .mat needs to point here.
//
// How the original works, and what is reproduced below:
//   * _Texture2 is the shared base/AO texture. Every zone colour is multiplied by it.
//   * _Texture0/1/3/4/5/6/7/8 are mask maps that each pack THREE zones into their R, G and B channels.
//   * ColorMask() turns a channel into a hard 0/1 selector (it selects where the channel is ~black).
//   * The zones are composited as a fixed-priority lerp chain; later entries win over earlier ones.
//   * Metallic and smoothness are composited through the same masks.
//
// Differences from the original, all deliberate:
//   * Ambient light comes from SampleSH (light probes). The original relied on the Built-in
//     ambient path. These armours are skinned/dynamic, so probes are the correct source and
//     lightmap support is intentionally omitted.
//   * Emission and normal mapping are not used, matching the original surface output.
Shader "Polytope Studio/ PT_Medieval Armors Shader PBR URP"
{
	Properties
	{
		[HDR] _SKINCOLOR( "SKIN COLOR", Color ) = ( 2.02193, 1.0081, 0.6199315, 0 )
		_SKINSMOOTHNESS( "SKIN SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _EYESCOLOR( "EYES COLOR", Color ) = ( 0.0734529, 0.1320755, 0.05046281, 1 )
		_EYESSMOOTHNESS( "EYES SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _HAIRCOLOR( "HAIR COLOR", Color ) = ( 0.5943396, 0.3518379, 0.1093361, 0 )
		_HAIRSMOOTHNESS( "HAIR SMOOTHNESS", Range( 0, 1 ) ) = 0.1
		[HDR] _SCLERACOLOR( "SCLERA COLOR", Color ) = ( 0.9056604, 0.8159487, 0.8159487, 0 )
		_SCLERASMOOTHNESS( "SCLERA SMOOTHNESS", Range( 0, 1 ) ) = 0.5
		[HDR] _LIPSCOLOR( "LIPS COLOR", Color ) = ( 0.8301887, 0.3185886, 0.2780349, 0 )
		_LIPSSMOOTHNESS( "LIPS SMOOTHNESS", Range( 0, 1 ) ) = 0.4
		[HDR] _SCARSCOLOR( "SCARS COLOR", Color ) = ( 0.8490566, 0.5037117, 0.3884835, 0 )
		_SCARSSMOOTHNESS( "SCARS SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _METAL1COLOR( "METAL 1 COLOR", Color ) = ( 2, 0.682353, 0.1960784, 0 )
		_METAL1METALLIC( "METAL 1 METALLIC", Range( 0, 1 ) ) = 0.65
		_METAL1SMOOTHNESS( "METAL 1 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _METAL2COLOR( "METAL 2 COLOR", Color ) = ( 0.4674706, 0.4677705, 0.5188679, 0 )
		_METAL2METALLIC( "METAL 2 METALLIC", Range( 0, 1 ) ) = 0.65
		_METAL2SMOOTHNESS( "METAL 2 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _METAL3COLOR( "METAL 3 COLOR", Color ) = ( 0.4383232, 0.4383232, 0.4716981, 0 )
		_METAL3METALLIC( "METAL 3 METALLIC", Range( 0, 1 ) ) = 0.65
		_METAL3SMOOTHNESS( "METAL 3 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _LEATHER1COLOR( "LEATHER 1 COLOR", Color ) = ( 0.4811321, 0.2041155, 0.08851016, 1 )
		_LEATHER1SMOOTHNESS( "LEATHER 1 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _LEATHER2COLOR( "LEATHER 2 COLOR", Color ) = ( 0.4245283, 0.190437, 0.09011215, 1 )
		_LEATHER2SMOOTHNESS( "LEATHER 2 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _LEATHER3COLOR( "LEATHER 3 COLOR", Color ) = ( 0.1698113, 0.04637412, 0.02963688, 1 )
		_LEATHER3SMOOTHNESS( "LEATHER 3 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _CLOTH1COLOR( "CLOTH 1 COLOR", Color ) = ( 0.1465379, 0.282117, 0.3490566, 0 )
		[HDR] _CLOTH2COLOR( "CLOTH 2 COLOR", Color ) = ( 1, 0, 0, 0 )
		[HDR] _CLOTH3COLOR( "CLOTH 3 COLOR", Color ) = ( 0.8773585, 0.6337318, 0.3434941, 0 )
		[HDR] _GEMS1COLOR( "GEMS 1 COLOR", Color ) = ( 0.3773585, 0, 0.06650025, 0 )
		_GEMS1SMOOTHNESS( "GEMS 1 SMOOTHNESS", Range( 0, 1 ) ) = 1
		[HDR] _GEMS2COLOR( "GEMS 2 COLOR", Color ) = ( 0.2023368, 0, 0.4339623, 0 )
		_GEMS2SMOOTHNESS( "GEMS 2 SMOOTHNESS", Range( 0, 1 ) ) = 0
		[HDR] _GEMS3COLOR( "GEMS 3 COLOR", Color ) = ( 0, 0.1132075, 0.01206957, 0 )
		_GEMS3SMOOTHNESS( "GEMS 3 SMOOTHNESS", Range( 0, 1 ) ) = 0
		[HDR] _FEATHERS1COLOR( "FEATHERS 1 COLOR", Color ) = ( 0.7735849, 0.492613, 0.492613, 0 )
		[HDR] _FEATHERS2COLOR( "FEATHERS 2 COLOR", Color ) = ( 0.6792453, 0, 0, 0 )
		[HDR] _FEATHERS3COLOR( "FEATHERS 3 COLOR", Color ) = ( 0, 0.1793142, 0.7264151, 0 )
		[HDR] _CUSTOM1COLOR( "CUSTOM 1 COLOR", Color ) = ( 0.0734529, 0.1320755, 0.05046281, 1 )
		_CUSTOM1SMOOTHNESS( "CUSTOM 1 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _CUSTOM2COLOR( "CUSTOM 2 COLOR", Color ) = ( 0.5943396, 0.3518379, 0.1093361, 0 )
		_CUSTOM2SMOOTHNESS( "CUSTOM 2 SMOOTHNESS", Range( 0, 1 ) ) = 0.1
		[HDR] _CUSTOM3COLOR( "CUSTOM 3 COLOR", Color ) = ( 2.02193, 1.0081, 0.6199315, 0 )
		_CUSTOM3SMOOTHNESS( "CUSTOM 3 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HideInInspector] _Texture0( "Texture 0", 2D ) = "white" {}
		[HideInInspector] _Texture8( "Texture 8", 2D ) = "white" {}
		[HideInInspector] _Texture1( "Texture 1", 2D ) = "white" {}
		[HideInInspector] _Texture6( "Texture 6", 2D ) = "white" {}
		[HideInInspector] _Texture3( "Texture 3", 2D ) = "white" {}
		[HideInInspector] _Texture5( "Texture 5", 2D ) = "white" {}
		[HideInInspector][HDR] _Texture2( "Texture 2", 2D ) = "white" {}
		[HideInInspector] _Texture4( "Texture 4", 2D ) = "white" {}
		[HideInInspector] _Texture7( "Texture 7", 2D ) = "white" {}
		[HDR] _COATOFARMSCOLOR( "COAT OF ARMS COLOR", Color ) = ( 1, 0, 0, 0 )
		[NoScaleOffset] _COATOFARMSMASK( "COAT OF ARMS MASK", 2D ) = "black" {}
		_OCCLUSION( "OCCLUSION", Range( 0, 1 ) ) = 0.5
		[Toggle] _MetalicOn( "Metalic On", Float ) = 1
		[Toggle] _SmoothnessOn( "Smoothness On", Float ) = 1
	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
			"UniversalMaterialType" = "Lit"
			"Queue" = "Geometry"
		}
		LOD 300

		// -------------------------------------------------------------------------------------
		// Shared material data. Everything non-texture lives in one CBUFFER so the SRP Batcher
		// can batch these renderers.
		// -------------------------------------------------------------------------------------
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _Texture0_ST;
			float4 _Texture1_ST;
			float4 _Texture2_ST;
			float4 _Texture3_ST;
			float4 _Texture4_ST;
			float4 _Texture5_ST;
			float4 _Texture6_ST;
			float4 _Texture7_ST;
			float4 _Texture8_ST;
			half4 _SKINCOLOR;
			half4 _EYESCOLOR;
			half4 _HAIRCOLOR;
			half4 _SCLERACOLOR;
			half4 _LIPSCOLOR;
			half4 _SCARSCOLOR;
			half4 _METAL1COLOR;
			half4 _METAL2COLOR;
			half4 _METAL3COLOR;
			half4 _LEATHER1COLOR;
			half4 _LEATHER2COLOR;
			half4 _LEATHER3COLOR;
			half4 _CLOTH1COLOR;
			half4 _CLOTH2COLOR;
			half4 _CLOTH3COLOR;
			half4 _GEMS1COLOR;
			half4 _GEMS2COLOR;
			half4 _GEMS3COLOR;
			half4 _FEATHERS1COLOR;
			half4 _FEATHERS2COLOR;
			half4 _FEATHERS3COLOR;
			half4 _CUSTOM1COLOR;
			half4 _CUSTOM2COLOR;
			half4 _CUSTOM3COLOR;
			half4 _COATOFARMSCOLOR;
			half _SKINSMOOTHNESS;
			half _EYESSMOOTHNESS;
			half _HAIRSMOOTHNESS;
			half _SCLERASMOOTHNESS;
			half _LIPSSMOOTHNESS;
			half _SCARSSMOOTHNESS;
			half _METAL1METALLIC;
			half _METAL1SMOOTHNESS;
			half _METAL2METALLIC;
			half _METAL2SMOOTHNESS;
			half _METAL3METALLIC;
			half _METAL3SMOOTHNESS;
			half _LEATHER1SMOOTHNESS;
			half _LEATHER2SMOOTHNESS;
			half _LEATHER3SMOOTHNESS;
			half _GEMS1SMOOTHNESS;
			half _GEMS2SMOOTHNESS;
			half _GEMS3SMOOTHNESS;
			half _CUSTOM1SMOOTHNESS;
			half _CUSTOM2SMOOTHNESS;
			half _CUSTOM3SMOOTHNESS;
			half _OCCLUSION;
			half _MetalicOn;
			half _SmoothnessOn;
		CBUFFER_END
		ENDHLSL

		// -------------------------------------------------------------------------------------
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			// SurfaceData is NOT pulled in by Core.hlsl or Lighting.hlsl - it must be included directly.
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_Texture0); SAMPLER(sampler_Texture0);
			TEXTURE2D(_Texture1); SAMPLER(sampler_Texture1);
			TEXTURE2D(_Texture2); SAMPLER(sampler_Texture2);
			TEXTURE2D(_Texture3); SAMPLER(sampler_Texture3);
			TEXTURE2D(_Texture4); SAMPLER(sampler_Texture4);
			TEXTURE2D(_Texture5); SAMPLER(sampler_Texture5);
			TEXTURE2D(_Texture6); SAMPLER(sampler_Texture6);
			TEXTURE2D(_Texture7); SAMPLER(sampler_Texture7);
			TEXTURE2D(_Texture8); SAMPLER(sampler_Texture8);
			TEXTURE2D(_COATOFARMSMASK); SAMPLER(sampler_COATOFARMSMASK);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				float2 uv         : TEXCOORD0;
				float2 uv2        : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv         : TEXCOORD0;
				float2 uv2        : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float3 normalWS   : TEXCOORD3;
				half   fogCoord   : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Reproduces the pack's "Color Mask" sub-graph exactly. It is a hard selector: the
			// division by 1e-5 makes the ramp effectively instantaneous, returning 1 only where
			// the sampled channel is (near) black and 0 everywhere else.
			float ColorMask( float channel )
			{
				float3 c = float3( channel, channel, channel );
				return saturate( 1.0 - ( ( distance( c, float3( 0, 0, 0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) );
			}

			Varyings vert( Attributes input )
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_TRANSFER_INSTANCE_ID( input, output );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				VertexPositionInputs positionInputs = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInputs = GetVertexNormalInputs( input.normalOS );

				output.positionCS = positionInputs.positionCS;
				output.positionWS = positionInputs.positionWS;
				output.normalWS = normalInputs.normalWS;
				output.uv = input.uv;
				output.uv2 = input.uv2;
				output.fogCoord = ComputeFogFactor( positionInputs.positionCS.z );
				return output;
			}

			half4 frag( Varyings input ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );

				// Base/AO texture that every zone colour is tinted by.
				float2 uvBase = input.uv * _Texture2_ST.xy + _Texture2_ST.zw;
				half4 baseTex = SAMPLE_TEXTURE2D( _Texture2, sampler_Texture2, uvBase );

				// --- Sample every mask map once ---------------------------------------------
				half4 mSkin     = SAMPLE_TEXTURE2D( _Texture0, sampler_Texture0, input.uv * _Texture0_ST.xy + _Texture0_ST.zw );
				half4 mFace     = SAMPLE_TEXTURE2D( _Texture1, sampler_Texture1, input.uv * _Texture1_ST.xy + _Texture1_ST.zw );
				half4 mLeather  = SAMPLE_TEXTURE2D( _Texture3, sampler_Texture3, input.uv * _Texture3_ST.xy + _Texture3_ST.zw );
				half4 mFeathers = SAMPLE_TEXTURE2D( _Texture4, sampler_Texture4, input.uv * _Texture4_ST.xy + _Texture4_ST.zw );
				half4 mCloth    = SAMPLE_TEXTURE2D( _Texture5, sampler_Texture5, input.uv * _Texture5_ST.xy + _Texture5_ST.zw );
				half4 mMetal    = SAMPLE_TEXTURE2D( _Texture6, sampler_Texture6, input.uv * _Texture6_ST.xy + _Texture6_ST.zw );
				half4 mGems     = SAMPLE_TEXTURE2D( _Texture7, sampler_Texture7, input.uv * _Texture7_ST.xy + _Texture7_ST.zw );
				half4 mCustom   = SAMPLE_TEXTURE2D( _Texture8, sampler_Texture8, input.uv * _Texture8_ST.xy + _Texture8_ST.zw );

				// --- Turn each channel into a 0/1 zone selector ------------------------------
				float gems3    = ColorMask( mGems.b );
				float gems2    = ColorMask( mGems.g );
				float gems1    = ColorMask( mGems.r );
				float feath3   = ColorMask( mFeathers.b );
				float feath2   = ColorMask( mFeathers.g );
				float feath1   = ColorMask( mFeathers.r );
				float cloth3   = ColorMask( mCloth.b );
				float cloth2   = ColorMask( mCloth.g );
				float cloth1   = ColorMask( mCloth.r );
				float leath3   = ColorMask( mLeather.b );
				float leath2   = ColorMask( mLeather.g );
				float leath1   = ColorMask( mLeather.r );
				float metal3   = ColorMask( mMetal.b );
				float metal2   = ColorMask( mMetal.g );
				float metal1   = ColorMask( mMetal.r );
				float scars    = ColorMask( mFace.b );
				float lips     = ColorMask( mFace.g );
				float sclera   = ColorMask( mFace.r );
				float eyes     = ColorMask( mSkin.b );
				float hair     = ColorMask( mSkin.g );
				float skin     = ColorMask( mSkin.r );
				float custom1  = ColorMask( mCustom.b );
				float custom2  = ColorMask( mCustom.g );
				float custom3  = ColorMask( mCustom.r );

				// --- Albedo: fixed-priority composite, later zones win -----------------------
				half4 albedo = half4( 0, 0, 0, 0 );
				albedo = lerp( albedo, baseTex * _GEMS3COLOR,     gems3 );
				albedo = lerp( albedo, baseTex * _GEMS2COLOR,     gems2 );
				albedo = lerp( albedo, baseTex * _GEMS1COLOR,     gems1 );
				albedo = lerp( albedo, baseTex * _FEATHERS3COLOR, feath3 );
				albedo = lerp( albedo, baseTex * _FEATHERS2COLOR, feath2 );
				albedo = lerp( albedo, baseTex * _FEATHERS1COLOR, feath1 );
				albedo = lerp( albedo, baseTex * _CLOTH3COLOR,    cloth3 );
				albedo = lerp( albedo, baseTex * _CLOTH2COLOR,    cloth2 );
				albedo = lerp( albedo, baseTex * _CLOTH1COLOR,    cloth1 );
				albedo = lerp( albedo, baseTex * _LEATHER3COLOR,  leath3 );
				albedo = lerp( albedo, baseTex * _LEATHER2COLOR,  leath2 );
				albedo = lerp( albedo, baseTex * _LEATHER1COLOR,  leath1 );
				albedo = lerp( albedo, baseTex * _METAL3COLOR,    metal3 );
				albedo = lerp( albedo, baseTex * _METAL2COLOR,    metal2 );
				albedo = lerp( albedo, baseTex * _METAL1COLOR,    metal1 );
				albedo = lerp( albedo, baseTex * _SCARSCOLOR,     scars );
				albedo = lerp( albedo, baseTex * _LIPSCOLOR,      lips );
				albedo = lerp( albedo, baseTex * _SCLERACOLOR,    sclera );
				albedo = lerp( albedo, baseTex * _EYESCOLOR,      eyes );
				albedo = lerp( albedo, baseTex * _HAIRCOLOR,      hair );
				albedo = lerp( albedo, baseTex * _SKINCOLOR,      skin );
				albedo = lerp( albedo, baseTex * _CUSTOM1COLOR,   custom1 );
				albedo = lerp( albedo, baseTex * _CUSTOM2COLOR,   custom2 );
				albedo = lerp( albedo, baseTex * _CUSTOM3COLOR,   custom3 );

				// --- Coat of arms decal, stamped from the second UV set ----------------------
				half coatAlpha = SAMPLE_TEXTURE2D( _COATOFARMSMASK, sampler_COATOFARMSMASK, input.uv2 ).a;
				half inv = 1.0 - coatAlpha;
				half4 invVec = half4( inv, inv, inv, inv );
				half blend = saturate( ( distance( invVec, half4( 0, 0, 0, 0 ) ) - 1.6 ) / max( 1.0, 1E-05 ) );
				half4 coat = lerp( _COATOFARMSCOLOR, invVec, blend );
				albedo = lerp( albedo, coat, coatAlpha );

				// --- Metallic ----------------------------------------------------------------
				half metallic = 0.0;
				metallic = lerp( metallic, _METAL3METALLIC, metal3 );
				metallic = lerp( metallic, _METAL2METALLIC, metal2 );
				metallic = lerp( metallic, _METAL1METALLIC, metal1 );
				metallic = _MetalicOn ? metallic : 0.0;

				// --- Smoothness, composited through the same masks ---------------------------
				half smoothness = 0.0;
				smoothness = lerp( smoothness, _GEMS3SMOOTHNESS,    gems3 );
				smoothness = lerp( smoothness, _GEMS2SMOOTHNESS,    gems2 );
				smoothness = lerp( smoothness, _GEMS1SMOOTHNESS,    gems1 );
				smoothness = lerp( smoothness, _LEATHER3SMOOTHNESS, leath3 );
				smoothness = lerp( smoothness, _LEATHER2SMOOTHNESS, leath2 );
				smoothness = lerp( smoothness, _LEATHER1SMOOTHNESS, leath1 );
				smoothness = lerp( smoothness, _METAL3SMOOTHNESS,   metal3 );
				smoothness = lerp( smoothness, _METAL2SMOOTHNESS,   metal2 );
				smoothness = lerp( smoothness, _METAL1SMOOTHNESS,   metal1 );
				smoothness = lerp( smoothness, _SCARSSMOOTHNESS,    scars );
				smoothness = lerp( smoothness, _LIPSSMOOTHNESS,     lips );
				smoothness = lerp( smoothness, _SCLERASMOOTHNESS,   sclera );
				smoothness = lerp( smoothness, _EYESSMOOTHNESS,     eyes );
				smoothness = lerp( smoothness, _HAIRSMOOTHNESS,     hair );
				smoothness = lerp( smoothness, _SKINSMOOTHNESS,     skin );
				smoothness = lerp( smoothness, _CUSTOM1SMOOTHNESS,  custom1 );
				smoothness = lerp( smoothness, _CUSTOM2SMOOTHNESS,  custom2 );
				smoothness = lerp( smoothness, _CUSTOM3SMOOTHNESS,  custom3 );
				smoothness = _SmoothnessOn ? smoothness : 0.0;

				// Original remapped _OCCLUSION from 0..1 onto 1..0.5.
				half occlusion = 1.0 - 0.5 * _OCCLUSION;

				// --- Hand over to URP's PBR lighting -----------------------------------------
				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedo.rgb;
				surfaceData.metallic = metallic;
				surfaceData.smoothness = smoothness;
				surfaceData.occlusion = occlusion;
				surfaceData.normalTS = half3( 0, 0, 1 );
				surfaceData.emission = half3( 0, 0, 0 );
				surfaceData.specular = half3( 0, 0, 0 );
				surfaceData.clearCoatMask = 0;
				surfaceData.clearCoatSmoothness = 0;
				surfaceData.alpha = 1;

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.normalWS = normalize( input.normalWS );
				inputData.viewDirectionWS = SafeNormalize( GetCameraPositionWS() - input.positionWS );
				inputData.shadowCoord = TransformWorldToShadowCoord( input.positionWS );
				inputData.fogCoord = input.fogCoord;
				inputData.vertexLighting = half3( 0, 0, 0 );
				// These armours are skinned/dynamic, so ambient comes from light probes.
				inputData.bakedGI = SampleSH( inputData.normalWS );
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV( input.positionCS );
				inputData.shadowMask = half4( 1, 1, 1, 1 );

				half4 color = UniversalFragmentPBR( inputData, surfaceData );
				color.rgb = MixFog( color.rgb, inputData.fogCoord );
				color.a = 1;
				return color;
			}
			ENDHLSL
		}

		// -------------------------------------------------------------------------------------
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull Back

			HLSLPROGRAM
			#pragma vertex ShadowVert
			#pragma fragment ShadowFrag
			#pragma target 3.0
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			float3 _LightDirection;
			float3 _LightPosition;

			struct ShadowAttributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct ShadowVaryings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			ShadowVaryings ShadowVert( ShadowAttributes input )
			{
				ShadowVaryings output = (ShadowVaryings)0;
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );
				float3 normalWS = TransformObjectToWorldNormal( input.normalOS );

			#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 lightDirectionWS = normalize( _LightPosition - positionWS );
			#else
				float3 lightDirectionWS = _LightDirection;
			#endif

				// URP 17 exposes the near-plane clamp as a helper; matches ShadowCasterPass.hlsl.
				float4 positionCS = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, lightDirectionWS ) );
				output.positionCS = ApplyShadowClamping( positionCS );
				return output;
			}

			half4 ShadowFrag( ShadowVaryings input ) : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}

		// -------------------------------------------------------------------------------------
		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }

			ZWrite On
			ColorMask R
			Cull Back

			HLSLPROGRAM
			#pragma vertex DepthVert
			#pragma fragment DepthFrag
			#pragma target 3.0
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct DepthAttributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct DepthVaryings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			DepthVaryings DepthVert( DepthAttributes input )
			{
				DepthVaryings output = (DepthVaryings)0;
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );
				output.positionCS = TransformObjectToHClip( input.positionOS.xyz );
				return output;
			}

			half4 DepthFrag( DepthVaryings input ) : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}

		// -------------------------------------------------------------------------------------
		Pass
		{
			Name "DepthNormals"
			Tags { "LightMode" = "DepthNormals" }

			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex DepthNormalsVert
			#pragma fragment DepthNormalsFrag
			#pragma target 3.0
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct DNAttributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct DNVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS   : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			DNVaryings DepthNormalsVert( DNAttributes input )
			{
				DNVaryings output = (DNVaryings)0;
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );
				output.positionCS = TransformObjectToHClip( input.positionOS.xyz );
				output.normalWS = TransformObjectToWorldNormal( input.normalOS );
				return output;
			}

			half4 DepthNormalsFrag( DNVaryings input ) : SV_Target
			{
				return half4( NormalizeNormalPerPixel( input.normalWS ), 0.0 );
			}
			ENDHLSL
		}
	}

	Fallback Off
}
