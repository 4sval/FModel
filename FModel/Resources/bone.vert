#version 460 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vColor;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uInstanceMatrix;

out vec3 fPos;
out vec3 fColor;

void main()
{
    gl_PointSize = 7.5f;
    gl_Position = uProjection * uView * uInstanceMatrix * vec4(vPos, 1.0);
    fPos = vec3(uInstanceMatrix * vec4(vPos, 1.0));
    fColor = vColor;
}
