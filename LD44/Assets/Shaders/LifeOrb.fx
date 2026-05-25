import *Variables, fs_createTransparencyMask, linearizeDepth from ChaosGraphics.TransparencyMask;
import vs_transformPosition from  ChaosGraphics.Instancing;

vec4 screenSize : SCREEN_SIZE;
vec4 scrollParams;
vec3 viewPosition : VIEW_POSITION;

void fs_band(
    vec2 inTex : TEXCOORD0,
    out vec4 col : COLOR0
) {
    float additive = 0.5;
    inTex.x += scrollParams.x;
    col = texture(tex, inTex);
    col.a *= 2.0;
    col.rgb *= (1 - additive) * col.a;
}

Pass Mask
{
    Enable(CullFace, false);
    VertexShader = vs_transformPosition;
    FragmentShader = fs_createTransparencyMask;
}

Pass Bands
{
    Enable(CullFace, false);
    Enable(Blend, true);
    BlendFuncSeperate(One, OneMinusSrcAlpha, OneMinusDstAlpha, One);
    VertexShader = vs_transformPosition, PASS(vec2 TEXCOORD0);
    FragmentShader = fs_band;
}
