#version 330 core

in vec3 fPos;
in vec3 fNormal;
in vec2 fTexCoords;

struct Material {
    sampler2D diffuseMap;
    sampler2D normalMap;
    sampler2D specularMap;
    sampler2D metallicMap;
    sampler2D emissionMap;

    bool swap;

    vec4 diffuseColor;
    vec4 emissionColor;
    float shininess;
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

out vec4 FragColor;

void main()
{
    if (material.swap)
    {
        FragColor = material.diffuseColor;
        return;
    }

    // ambient
    vec3 ambient = light.ambient * vec3(texture(material.diffuseMap, fTexCoords));

    // diffuse
    vec3 norm = texture(material.normalMap, fTexCoords).rgb;
    norm = normalize(norm * 2.0 - 1.0);
    vec3 lightDir = normalize(light.position - fPos);
    float diff = max(dot(norm, lightDir), 0.0f);
    vec3 diffuseMap = vec3(texture(material.diffuseMap, fTexCoords));
    vec3 diffuse = light.diffuse * diff * diffuseMap;

    // specular
    vec3 viewDir = normalize(viewPos - fPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0f), material.shininess);
    vec3 specularMap = vec3(texture(material.specularMap, fTexCoords));
    vec3 specular = light.specular * spec * specularMap;

    // emission
    vec3 emissionMap = vec3(texture(material.emissionMap, fTexCoords));
    vec3 emission = material.emissionColor.rgb * emissionMap;

    vec3 result = ambient + diffuse + specular + emission;
    FragColor = vec4(result, 1.0);
}
