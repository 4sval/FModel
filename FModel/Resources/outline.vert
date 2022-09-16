#version 330 core

layout (location = 1) in vec3 vPos;
layout (location = 2) in vec3 vNormal;
layout (location = 7) in mat4 vInstanceMatrix;
layout (location = 11) in vec3 vMorphTarget;

uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorphTime;
uniform vec3 viewPos;

void main()
{
    vec3 pos = mix(vPos, vMorphTarget, uMorphTime);
    float scaleFactor = distance(pos, viewPos) * 0.0025;
    vec3 scaleVertex = pos + vNormal * scaleFactor;
    gl_Position = uProjection * uView * vInstanceMatrix * vec4(scaleVertex, 1.0);
}
