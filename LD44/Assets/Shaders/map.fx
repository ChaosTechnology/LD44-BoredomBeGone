import ChaosGraphics.NormalMap;

sampler2DArray maps;
sampler2D mappingSampler;

void sampleMaterial(
    vec2 inTex,
    out vec4 outEmissive,
    out vec4 outDiffuse,
    out vec4 outSpecular
) {
    vec2 texCoord = inTex * 40.0;
    vec2 texCoordHighFreq = texCoord * 3.6;
    vec4 mappingValue = texture(mappingSampler, inTex);

    outEmissive = outDiffuse = outSpecular = vec4(0,0,0,1);
    vec4 dirtEmissiveA = texture(maps, vec3(texCoordHighFreq, float(2 * 4 + 1)));
    vec4 dirtDiffuseA =  texture(maps, vec3(texCoordHighFreq, float(2 * 4 + 2)));
    vec4 dirtSpecularA = texture(maps, vec3(texCoordHighFreq, float(2 * 4 + 3)));

    vec4 dirtEmissiveB = texture(maps, vec3(texCoord, float(3 * 4 + 1)));
    vec4 dirtDiffuseB =  texture(maps, vec3(texCoord, float(3 * 4 + 2)));
    vec4 dirtSpecularB = texture(maps, vec3(texCoord, float(3 * 4 + 3)));

    float dirtInterpolate = mappingValue.b;
    outEmissive = dirtEmissiveA + (dirtEmissiveB - dirtEmissiveA) * dirtInterpolate;
    outDiffuse  = dirtDiffuseA + (dirtDiffuseB - dirtDiffuseA) * dirtInterpolate;
    outSpecular = dirtSpecularA + (dirtSpecularB - dirtSpecularA) * dirtInterpolate;

    for(int i = 0; i >= 0; i--)
    {
        float f = mappingValue[i];
        vec4 e = texture(maps, vec3(texCoord, float(i * 4 + 1)));
        vec4 d = texture(maps, vec3(texCoord, float(i * 4 + 2)));
        vec4 s = texture(maps, vec3(texCoord, float(i * 4 + 3)));

        float fe = f * e.a;
        float fd = f * d.a;
        float fs = f * s.a;

        outEmissive.rgb = outEmissive.rgb * (1.0 - fe) + e.xyz * fe;
        outDiffuse.rgb  = outDiffuse.rgb  * (1.0 - fd) + d.xyz * fd;
        outSpecular.rgb = outSpecular.rgb * (1.0 - fs) + s.xyz * fs;
    }
}

void sampleWorld(
    vec2 inTex,
    out vec4 outNormal
) {
    vec2 texCoord = inTex * 40.0;
    vec2 texCoordHighFreq = texCoord * 3.6;
    vec4 mappingValue = texture(mappingSampler, inTex);

    vec4 dirtNormalA = texture(maps, vec3(texCoordHighFreq, float(2 * 4)));
    vec4 dirtNormalB = texture(maps, vec3(texCoord, float(3 * 4)));

    float dirtInterpolate = mappingValue.b;
    outNormal = dirtNormalA + (dirtNormalB - dirtNormalA) * dirtInterpolate;

    for(int i = 0; i >= 0; i--)
    {
        float f = mappingValue[i];
        vec4 e = texture(maps, vec3(texCoord, float(i * 4)));
        float fe = f * e.a;
        outNormal.rgb = outNormal.rgb * (1.0 - fe) + e.xyz * fe;
    }
}
