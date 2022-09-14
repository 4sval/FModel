#version 330 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 7) in mat4 vInstanceMatrix;

uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 viewPos;

void main()
{
    float scaleFactor = distance(vPos, viewPos) * 0.0025;
    vec3 scaleVertex = vPos + vNormal * scaleFactor;
    gl_Position = uProjection * uView * vInstanceMatrix * vec4(scaleVertex, 1.0);
}
