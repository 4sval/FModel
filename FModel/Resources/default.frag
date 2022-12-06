#version 330 core

#define PI 3.1415926535897932384626433832795
#define MAX_UV_COUNT 8
#define MAX_LIGHT_COUNT 100

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

struct AoParams
{
    sampler2D Sampler;
    float AmbientOcclusion;

    Boost ColorBoost;
    bool HasColorBoost;
};

struct Parameters
{
    Texture Diffuse[MAX_UV_COUNT];
    Texture Normals[MAX_UV_COUNT];
    Texture SpecularMasks[MAX_UV_COUNT];
    Texture Emissive[MAX_UV_COUNT];

    AoParams Ao;
    bool HasAo;

    float Roughness;
    float EmissiveMult;
    float UVScale;
};

struct Light {
    vec4 Color;
    vec3 Position;
    float Intensity;

    vec2 Direction;
    float ConeAngle;
    float Attenuation;

    float Linear;
    float Quadratic;

    int Type; // 0 Point, 1 Spot
};

uniform Parameters uParameters;
uniform Light uLights[MAX_LIGHT_COUNT];
uniform int uNumLights;
uniform int uNumTexCoords;
uniform bool uHasVertexColors;
uniform vec3 uViewPos;
uniform vec3 uViewDir;
uniform bool bVertexColors[6];

out vec4 FragColor;

int LayerToIndex()
{
    return clamp(int(fTexLayer), 0, uNumTexCoords - 1);
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
    vec3 f0 = vec3(0.04);
    return f0 + (1.0 - f0) * pow(clamp(1.0 - vDotH, 0.0, 1.0), 5);
}

float ggxDistribution(float roughness, float nDoth)
{
    float alpha2 = roughness * roughness * roughness * roughness;
    float d = nDoth * nDoth * (alpha2- 1.0) + 1.0;
    return alpha2 / (PI * d * d);
}

float geomSmith(float roughness, float dp)
{
    float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
    float denom = dp * (1.0 - k) + k;
    return dp / denom;
}

vec3 CalcCameraLight(int layer, vec3 normals)
{
    vec3 specular_masks = SamplerToVector(uParameters.SpecularMasks[layer].Sampler).rgb;
    float roughness = max(0.0f, mix(specular_masks.r, specular_masks.b, uParameters.Roughness));

    vec3 intensity = vec3(1.0f) * 1.0;
    vec3 l = -uViewDir;

    vec3 n = normals;
    vec3 v = normalize(uViewPos - fPos);
    vec3 h = normalize(v + l);

    float nDotH = max(dot(n, h), 0.0);
    float vDotH = max(dot(v, h), 0.0);
    float nDotL = max(dot(n, l), 0.0);
    float nDotV = max(dot(n, v), 0.0);

    vec3 f = schlickFresnel(layer, vDotH);

    vec3 kS = f;
    vec3 kD = 1.0 - kS;
    kD *= max(0.0, dot(v, reflect(-v, normals)) * specular_masks.g);

    vec3 specBrdfNom = ggxDistribution(roughness, nDotH) * f * geomSmith(roughness, nDotL) * geomSmith(roughness, nDotV);
    float specBrdfDenom = 4.0 * nDotV * nDotL + 0.0001;
    vec3 specBrdf = specBrdfNom / specBrdfDenom;

    vec3 fLambert = SamplerToVector(uParameters.Diffuse[layer].Sampler).rgb * uParameters.Diffuse[layer].Color.rgb;

    vec3 diffuseBrdf = kD * fLambert / PI;
    return (diffuseBrdf + specBrdf) * intensity * nDotL;
}

void main()
{
    if (bVertexColors[2] && uHasVertexColors)
    {
        FragColor = fColor;
    }
    else if (bVertexColors[3])
    {
        FragColor = vec4(fNormal, 1);
    }
    else if (bVertexColors[4])
    {
        FragColor = vec4(fTangent, 1);
    }
    else if (bVertexColors[5])
    {
        FragColor = vec4(fTexCoords, 0, 1);
    }
    else
    {
        int layer = LayerToIndex();
        vec3 normals = ComputeNormals(layer);
        vec4 diffuse = SamplerToVector(uParameters.Diffuse[layer].Sampler);
        vec3 result = uParameters.Diffuse[layer].Color.rgb * diffuse.rgb;

        if (uParameters.HasAo)
        {
            vec3 m = SamplerToVector(uParameters.Ao.Sampler).rgb;
            if (uParameters.Ao.HasColorBoost)
            {
                vec3 color = uParameters.Ao.ColorBoost.Color * uParameters.Ao.ColorBoost.Exponent;
                result = mix(result, result * color, m.b);
            }
            result = mix(result * m.r * uParameters.Ao.AmbientOcclusion, result, m.g);
        }

        vec4 emissive = SamplerToVector(uParameters.Emissive[layer].Sampler);
        result += uParameters.Emissive[layer].Color.rgb * emissive.rgb * uParameters.EmissiveMult;

        if (!bVertexColors[1])
        {
            result += CalcCameraLight(layer, normals);

            vec3 lights = vec3(uNumLights > 0 ? 0 : 1);
            for (int i = 0; i < uNumLights; i++)
            {
                float attenuation = 0.0;
                float distanceToLight = length(uLights[i].Position - fPos);

                if (uLights[i].Type == 0)
                {
                    attenuation = 1.0 / (1.0 + uLights[i].Linear * distanceToLight + uLights[i].Quadratic * pow(distanceToLight, 2));
                }
                else if (uLights[i].Type == 1)
                {
                    float theta = dot(normalize(uLights[i].Position - fPos), normalize(-vec3(uLights[i].Direction.x, -uLights[i].Attenuation, uLights[i].Direction.y)));
                    if(theta > uLights[i].ConeAngle)
                    {
                        attenuation = 1.0 / (1.0 + uLights[i].Attenuation * pow(distanceToLight, 2));
                    }
                }

                vec3 intensity = uLights[i].Color.rgb * uLights[i].Intensity;
                lights += result * intensity * attenuation;
            }
            result *= lights; // use * to darken the scene, + to lighten it
        }

        result = result / (result + vec3(1.0));
        FragColor = vec4(pow(result, vec3(1.0 / 2.2)), 1.0);
    }
}
