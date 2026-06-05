import ChaosGraphics.MaskedSpotLight;

vec4 getMaskSample(
	vec3 worldPos,
	float angle,
	mat4 invLightTransform
) {
    vec3 worldPosCameraSpace = (vec4(worldPos, 1.0) * invLightTransform).xyz;
    float angleMultiplier = worldPosCameraSpace.z * tan(angle);
    worldPosCameraSpace.xy /= angleMultiplier;
    if (abs(worldPosCameraSpace.x) > 1.0 || abs(worldPosCameraSpace.y) > 1.0)
        discard;

    vec4 col = texture(mask, (worldPosCameraSpace.xy + 1.0) / 2.0);
    col = vec4(-vec3(col.a), col.a) + col;
    col.a = clamp(col.a, 0.0, 1.0);
    return vec4(col.xyz * col.a, col.a);
}
