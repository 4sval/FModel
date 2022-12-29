﻿#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTarget;

uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorphTime;

void main()
{
    vec3 pos = mix(vPos, vMorphTarget, uMorphTime);
    gl_Position = uProjection * uView * vInstanceMatrix * vec4(pos, 1.0);
}
