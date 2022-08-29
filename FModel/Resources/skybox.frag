#version 330 core

in vec3 fPos;

uniform samplerCube cubemap;

out vec4 FragColor;

void main()
{
    FragColor = texture(cubemap, fPos);
}
