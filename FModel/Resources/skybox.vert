#version 330 core

layout (location = 0) in vec3 vPos;

uniform mat4 uView;
uniform mat4 uProjection;

out vec3 fPos;

void main()
{
    fPos = vPos;
    vec4 pos = uProjection * uView * vec4(vPos, 1.0);

    gl_Position = pos.xyww;
}
