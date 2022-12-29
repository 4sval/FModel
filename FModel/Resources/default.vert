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
layout (location = 13) in vec3 vMorphTarget;

//const int MAX_BONES = 0;
//const int MAX_BONE_INFLUENCE = 0;

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
    vec4 pos = vec4(mix(vPos, vMorphTarget, uMorphTime), 1.0);
//    for(int i = 0 ; i < MAX_BONE_INFLUENCE; i++)
//    {
//        if(vBoneIds[i] == -1) continue;
//        if(vBoneIds[i] >= MAX_BONES)
//        {
//            break;
//        }
//
//        vec4 localPos = uFinalBonesMatrix[int(vBoneIds[i])] * pos;
//        pos += localPos * vBoneWeights[i];
//    }

    gl_Position = uProjection * uView * vInstanceMatrix * pos;

    fPos = vec3(vInstanceMatrix * pos);
    fNormal = mat3(transpose(inverse(vInstanceMatrix))) * vNormal;
    fTangent = mat3(transpose(inverse(vInstanceMatrix))) * vTangent;
    fTexCoords = vTexCoords;
    fTexLayer = vTexLayer;
    fColor = vColor;
}
