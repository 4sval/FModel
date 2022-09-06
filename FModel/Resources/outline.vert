#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 6) in mat4 vInstanceMatrix;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    float scaleFactor = 0.005;
    vec3 scaleVertex = vPos + vNormal * scaleFactor;
    gl_Position = uProjection * uView * vInstanceMatrix * vec4(scaleVertex, 1.0);
}
