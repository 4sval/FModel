using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using Silk.NET.OpenGL;

namespace FModel.Views.Snooper;

public class Shader : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;
    private readonly Dictionary<string, int> _uniformToLocation = new ();
    private readonly Dictionary<string, int> _attribLocation = new ();

    public Shader(GL gl) : this(gl, "default") {}

    public Shader(GL gl, string name)
    {
        _gl = gl;

        _handle = _gl.CreateProgram();

        uint v = LoadShader(ShaderType.VertexShader, $"{name}.vert");
        uint f = LoadShader(ShaderType.FragmentShader, $"{name}.frag");
        _gl.AttachShader(_handle, v);
        _gl.AttachShader(_handle, f);
        _gl.LinkProgram(_handle);
        _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");
        }
        _gl.DetachShader(_handle, v);
        _gl.DetachShader(_handle, f);
        _gl.DeleteShader(v);
        _gl.DeleteShader(f);
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

    public void SetUniform(string name, bool value) => SetUniform(name, Convert.ToUInt32(value));

    public void SetUniform(string name, uint value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        _gl.Uniform1(location, value);
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

    public void SetUniform(string name, Vector4 value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }

    public int GetUniformLocation(string uniform)
    {
        if (!_uniformToLocation.TryGetValue(uniform, out int location))
        {
            location = _gl.GetUniformLocation(_handle, uniform);
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
            location = _gl.GetAttribLocation(_handle, attrib);
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
        _gl.DeleteProgram(_handle);
    }

    private uint LoadShader(ShaderType type, string file)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        using var stream = executingAssembly.GetManifestResourceStream($"{executingAssembly.GetName().Name}.Resources.{file}");
        using var reader = new StreamReader(stream);
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, reader.ReadToEnd());
        _gl.CompileShader(handle);
        string infoLog = _gl.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        return handle;
    }
}
