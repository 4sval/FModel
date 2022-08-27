using System;
using System.Numerics;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Shader : IDisposable
{
    private uint _handle;
    private GL _gl;

    private readonly string VertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoords;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 fPos;
out vec3 fNormal;
out vec2 fTexCoords;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);

    fPos = vec3(uModel * vec4(vPos, 1.0));
    fNormal = mat3(transpose(inverse(uModel))) * vNormal;
    fTexCoords = vTexCoords;
}
        ";

    private readonly string FragmentShaderSource = @"
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
        ";

    public Shader(GL gl)
    {
        _gl = gl;

        uint vertex = LoadShader(ShaderType.VertexShader, VertexShaderSource);
        uint fragment = LoadShader(ShaderType.FragmentShader, FragmentShaderSource);
        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, vertex);
        _gl.AttachShader(_handle, fragment);
        _gl.LinkProgram(_handle);
        _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");
        }
        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
    }

    public void Use()
    {
        _gl.UseProgram(_handle);
    }

    public void SetUniform(string name, int value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        _gl.Uniform1(location, value);
    }

    public unsafe void SetUniform(string name, Matrix4x4 value)
    {
        //A new overload has been created for setting a uniform so we can use the transform in our shader.
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        _gl.UniformMatrix4(location, 1, false, (float*) &value);
    }

    public void SetUniform(string name, float value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, Vector3 value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        _gl.Uniform3(location, value.X, value.Y, value.Z);
    }

    public void Dispose()
    {
        _gl.DeleteProgram(_handle);
    }

    private uint LoadShader(ShaderType type, string content)
    {
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, content);
        _gl.CompileShader(handle);
        string infoLog = _gl.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        return handle;
    }
}
