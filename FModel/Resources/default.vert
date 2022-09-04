#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoords;
layout (location = 3) in vec4 vColor;
layout (location = 4) in ivec4 vBoneIds;
layout (location = 5) in vec4 vWeights;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 fPos;
out vec3 fNormal;
out vec2 fTexCoords;
out vec4 fColor;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);

    fPos = vec3(uModel * vec4(vPos, 1.0));
    fNormal = mat3(transpose(inverse(uModel))) * vNormal;
    fTexCoords = vTexCoords;
    fColor = vColor;
}
