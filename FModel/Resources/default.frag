#version 330 core
#define MAX_UV_COUNT 8

in vec3 fPos;
in vec3 fNormal;
in vec3 fTangent;
in vec2 fTexCoords;
in float fTexLayer;
in vec4 fColor;

struct Texture
{
    sampler2D Sampler;
    vec4 Color;
};

struct Boost
{
    vec3 Color;
    float Exponent;
};

struct Mask
{
    sampler2D Sampler;
    Boost SkinBoost;

    float AmbientOcclusion;
    float Cavity;
};

struct Parameters
{
    Texture Diffuse[MAX_UV_COUNT];
    Texture Normals[MAX_UV_COUNT];
    Texture SpecularMasks[MAX_UV_COUNT];
    Texture Emissive[MAX_UV_COUNT];

    Mask M;
    bool HasM;

    float Roughness;
    float SpecularMult;
    float EmissiveMult;

    float UVScale;
};

uniform Parameters uParameters;
uniform int uNumTexCoords;
uniform vec3 uViewPos;
uniform bool bVertexColors;
uniform bool bVertexNormals;
uniform bool bVertexTangent;
uniform bool bVertexTexCoords;

out vec4 FragColor;

int LayerToIndex()
{
    return min(int(fTexLayer), uNumTexCoords - 1);
}

vec4 SamplerToVector(sampler2D s)
{
    return texture(s, fTexCoords * uParameters.UVScale);
}

vec3 ComputeNormals(int layer)
{
    vec3 normal = SamplerToVector(uParameters.Normals[layer].Sampler).rgb * 2.0 - 1.0;

    vec3 t  = normalize(fTangent);
    vec3 n  = normalize(fNormal);
    vec3 b  = -normalize(cross(n, t));
    mat3 tbn = mat3(t, b, n);

    return normalize(tbn * normal);
}

void main()
{
    if (bVertexColors)
    {
        FragColor = fColor;
    }
    else if (bVertexNormals)
    {
        FragColor = vec4(fNormal, 1);
    }
    else if (bVertexTangent)
    {
        FragColor = vec4(fTangent, 1);
    }
    else if (bVertexTexCoords)
    {
        FragColor = vec4(fTexCoords, 0, 1);
    }
    else
    {
        int layer = LayerToIndex();
        vec4 diffuse = SamplerToVector(uParameters.Diffuse[layer].Sampler);
        vec3 result = uParameters.Diffuse[layer].Color.rgb * diffuse.rgb;

        vec3 normals = ComputeNormals(layer);
        vec3 light_direction = normalize(uViewPos - fPos);
        result += max(dot(normals, light_direction), 0.0f) * result;

        if (uParameters.HasM)
        {
            vec3 m = SamplerToVector(uParameters.M.Sampler).rgb;
            float subsurface = clamp(m.b * .04f, 0.0f, 1.0f);

            if (subsurface > 0.0f && uParameters.M.SkinBoost.Exponent > 0.0f)
            {
                vec3 color = pow(uParameters.M.SkinBoost.Exponent, 2) * uParameters.M.SkinBoost.Color;
                result *= clamp(color * m.b, 0.0f, 1.0f);
            }

            result *= m.r * uParameters.M.AmbientOcclusion;
            result += m.g * uParameters.M.Cavity;
        }

        vec3 reflect_direction = reflect(-light_direction, normals);
        vec3 specular_masks = SamplerToVector(uParameters.SpecularMasks[layer].Sampler).rgb;
        float specular = uParameters.SpecularMult * max(0.0f, specular_masks.r);
        float metallic = max(0.0f, dot(light_direction, reflect_direction) * specular_masks.g);
        float roughness = max(0.0f, uParameters.Roughness * specular_masks.b);
        result += metallic * roughness * specular;

        vec4 emissive = SamplerToVector(uParameters.Emissive[layer].Sampler);
        result += uParameters.Emissive[layer].Color.rgb * emissive.rgb * uParameters.EmissiveMult;

        FragColor = vec4(result, 1.0f);
    }
}
