using System.Collections.Generic;

namespace FModel.Views.Snooper.Animations;

public class Bone
{
    public readonly int Index;
    public int ParentIndex;
    public readonly Transform Rest;
    public readonly bool IsVirtual;

    public string LoweredParentName;
    public List<string> LoweredChildNames;

    public int SkeletonIndex = -1;
    public readonly List<int> AnimatedBySequences;

    public Bone(int i, int p, Transform t, bool isVirtual = false)
    {
        Index = i;
        ParentIndex = p;
        Rest = t;
        IsVirtual = isVirtual;

        LoweredChildNames = new List<string>();
        AnimatedBySequences = new List<int>();
    }

    public bool IsRoot => Index == 0 && ParentIndex == -1 && string.IsNullOrEmpty(LoweredParentName);
    public bool IsDaron => LoweredChildNames.Count > 0;
    public bool IsMapped => SkeletonIndex > -1;
    public bool IsAnimated => AnimatedBySequences.Count > 0;
    public bool IsNative => Index == SkeletonIndex;
    public uint Color => !IsMapped || !IsAnimated ? 0xFFA0A0A0 : 0xFF48B048;

    public override string ToString() => $"Mesh Ref '{Index}' is Skel Ref '{SkeletonIndex}'";
}
