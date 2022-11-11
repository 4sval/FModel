#version 330 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 3) in vec3 vTangent;
layout (location = 4) in vec2 vTexCoords;
layout (location = 5) in float vTexLayer;
layout (location = 6) in vec4 vColor;
layout (location = 7) in ivec4 vBoneIds;
layout (location = 8) in vec4 vWeights;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTarget;

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
    vec4 pos = vec4(mix(vPos, vMorphTarget, uMorphTime), 1.0);
    gl_Position = uProjection * uView * vInstanceMatrix * pos;

    fPos = vec3(vInstanceMatrix * pos);
    fNormal = mat3(transpose(inverse(vInstanceMatrix))) * vNormal;
    fTangent = mat3(transpose(inverse(vInstanceMatrix))) * vTangent;
    fTexCoords = vTexCoords;
    fTexLayer = vTexLayer;
    fColor = vColor;
}
