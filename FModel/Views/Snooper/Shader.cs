using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;

namespace FModel.Views.Snooper;

public class Shader : IDisposable
{
    private readonly int _handle;
    private readonly Dictionary<string, int> _uniformsLocation = new ();

    public Shader() : this("default") {}

    public Shader(string name)
    {
        _handle = GL.CreateProgram();

        var v = LoadShader(ShaderType.VertexShader, $"{name}.vert");
        var f = LoadShader(ShaderType.FragmentShader, $"{name}.frag");
        GL.AttachShader(_handle, v);
        GL.AttachShader(_handle, f);
        GL.LinkProgram(_handle);
        GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out var status);
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {GL.GetProgramInfoLog(_handle)}");
        }
        GL.DetachShader(_handle, v);
        GL.DetachShader(_handle, f);
        GL.DeleteShader(v);
        GL.DeleteShader(f);
    }

    private int LoadShader(ShaderType type, string file)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        using var stream = executingAssembly.GetManifestResourceStream($"{executingAssembly.GetName().Name}.Resources.{file}");
        using var reader = new StreamReader(stream);
        var handle = GL.CreateShader(type);
        GL.ShaderSource(handle, reader.ReadToEnd());
        GL.CompileShader(handle);
        string infoLog = GL.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        return handle;
    }

    public void Use()
    {
        GL.UseProgram(_handle);
    }

    public void Render(Matrix4x4 viewMatrix, Vector3 viewPos, Vector3 viewDir, Matrix4x4 projMatrix)
    {
        Render(viewMatrix, viewPos, projMatrix);
        SetUniform("uViewDir", viewDir);
    }
    public void Render(Matrix4x4 viewMatrix, Vector3 viewPos, Matrix4x4 projMatrix)
    {
        Render(viewMatrix, projMatrix);
        SetUniform("uViewPos", viewPos);
    }
    public void Render(Matrix4x4 viewMatrix, Matrix4x4 projMatrix)
    {
        Use();
        SetUniform("uView", viewMatrix);
        SetUniform("uProjection", projMatrix);
    }

    public void SetUniform(string name, int value)
    {
        GL.Uniform1(GetUniformLocation(name), value);
    }

    public unsafe void SetUniform(string name, Matrix4x4 value) => UniformMatrix4(name, (float*) &value);
    public unsafe void UniformMatrix4(string name, float* value)
    {
        GL.UniformMatrix4(GetUniformLocation(name), 1, false, value);
    }

    public void SetUniform(string name, bool value) => SetUniform(name, Convert.ToUInt32(value));

    public void SetUniform(string name, uint value)
    {
        GL.Uniform1(GetUniformLocation(name), value);
    }

    public void SetUniform(string name, float value)
    {
        GL.Uniform1(GetUniformLocation(name), value);
    }

    public void SetUniform(string name, Vector2 value) => SetUniform3(name, value.X, value.Y);
    public void SetUniform3(string name, float x, float y)
    {
        GL.Uniform2(GetUniformLocation(name), x, y);
    }

    public void SetUniform(string name, Vector3 value) => SetUniform3(name, value.X, value.Y, value.Z);
    public void SetUniform3(string name, float x, float y, float z)
    {
        GL.Uniform3(GetUniformLocation(name), x, y, z);
    }

    public void SetUniform(string name, Vector4 value) => SetUniform4(name, value.X, value.Y, value.Z, value.W);
    public void SetUniform4(string name, float x, float y, float z, float w)
    {
        GL.Uniform4(GetUniformLocation(name), x, y, z, w);
    }

    private int GetUniformLocation(string name)
    {
        if (!_uniformsLocation.TryGetValue(name, out int location))
        {
            location = GL.GetUniformLocation(_handle, name);
            _uniformsLocation.Add(name, location);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
        }
        return location;
    }

    public void Dispose()
    {
        GL.DeleteProgram(_handle);
    }
}
