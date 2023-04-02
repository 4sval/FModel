#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 3) in vec3 vTangent;
layout (location = 4) in vec2 vTexCoords;
layout (location = 5) in float vTexLayer;
layout (location = 6) in vec4 vColor;
layout (location = 7) in vec4 vBoneIds;
layout (location = 8) in vec4 vBoneWeights;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTargetPos;
layout (location = 14) in vec3 vMorphTargetTangent;

layout(std430, binding = 1) buffer BoneMatrices
{
    mat4 uFinalBonesMatrix[];
};

uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorphTime;

out vec3 fPos;
out vec3 fNormal;
out vec3 fTangent;
out vec2 fTexCoords;
out float fTexLayer;
out vec4 fColor;

void main()
{
    vec4 bindPos = vec4(mix(vPos, vMorphTargetPos, uMorphTime), 1.0);
    vec4 bindNormal = vec4(vNormal, 1.0);
    vec4 bindTangent = vec4(mix(vTangent, vMorphTargetTangent, uMorphTime), 1.0);

    vec4 finalPos = vec4(0.0);
    vec4 finalNormal = vec4(0.0);
    vec4 finalTangent = vec4(0.0);
    if (vBoneIds != vBoneWeights)
    {
        for(int i = 0 ; i < 4; i++)
        {
            int boneIndex = int(vBoneIds[i]);
            if(boneIndex < 0) break;

            mat4 boneMatrix = uFinalBonesMatrix[boneIndex];
            mat4 inverseBoneMatrix = transpose(inverse(boneMatrix));
            float weight = vBoneWeights[i];

            finalPos += boneMatrix * bindPos * weight;
            finalNormal += inverseBoneMatrix * bindNormal * weight;
            finalTangent += inverseBoneMatrix * bindTangent * weight;
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
    fColor = vColor;
}
