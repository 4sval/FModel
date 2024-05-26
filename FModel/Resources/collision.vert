#version 460 core

layout (location = 0) in vec3 vPos;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uInstanceMatrix;
uniform mat4 uCollisionMatrix;
uniform float uScaleDown;

out vec3 fPos;
out vec3 fColor;

void main()
{
    gl_PointSize = 7.5f;
    gl_Position = uProjection * uView * uInstanceMatrix * uCollisionMatrix * vec4(vPos.xzy * uScaleDown, 1.0);
    fPos = vec3(uInstanceMatrix * uCollisionMatrix * vec4(vPos.xzy * uScaleDown, 1.0));
    fColor = vec3(1.0);
}
