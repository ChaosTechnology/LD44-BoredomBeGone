import ChaosGraphics.VertexShaders;

const vec3 boxPositions[8] = vec3[]
(
    vec3(-1.0, -1.0, -1.0),
    vec3( 1.0, -1.0, -1.0),
    vec3(-1.0,  1.0, -1.0),
    vec3( 1.0,  1.0, -1.0),
    vec3(-1.0, -1.0,  1.0),
    vec3( 1.0, -1.0,  1.0),
    vec3(-1.0,  1.0,  1.0),
    vec3( 1.0,  1.0,  1.0)
);

const int indices[14] = int[]
(
    0, 1, 2, 3,  // Back Face
    7,           // Top Face, Right Half
    1, 5,        // Right Face
    0, 4,        // Bottom Face
    2, 6,        // Left Face
    7,           // Top Face, Left Half
    4, 5         // Front Face
);

samplerCube tex;

void fs_sampleCube(
    vec3 inTex : TEXCOORD0,
    out vec4 outColor : COLOR0
) {
    outColor = texture(tex, inTex);
}

void vs_createCube(
    out vec4 position : gl_Position,
    out vec3 texCoord : TEXCOORD0
) {
    int index = indices[gl_VertexID];
    texCoord = boxPositions[index];
    position = vec4(boxPositions[index], 1.0) * transform * viewProj;
    position.z = position.w - 0.00000001;
}

Pass Sky
{
    Enable(CullFace, false);
    Enable(Blend, false);
    DepthMask(false);
    Enable(DepthTest, false);
    VertexShader = vs_createCube;
    FragmentShader = fs_sampleCube;
}
