#version 460 core

layout (location = 0) in vec3 vPos;
layout (location = 9) in mat4 vInstanceMatrix;

uniform mat4 uView;
uniform mat4 uProjection;

out vec2 fTexCoords;

void main()
{
    float scale = 0.075;
    mat4 result;
    result[0] = vec4(scale, 0.0, 0.0, 0.0);
    result[1] = vec4(0.0, scale, 0.0, 0.0);
    result[2] = vec4(0.0, 0.0, scale, 0.0);
    result[3] = vInstanceMatrix[3];

    gl_Position = uProjection * uView * result * vec4(inverse(mat3(uView)) * vPos, 1.0);
    fTexCoords = -vPos.xy * 0.5 + 0.5; // fits the whole rectangle
}
