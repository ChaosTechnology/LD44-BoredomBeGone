import ChaosGraphics.MaterialDefault;
import ChaosGraphics.VertexShaders;

vec4 viewPosition : VIEW_POSITION;

samplerCube environmentSampler;
sampler2D reflectiveMask : REFLECTIVE_MAP;

void fs_sampleMaterial(
    vec2 inTex : TEXCOORD0,
    vec3 inWorldPos : TEXCOORD1,
    vec3 inNormal : TEXCOORD2,
    out vec4 outEmissive : COLOR0,
    out vec4 outDiffuse : COLOR1,
    out vec4 outSpecular : COLOR2
) {
    outEmissive = texture(emissiveSampler, inTex)
                + texture(reflectiveMask, inTex) * texture(environmentSampler, reflect(inWorldPos - viewPosition.xyz, normalize(inNormal)));
    outDiffuse  = texture(diffuseSampler, inTex);
    outSpecular = texture(specularSampler, inTex);
}

Pass World
{
    FragmentShader = fs_sampleWorldNormalMap;
    VertexShader = vs_transformPositionAndWorld, vs_transformNormal, vs_transformTangent, PASS(vec2 TEXCOORD0);
}

Pass Material
{
    FragmentShader = fs_sampleMaterial;
    VertexShader = vs_transformPositionAndWorld, vs_transformNormal, vs_transformTangent, PASS(vec2 TEXCOORD0);
}
