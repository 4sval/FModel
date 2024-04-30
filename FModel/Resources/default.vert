#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 3) in vec3 vTangent;
layout (location = 4) in vec2 vTexCoords;
layout (location = 5) in int vTexLayer;
layout (location = 6) in float vColor;
layout (location = 7) in vec4 vBoneInfluence;
layout (location = 8) in vec4 vBoneInfluenceExtra;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTargetPos;
layout (location = 14) in vec3 vMorphTargetTangent;

layout(std430, binding = 1) buffer BoneMatrices
{
    mat4 uFinalBonesMatrix[];
};
layout(std430, binding = 2) buffer RestBoneMatrices
{
    mat4 uRestBonesMatrix[];
};

uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorphTime;
uniform bool uIsAnimated;

out vec3 fPos;
out vec3 fNormal;
out vec3 fTangent;
out vec2 fTexCoords;
flat out int fTexLayer;
out vec4 fColor;

vec4 unpackARGB(int color)
{
    float a = float((color >> 24) & 0xFF);
    float r = float((color >> 16) & 0xFF);
    float g = float((color >> 8) & 0xFF);
    float b = float((color >> 0) & 0xFF);
    return vec4(r, g, b, a);
}

vec2 unpackBoneIDsAndWeights(int packedData)
{
    return vec2(float((packedData >> 16) & 0xFFFF), float(packedData & 0xFFFF));
}

void main()
{
    vec4 bindPos = vec4(mix(vPos, vMorphTargetPos, uMorphTime), 1.0);
    vec4 bindNormal = vec4(vNormal, 1.0);
    vec4 bindTangent = vec4(mix(vTangent, vMorphTargetTangent, uMorphTime), 1.0);

    vec4 finalPos = vec4(0.0);
    vec4 finalNormal = vec4(0.0);
    vec4 finalTangent = vec4(0.0);
    if (uIsAnimated)
    {
        vec4 boneInfluences[2];
        boneInfluences[0] = vBoneInfluence;
        boneInfluences[1] = vBoneInfluenceExtra;
        for (int i = 0; i < 2; i++)
        {
            for(int j = 0 ; j < 4; j++)
            {
                vec2 boneInfluence = unpackBoneIDsAndWeights(int(boneInfluences[i][j]));
                int boneIndex = int(boneInfluence.x);
                float weight = boneInfluence.y;

                mat4 boneMatrix = uFinalBonesMatrix[boneIndex] * inverse(uRestBonesMatrix[boneIndex]);
                mat4 inverseBoneMatrix = transpose(inverse(boneMatrix));

                finalPos += boneMatrix * bindPos * weight;
                finalNormal += inverseBoneMatrix * bindNormal * weight;
                finalTangent += inverseBoneMatrix * bindTangent * weight;
            }
        }
    }
    else
    {
        finalPos = bindPos;
        finalNormal = bindNormal;
        finalTangent = bindTangent;
    }

    gl_Position = uProjection * uView * vInstanceMatrix * finalPos;

    fPos = vec3(vInstanceMatrix * finalPos);
    fNormal = vec3(transpose(inverse(vInstanceMatrix)) * finalNormal);
    fTangent = vec3(transpose(inverse(vInstanceMatrix)) * finalTangent);
    fTexCoords = vTexCoords;
    fTexLayer = vTexLayer;
    fColor = unpackARGB(int(vColor));
}
