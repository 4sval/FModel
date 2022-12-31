#version 460 core

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

    vec4 EmissiveRegion;
    float RoughnessMin;
    float RoughnessMax;
    float EmissiveMult;
};

struct BaseLight
{
    vec4 Color;
    vec3 Position;
    float Intensity;
};

//struct PointLight
//{
//    BaseLight Light;
//
//    float Linear;
//    float Quadratic;
//};
//
//struct SpotLight
//{
//    BaseLight Light;
//
//    float InnerConeAngle;
//    float OuterConeAngle;
//    float Attenuation;
//};

struct Light {
    BaseLight Base;

    float InnerConeAngle;
    float OuterConeAngle;
    float Attenuation;

    float Linear;
    float Quadratic;

    int Type; // 0 Point, 1 Spot
};

uniform Parameters uParameters;
uniform Light uLights[MAX_LIGHT_COUNT];
uniform int uNumLights;
uniform int uUvCount;
uniform bool uHasVertexColors;
uniform bool bVertexColors[6];
uniform vec3 uViewPos;

out vec4 FragColor;

int LayerToIndex()
{
    return clamp(int(fTexLayer), 0, uUvCount - 1);
}

vec4 SamplerToVector(sampler2D s, vec2 coords)
{
    return texture(s, coords);
}

vec4 SamplerToVector(sampler2D s)
{
    return SamplerToVector(s, fTexCoords);
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

vec3 schlickFresnel(vec3 fLambert, float metallic, float hDotv)
{
    vec3 f0 = vec3(0.04);
    f0 = mix(f0, fLambert, metallic);
    return f0 + (1.0 - f0) * pow(clamp(1.0 - hDotv, 0.0, 1.0), 5);
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

vec3 CalcLight(int layer, vec3 normals, vec3 position, vec3 color, float attenuation, bool global)
{
    vec3 fLambert = SamplerToVector(uParameters.Diffuse[layer].Sampler).rgb * uParameters.Diffuse[layer].Color.rgb;
    vec3 specular_masks = SamplerToVector(uParameters.SpecularMasks[layer].Sampler).rgb;
    float roughness = mix(uParameters.RoughnessMin, uParameters.RoughnessMax, specular_masks.b);

    vec3 l = normalize(uViewPos - fPos);

    vec3 n = normals;
    vec3 v = normalize(position - fPos);
    vec3 h = normalize(v + l);

    float nDotH = max(dot(n, h), 0.0);
    float hDotv = max(dot(h, v), 0.0);
    float nDotL = max(dot(n, l), 0.0);
    float nDotV = max(dot(n, v), 0.0);

    vec3 f = schlickFresnel(fLambert, specular_masks.g, hDotv);

    vec3 kS = f;
    vec3 kD = 1.0 - kS;
    kD *= 1.0 - specular_masks.g;

    vec3 specBrdfNom = ggxDistribution(roughness, nDotH) * geomSmith(roughness, nDotL) * geomSmith(roughness, nDotV) * f;
    float specBrdfDenom = 4.0 * nDotV * nDotL + 0.0001;
    vec3 specBrdf = specBrdfNom / specBrdfDenom;

    vec3 diffuseBrdf = fLambert;
    if (!global) diffuseBrdf = kD * fLambert / PI;
    return (diffuseBrdf + specBrdf) * color * attenuation * nDotL;
}

vec3 CalcBaseLight(int layer, vec3 normals, BaseLight base, float attenuation, bool global)
{
    return CalcLight(layer, normals, base.Position, base.Color.rgb * base.Intensity, attenuation, global);
}

vec3 CalcPointLight(int layer, vec3 normals, Light light)
{
    float distanceToLight = length(light.Base.Position - fPos);
    float attenuation = 1.0 / (1.0 + light.Linear * distanceToLight + light.Quadratic * pow(distanceToLight, 2));
    return CalcBaseLight(layer, normals, light.Base, attenuation, true);
}

vec3 CalcSpotLight(int layer, vec3 normals, Light light)
{
    vec3 v = normalize(light.Base.Position - fPos);
    float inner = cos(radians(light.InnerConeAngle));
    float outer = cos(radians(light.OuterConeAngle));

    float distanceToLight = length(light.Base.Position - fPos);
    float theta = dot(v, normalize(-vec3(0, -1, 0)));
    float epsilon = inner - outer;
    float attenuation = 1.0 / (1.0 + light.Attenuation * pow(distanceToLight, 2));
    light.Base.Intensity *= smoothstep(0.0, 1.0, (theta - outer) / epsilon);

    if(theta > outer)
    {
        return CalcBaseLight(layer, normals, light.Base, attenuation, true);
    }
    else
    {
        return vec3(0.0);
    }
}

void main()
{
    if (bVertexColors[2] && uHasVertexColors)
    {
        FragColor = fColor;
    }
    else if (bVertexColors[3])
    {
        int layer = LayerToIndex();
        vec3 normals = ComputeNormals(layer);
        FragColor = vec4(normals, 1);
    }
    else if (bVertexColors[4])
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
            result = vec3(uParameters.Ao.AmbientOcclusion) * result * m.r;
            result += CalcLight(layer, normals, vec3(0.0), vec3(0.25), m.g, false);
        }

        vec2 coords = fTexCoords;
        if (coords.x > uParameters.EmissiveRegion.x &&
            coords.y > uParameters.EmissiveRegion.y &&
            coords.x < uParameters.EmissiveRegion.z &&
            coords.y < uParameters.EmissiveRegion.w)
        {
            coords.x -= uParameters.EmissiveRegion.x;
            coords.y -= uParameters.EmissiveRegion.y;
            coords.x *= 1.0 / (uParameters.EmissiveRegion.z - uParameters.EmissiveRegion.x);
            coords.y *= 1.0 / (uParameters.EmissiveRegion.w - uParameters.EmissiveRegion.y);
            vec4 emissive = SamplerToVector(uParameters.Emissive[layer].Sampler, coords);
            result += uParameters.Emissive[layer].Color.rgb * emissive.rgb * uParameters.EmissiveMult;
        }

        if (!bVertexColors[1])
        {
            result += CalcLight(layer, normals, uViewPos, vec3(0.75), 1.0, false);

            vec3 lights = vec3(uNumLights > 0 ? 0 : 1);
            for (int i = 0; i < uNumLights; i++)
            {
                if (uLights[i].Type == 0)
                {
                    lights += CalcPointLight(layer, normals, uLights[i]);
                }
                else if (uLights[i].Type == 1)
                {
                    lights += CalcSpotLight(layer, normals, uLights[i]);
                }
            }
            result *= lights; // use * to darken the scene, + to lighten it
        }

        result = result / (result + vec3(1.0));
        FragColor = vec4(pow(result, vec3(1.0 / 2.2)), 1.0);
    }
}
