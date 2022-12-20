#version 460 core

layout (location = 0) in vec2 vPos;
layout (location = 1) in vec2 vTexCoords;

out vec2 fTexCoords;

void main()
{
    gl_Position = vec4(vPos.x, vPos.y, 0.0, 1.0);
    fTexCoords = vTexCoords;
}
