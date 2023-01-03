using System;
using FModel.Views.Snooper.Shading;

namespace FModel.Views.Snooper.Models;

public class Section
{
    public readonly int MaterialIndex;
    public readonly int FacesCount;
    public readonly int FirstFaceIndex;
    public readonly IntPtr FirstFaceIndexPtr;

    public bool Show;

    public Section(int index, int facesCount, int firstFaceIndex)
    {
        MaterialIndex = index;
        FacesCount = facesCount;
        FirstFaceIndex = firstFaceIndex;
        FirstFaceIndexPtr = new IntPtr(FirstFaceIndex * sizeof(uint));
        Show = true;
    }

    public void SetupMaterial(Material material)
    {
        material.IsUsed = true;
        Show = !material.Parameters.IsNull && !material.Parameters.IsTranslucent;
    }
}
