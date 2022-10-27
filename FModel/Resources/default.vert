#version 330 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 3) in vec2 vTexCoords;
layout (location = 4) in float vTexIndex;
layout (location = 5) in vec4 vColor;
layout (location = 6) in ivec4 vBoneIds;
layout (location = 7) in vec4 vWeights;
layout (location = 8) in mat4 vInstanceMatrix;
layout (location = 12) in vec3 vMorphTarget;

uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorphTime;

out vec3 fPos;
out vec3 fNormal;
out vec2 fTexCoords;
out float fTexIndex;
out vec4 fColor;

void main()
{
    vec3 pos = mix(vPos, vMorphTarget, uMorphTime);
    gl_Position = uProjection * uView * vInstanceMatrix * vec4(pos, 1.0);

    fPos = vec3(vInstanceMatrix * vec4(pos, 1.0));
    fNormal = mat3(transpose(inverse(vInstanceMatrix))) * vNormal;
    fTexCoords = vTexCoords;
    fTexIndex = vTexIndex;
    fColor = vColor;
}
