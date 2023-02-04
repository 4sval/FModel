#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 7) in vec4 vBoneIds;
layout (location = 8) in vec4 vBoneWeights;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTargetPos;

uniform mat4 uView;
uniform vec3 uViewPos;
uniform mat4 uProjection;
uniform float uMorphTime;
uniform mat4 uFinalBonesMatrix[250];

void main()
{
    vec4 bindPos = vec4(mix(vPos, vMorphTargetPos, uMorphTime), 1.0);
    vec4 bindNormal = vec4(vNormal, 1.0);

    vec4 finalPos = vec4(0.0);
    vec4 finalNormal = vec4(0.0);
    for(int i = 0 ; i < 4; i++)
    {
        int boneIndex = int(vBoneIds[i]);
        if(boneIndex < 0) break;

        mat4 boneMatrix = uFinalBonesMatrix[boneIndex];
        float weight = vBoneWeights[i];

        finalPos += boneMatrix * bindPos * weight;
        finalNormal += boneMatrix * bindNormal * weight;
    }

    vec3 pos = vec3(vInstanceMatrix * finalPos);
    vec3 nor = vec3(transpose(inverse(vInstanceMatrix)) * finalNormal);

    float scaleFactor = distance(pos, uViewPos) * 0.0025;
    vec3 scaleVertex = pos + nor * scaleFactor;
    gl_Position = uProjection * uView * vec4(scaleVertex, 1.0);
}
