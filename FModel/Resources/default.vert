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

//const int MAX_BONES = 140;

uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorphTime;
//uniform mat4 uFinalBonesMatrix[MAX_BONES];

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

//    vec4 finalPos = vec4(0.0);
//    vec4 finalNormal = vec4(0.0);
//    vec4 finalTangent = vec4(0.0);
//    vec4 weights = normalize(vBoneWeights);
//    for(int i = 0 ; i < 4; i++)
//    {
//        int boneIndex = int(vBoneIds[i]);
//        if(boneIndex < 0) break;
//
//        finalPos += uFinalBonesMatrix[boneIndex] * bindPos * weights[i];
//        finalNormal += uFinalBonesMatrix[boneIndex] * bindNormal * weights[i];
//        finalTangent += uFinalBonesMatrix[boneIndex] * bindTangent * weights[i];
//    }

    gl_Position = uProjection * uView * vInstanceMatrix * bindPos;

    fPos = vec3(vInstanceMatrix * bindPos);
    fNormal = vec3(transpose(inverse(vInstanceMatrix)) * bindNormal);
    fTangent = vec3(transpose(inverse(vInstanceMatrix)) * bindTangent);
    fTexCoords = vTexCoords;
    fTexLayer = vTexLayer;
    fColor = vColor;
}
