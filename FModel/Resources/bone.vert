#version 460 core

layout (location = 0) in vec3 vPos;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uInstanceMatrix;

out vec3 fPos;

void main()
{
    gl_Position = uProjection * uView * uInstanceMatrix * vec4(vPos, 1.0);
    fPos = vec3(uInstanceMatrix * vec4(vPos, 1.0));
}
