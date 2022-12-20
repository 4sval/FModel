#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTarget;

uniform mat4 uView;
uniform vec3 uViewPos;
uniform mat4 uProjection;
uniform float uMorphTime;

void main()
{
    vec3 pos = vec3(vInstanceMatrix * vec4(mix(vPos, vMorphTarget, uMorphTime), 1.0));
    vec3 nor = mat3(transpose(inverse(vInstanceMatrix))) * vNormal;

    float scaleFactor = distance(pos, uViewPos) * 0.0025;
    vec3 scaleVertex = pos + nor * scaleFactor;
    gl_Position = uProjection * uView * vec4(scaleVertex, 1.0);
}
