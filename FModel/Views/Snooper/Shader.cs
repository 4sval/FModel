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

out vec3 fNormal;
out vec3 fPos;
out vec2 fTexCoords;

void main()
{
    //Multiplying our uniform with the vertex position, the multiplication order here does matter.
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
    //We want to know the fragment's position in World space, so we multiply ONLY by uModel and not uView or uProjection
    fPos = vec3(uModel * vec4(vPos, 1.0));
    //The Normal needs to be in World space too, but needs to account for Scaling of the object
    fNormal = mat3(transpose(inverse(uModel))) * vNormal;
    //Pass the texture coordinates straight through to the fragment shader
    fTexCoords = vTexCoords;
}
        ";

    private readonly string FragmentShaderSource = @"
#version 330 core

in vec3 fNormal;
in vec3 fPos;
in vec2 fTexCoords;

uniform vec3 viewPos;

out vec4 FragColor;

void main()
{
    vec3 viewDirection = normalize(viewPos - fPos);
    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
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
