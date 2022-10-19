using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace FModel.Views.Snooper;

public class Cache : IDisposable
{
    private readonly Dictionary<FGuid, Model> _models;
    private readonly Dictionary<FGuid, Texture> _textures;

    public Cache()
    {
        _models = new Dictionary<FGuid, Model>();
        _textures = new Dictionary<FGuid, Texture>();
    }

    public void AddModel(FGuid guid, Model model) => _models.Add(guid, model);
    public void AddTexture(FGuid guid, Texture texture) => _textures.Add(guid, texture);

    public bool HasModel(FGuid guid) => _models.ContainsKey(guid);
    public bool HasTexture(FGuid guid) => _textures.ContainsKey(guid);

    public bool TryGetModel(FGuid guid, out Model model) => _models.TryGetValue(guid, out model);
    public bool TryGetTexture(FGuid guid, out Texture texture) => _textures.TryGetValue(guid, out texture);

    public void Setup()
    {
        foreach (var model in _models.Values)
        {
            if (model.IsSetup) continue;
            model.Setup();
        }
    }
    public void Render(Shader shader)
    {
        foreach (var model in _models.Values)
        {
            if (!model.Show) continue;
            model.Render(shader);
        }
    }
    public void Outline(Shader shader)
    {
        foreach (var model in _models.Values)
        {
            if (!model.IsSelected) continue;
            model.Outline(shader);
        }
    }

    public void ClearModels() => _models.Clear();
    public void ClearTextures() => _textures.Clear();

    public void DisposeModels()
    {
        foreach (var model in _models.Values)
        {
            model.Dispose();
        }
    }
    public void DisposeTextures()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }
    }

    public void Dispose()
    {
        DisposeModels();
        ClearModels();

        DisposeTextures();
        ClearTextures();
    }
}
