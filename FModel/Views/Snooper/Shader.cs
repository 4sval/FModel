using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace FModel.Views.Snooper;

public class Shader : IDisposable
{
    private readonly int _handle;
    private readonly Dictionary<string, int> _uniformToLocation = new ();
    private readonly Dictionary<string, int> _attribLocation = new ();

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

    public void Use()
    {
        GL.UseProgram(_handle);
    }

    public void SetUniform(string name, int value)
    {
        int location = GL.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        GL.Uniform1(location, value);
    }

    public unsafe void SetUniform(string name, Matrix4 value)
    {
        //A new overload has been created for setting a uniform so we can use the transform in our shader.
        int location = GL.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        GL.UniformMatrix4(location, 1, false, (float*) &value);
    }

    public void SetUniform(string name, bool value) => SetUniform(name, Convert.ToUInt32(value));

    public void SetUniform(string name, uint value)
    {
        int location = GL.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        GL.Uniform1(location, value);
    }

    public void SetUniform(string name, float value)
    {
        int location = GL.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        GL.Uniform1(location, value);
    }

    public void SetUniform(string name, Vector3 value)
    {
        int location = GL.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        GL.Uniform3(location, value.X, value.Y, value.Z);
    }

    public void SetUniform(string name, Vector4 value)
    {
        int location = GL.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }

    public int GetUniformLocation(string uniform)
    {
        if (!_uniformToLocation.TryGetValue(uniform, out int location))
        {
            location = GL.GetUniformLocation(_handle, uniform);
            _uniformToLocation.Add(uniform, location);
            if (location == -1)
            {
                Serilog.Log.Debug($"The uniform '{uniform}' does not exist in the shader!");
            }
        }
        return location;
    }

    public int GetAttribLocation(string attrib)
    {
        if (!_attribLocation.TryGetValue(attrib, out int location))
        {
            location = GL.GetAttribLocation(_handle, attrib);
            _attribLocation.Add(attrib, location);
            if (location == -1)
            {
                Serilog.Log.Debug($"The attrib '{attrib}' does not exist in the shader!");
            }
        }
        return location;
    }

    public void Dispose()
    {
        GL.DeleteProgram(_handle);
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
}
