namespace FModel.Views.Snooper.Models.Animations;

public class BoneIndice
{
    public int BoneIndex = -1;
    public int ParentBoneIndex = -1;
    public string LoweredParentBoneName;
    public bool IsRoot => BoneIndex == 0 && ParentBoneIndex == -1 && string.IsNullOrEmpty(LoweredParentBoneName);

    public int TrackIndex = -1;
    public int ParentTrackIndex = -1; // bone index of the first tracked parent bone
    public bool HasTrack => TrackIndex > -1;
    public bool HasParentTrack => ParentTrackIndex > -1;
}
