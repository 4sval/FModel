#version 330 core

#define PI 3.1415926535897932384626433832795
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

    float EmissiveMult;

    float UVScale;
};

uniform Parameters uParameters;
uniform int uNumTexCoords;
uniform vec3 uViewPos;
uniform vec3 uViewDir;
uniform bool bDiffuseOnly;
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

vec3 schlickFresnel(int layer, float vDotH)
{
    vec3 f0 = vec3(0.04f);
    return f0 + (1.0f - f0) * pow(clamp(1.0f - vDotH, 0.0f, 1.0f), 5);
}

float ggxDistribution(float roughness, float nDoth)
{
    float alpha2 = roughness * roughness * roughness * roughness;
    float d = nDoth * nDoth * (alpha2- 1.0f) + 1.0f;
    return alpha2 / (PI * d * d);
}

float geomSmith(float roughness, float dp)
{
    float k = (roughness + 1.0f) * (roughness + 1.0f) / 8.0f;
    float denom = dp * (1.0f - k) + k;
    return dp / denom;
}

vec3 CalcPBRLight(int layer, vec3 normals)
{
    vec3 specular_masks = SamplerToVector(uParameters.SpecularMasks[layer].Sampler).rgb;
    float roughness = max(0.0f, specular_masks.b);

    vec3 intensity = vec3(1.0f) * 1.0f;
    vec3 l = -uViewDir;

    vec3 n = normals;
    vec3 v = normalize(uViewPos - fPos);
    vec3 h = normalize(v + l);

    float nDotH = max(dot(n, h), 0.0f);
    float vDotH = max(dot(v, h), 0.0f);
    float nDotL = max(dot(n, l), 0.0f);
    float nDotV = max(dot(n, v), 0.0f);

    vec3 f = schlickFresnel(layer, vDotH);

    vec3 kS = f;
    vec3 kD = 1.0 - kS;
    kD *= 1.0 - max(0.0f, dot(v, reflect(-v, normals)) * specular_masks.g);

    vec3 specBrdfNom = ggxDistribution(roughness, nDotH) * f * geomSmith(roughness, nDotL) * geomSmith(roughness, nDotV);
    float specBrdfDenom = 4.0f * nDotV * nDotL + 0.0001f;
    vec3 specBrdf = specBrdfNom / specBrdfDenom;

    vec3 fLambert = SamplerToVector(uParameters.Diffuse[layer].Sampler).rgb * uParameters.Diffuse[layer].Color.rgb;

    vec3 diffuseBrdf = kD * fLambert / PI;
    return (diffuseBrdf + specBrdf) * intensity * nDotL;
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
        vec3 normals = ComputeNormals(layer);
        vec4 diffuse = SamplerToVector(uParameters.Diffuse[layer].Sampler);
        vec3 result = uParameters.Diffuse[layer].Color.rgb * diffuse.rgb;

        if (uParameters.HasM)
        {
            vec3 m = SamplerToVector(uParameters.M.Sampler).rgb;
            float subsurface = clamp(m.b * .04f, 0.0f, 1.0f);

            if (subsurface > 0.0f && uParameters.M.SkinBoost.Exponent > 0.0f)
            {
                vec3 color = uParameters.M.SkinBoost.Color * pow(uParameters.M.SkinBoost.Exponent, uParameters.M.SkinBoost.Exponent);
                result *= clamp(color * m.b, 0.0f, 1.0f);
            }

            if (m.r > 0.0f) result *= m.r * uParameters.M.AmbientOcclusion;
            if (m.g > 0.0f) result += m.g * uParameters.M.Cavity;
        }

        vec4 emissive = SamplerToVector(uParameters.Emissive[layer].Sampler);
        result += uParameters.Emissive[layer].Color.rgb * emissive.rgb * uParameters.EmissiveMult;

        if (!bDiffuseOnly)
        {
            result += CalcPBRLight(layer, normals);
        }

        result = result / (result + vec3(1.0f));
        FragColor = vec4(pow(result, vec3(1.0f / 2.2f)), 1.0f);
    }
}
