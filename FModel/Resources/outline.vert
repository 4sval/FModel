﻿#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 7) in vec4 vBoneIds;
layout (location = 8) in vec4 vBoneWeights;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTargetPos;

layout(std430, binding = 1) buffer BoneMatrices
{
    mat4 uFinalBonesMatrix[];
};
layout(std430, binding = 2) buffer RestBoneMatrices
{
    mat4 uRestBonesMatrix[];
};

uniform mat4 uView;
uniform vec3 uViewPos;
uniform mat4 uProjection;
uniform float uMorphTime;
uniform bool uIsAnimated;

void main()
{
    vec4 bindPos = vec4(mix(vPos, vMorphTargetPos, uMorphTime), 1.0);
    vec4 bindNormal = vec4(vNormal, 1.0);

    vec4 finalPos = vec4(0.0);
    vec4 finalNormal = vec4(0.0);
    if (uIsAnimated)
    {
        for(int i = 0 ; i < 4; i++)
        {
            int boneIndex = int(vBoneIds[i]);
            if(boneIndex < 0) break;

            mat4 boneMatrix = uFinalBonesMatrix[boneIndex] * inverse(uRestBonesMatrix[boneIndex]);
            float weight = vBoneWeights[i];

            finalPos += boneMatrix * bindPos * weight;
            finalNormal += transpose(inverse(boneMatrix)) * bindNormal * weight;
        }
    }
    else
    {
        finalPos = bindPos;
        finalNormal = bindNormal;
    }

    finalPos = vInstanceMatrix * finalPos;
    float scaleFactor = distance(vec3(finalPos), uViewPos) * 0.0035;
    vec4 nor = transpose(inverse(vInstanceMatrix)) * normalize(finalNormal) * scaleFactor;
    finalPos.xyz += nor.xyz;

    gl_Position = uProjection * uView * finalPos;
}
