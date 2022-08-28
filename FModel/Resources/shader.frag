#version 330 core

in vec3 fPos;
in vec3 fNormal;
in vec2 fTexCoords;

struct Material {
    sampler2D diffuse;
    sampler2D normal;
    sampler2D specular;
    sampler2D metallic;
    sampler2D emission;

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
    // ambient
    vec3 ambient = light.ambient * vec3(texture(material.diffuse, fTexCoords));

    // diffuse
    vec3 norm = texture(material.normal, fTexCoords).rgb;
    norm = normalize(norm * 2.0 - 1.0);
    vec3 lightDir = normalize(light.position - fPos);
    float diff = max(dot(norm, lightDir), 0.0f);
    vec3 diffuseMap = vec3(texture(material.diffuse, fTexCoords));
    vec3 diffuse = light.diffuse * diff * diffuseMap;

    // specular
    vec3 viewDir = normalize(viewPos - fPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0f), material.shininess);
    vec3 specularMap = vec3(texture(material.specular, fTexCoords));
    vec3 specular = light.specular * spec * specularMap;

    vec3 result = ambient + diffuse + specular;
    FragColor = vec4(result, 1.0);
}
