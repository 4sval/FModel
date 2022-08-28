#version 430 core

layout (location = 0) in vec3 vPos;

// --------------------- OUT ---------------------
out OUT_IN_VARIABLES {
    vec3 nearPoint;
    vec3 farPoint;
    mat4 proj;
    mat4 view;
    float near;
    float far;
} outVar;

uniform mat4 proj;
uniform mat4 view;
uniform float uNear;
uniform float uFar;

vec3 UnprojectPoint(vec2 xy, float z) {
    mat4 viewInv = inverse(view);
    mat4 projInv = inverse(proj);
    vec4 unprojectedPoint =  viewInv * projInv * vec4(xy, z, 1.0);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

void main()
{
    outVar.near = uNear;
    outVar.far  = uFar;
    outVar.proj = proj;
    outVar.view = view;
    outVar.nearPoint = UnprojectPoint(vPos.xy, -1.0).xyz;
    outVar.farPoint  = UnprojectPoint(vPos.xy, 1.0).xyz;
    gl_Position = vec4(vPos, 1.0f);
}
