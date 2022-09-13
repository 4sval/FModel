#version 330 core

in vec3 fPos;
in vec3 fNormal;
in vec2 fTexCoords;
in vec4 fColor;

struct Material {
    sampler2D diffuseMap;
    sampler2D normalMap;
    sampler2D emissionMap;

    bool useSpecularMap;
    sampler2D specularMap;

    bool hasDiffuseColor;
    vec4 diffuseColor;

    vec4 emissionColor;

    float metallic_value;
    float roughness_value;
};

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform Material material;
uniform Light light;
uniform vec3 viewPos;
uniform bool display_vertex_colors;

out vec4 FragColor;

vec3 getNormalFromMap()
{
    vec3 tangentNormal = texture(material.normalMap, fTexCoords).xyz * 2.0 - 1.0;

    vec3 q1  = dFdx(fPos);
    vec3 q2  = dFdy(fPos);
    vec2 st1 = dFdx(fTexCoords);
    vec2 st2 = dFdy(fTexCoords);

    vec3 n   = normalize(fNormal);
    vec3 t  = normalize(q1*st2.t - q2*st1.t);
    vec3 b  = -normalize(cross(n, t));
    mat3 tbn = mat3(t, b, n);

    return normalize(tbn * tangentNormal);
}

void main()
{
    if (display_vertex_colors)
    {
        FragColor = fColor;
    }
    else
    {
        vec3 n_normal_map = getNormalFromMap();
        vec3 n_light_direction = normalize(light.position - fPos);
        float diff = max(dot(n_normal_map, n_light_direction), 0.0f);

        if (material.hasDiffuseColor)
        {
            vec4 result = vec4(light.ambient, 1.0) * material.diffuseColor;
            result += vec4(light.diffuse, 1.0) * diff * material.diffuseColor;
            FragColor = result;
        }
        else
        {
            vec3 result = light.ambient * vec3(texture(material.diffuseMap, fTexCoords));

            // diffuse
            vec4 diffuse_map = texture(material.diffuseMap, fTexCoords);
            result += light.diffuse * diff * diffuse_map.rgb;

            // specular
            if (material.useSpecularMap)
            {
                vec3 n_view_direction = normalize(viewPos - fPos);
                vec3 reflect_direction = reflect(-n_light_direction, n_normal_map);
                float metallic = pow(max(dot(n_view_direction, reflect_direction), 0.0f), material.metallic_value);
                vec3 specular_map = vec3(texture(material.specularMap, fTexCoords));
                result += light.specular * metallic * specular_map.b * material.roughness_value * specular_map.g * specular_map.r;
            }

            // emission
            vec3 emission_map = vec3(texture(material.emissionMap, fTexCoords));
            result += material.emissionColor.rgb * emission_map;

            FragColor = vec4(result, 1.0);
        }
    }
}
