#version 460 core

layout (location = 1) in vec3 vPos;
layout (location = 7) in vec4 vBoneInfluence;
layout (location = 8) in vec4 vBoneInfluenceExtra;
layout (location = 9) in mat4 vInstanceMatrix;
layout (location = 13) in vec3 vMorphTargetPos;

layout(std430, binding = 1) buffer BoneMatrices
{
    mat4 uFinalBonesMatrix[];
};
layout(std430, binding = 2) buffer RestBoneMatrices
{
    mat4 uRestBonesMatrix[];
};

uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorphTime;
uniform bool uIsAnimated;

vec2 unpackBoneIDsAndWeights(int packedData)
{
    return vec2(float((packedData >> 16) & 0xFFFF), float(packedData & 0xFFFF));
}

void main()
{
    vec4 bindPos = vec4(mix(vPos, vMorphTargetPos, uMorphTime), 1.0);

    vec4 finalPos = vec4(0.0);
    if (uIsAnimated)
    {
        vec4 boneInfluences[2];
        boneInfluences[0] = vBoneInfluence;
        boneInfluences[1] = vBoneInfluenceExtra;
        for(int i = 0 ; i < 2; i++)
        {
            for(int j = 0; j < 4; j++)
            {
                vec2 boneInfluence = unpackBoneIDsAndWeights(int(boneInfluences[i][j]));
                int boneIndex = int(boneInfluence.x);
                float weight = boneInfluence.y;

                finalPos += uFinalBonesMatrix[boneIndex] * inverse(uRestBonesMatrix[boneIndex]) * bindPos * weight;
            }
        }
    }
    else finalPos = bindPos;

    gl_Position = uProjection * uView * vInstanceMatrix * finalPos;
}
