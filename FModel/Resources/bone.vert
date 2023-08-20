#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 9) in mat4 vInstanceMatrix;

uniform mat4 uView;
uniform mat4 uProjection;
uniform bool uSocket;

out vec3 fPos;

void main()
{
    float scale = 0.0075;
    mat4 result;
    result[0] = vec4(scale, 0.0, 0.0, 0.0);
    result[1] = vec4(0.0, scale, 0.0, 0.0);
    result[2] = vec4(0.0, 0.0, scale, 0.0);
    result[3] = vInstanceMatrix[3];

    gl_Position = uProjection * uView * result * vec4(vPos, 1.0);
    fPos = vec3(result * vec4(vPos, 1.0));
}
