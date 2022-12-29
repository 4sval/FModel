#version 460 core

layout (location = 0) in vec3 vPos;
layout (location = 9) in mat4 vInstanceMatrix;

uniform mat4 uView;
uniform mat4 uProjection;

out vec2 fTexCoords;

void main()
{
    gl_Position = uProjection * uView * vInstanceMatrix * vec4(inverse(mat3(uView)) * vPos, 1.0);
    fTexCoords = -vPos.xy;
}
