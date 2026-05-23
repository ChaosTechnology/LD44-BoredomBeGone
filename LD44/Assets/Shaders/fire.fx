import ChaosGraphics.ParticleSystem;
import tex from ChaosGraphics.Sprite;

void fs_particle(
    vec2 inTex : TEXCOORD0,
    float additive : ADDITIVE,
    vec4 inColor : COLOR0,
    out vec4 col : COLOR0
) {
    col = texture(tex, inTex) * inColor;
    col.rgb *= col.a + (1 - col.a)  *  additive;
}

void vs_additiveValue(
    vec4 stuffyStream : PARTICLE_TEXOFFSET,
    out float additive : ADDITIVE
) {
    additive = stuffyStream.z;
}

Pass Instanced
{
    Enable(Blend, true);
    BlendFuncSeperate(One, OneMinusSrcAlpha, OneMinusDstAlpha, One);
    VertexShader = vs_createParticleInstanced, vs_additiveValue;
    FragmentShader = fs_particle;
}
