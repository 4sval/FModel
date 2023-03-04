namespace FModel.Views.Snooper.Animations;

public class BoneIndice
{
    public int BoneIndex = -1;
    public int ParentBoneIndex = -1;
    public string LoweredParentBoneName;
    public bool IsRoot => BoneIndex == 0 && ParentBoneIndex == -1 && string.IsNullOrEmpty(LoweredParentBoneName);

    public int TrackedBoneIndex = -1;
    public int TrackedParentBoneIndex = -1; // bone index of the first tracked parent bone
    public bool IsTracked => TrackedBoneIndex > -1;
    public bool IsParentTracked => TrackedParentBoneIndex > -1;

    public bool IsNative => BoneIndex == TrackedBoneIndex;
    public bool IsParentNative => ParentBoneIndex == TrackedParentBoneIndex; // always true?

    public override string ToString() => $"Mesh Ref '{BoneIndex}' is Skel Ref '{TrackedBoneIndex}' ({IsNative}, {IsParentNative})";
}
