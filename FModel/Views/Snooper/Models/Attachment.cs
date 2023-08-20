using System.Collections.Generic;
using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FModel.Views.Snooper.Models;

public class Attachment
{
    private string _modelName;
    private string _attachedTo;
    private readonly List<string> _attachedFor;
    private Matrix4x4 _oldRelation;

    public bool IsAttached => _attachedTo.Length > 0;
    public bool IsAttachment => _attachedFor.Count > 0;

    public string Icon => IsAttachment ? "link_has" : IsAttached ? "link_on" : "link_off";
    public string Tooltip => IsAttachment ? $"Is Attachment For:\n{string.Join("\n", _attachedFor)}" : IsAttached ? $"Is Attached To {_attachedTo}" : "Not Attached To Any Socket Nor Attachment For Any Model";

    public Attachment(string modelName)
    {
        _modelName = modelName;
        _attachedTo = string.Empty;
        _attachedFor = new List<string>();
    }

    public void Attach(UModel attachedTo, Transform transform, Socket socket, SocketAttachementInfo info)
    {
        socket.AttachedModels.Add(info);

        _attachedTo = $"'{socket.Name}' from '{attachedTo.Name}'{(!socket.BoneName.IsNone ? $" at '{socket.BoneName}'" : "")}";
        attachedTo.Attachments.AddAttachment(_modelName);

        // reset PRS to 0 so it's attached to the actual position (can be transformed relative to the socket later by the user)
        _oldRelation = transform.Relation;
        transform.Position = FVector.ZeroVector;
        transform.Rotation = FQuat.Identity;
        transform.Scale = FVector.OneVector;
    }

    public void Detach(UModel attachedTo, Transform transform, Socket socket, SocketAttachementInfo info)
    {
        socket.AttachedModels.Remove(info);
        SafeDetach(attachedTo, transform);
    }

    public void SafeDetach(UModel attachedTo, Transform transform)
    {
        _attachedTo = string.Empty;
        attachedTo.Attachments.RemoveAttachment(_modelName);

        transform.Relation = _oldRelation;
    }

    public void AddAttachment(string modelName) => _attachedFor.Add($"'{modelName}'");
    public void RemoveAttachment(string modelName) => _attachedFor.Remove($"'{modelName}'");
}
