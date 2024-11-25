using CUE4Parse.UE4.Assets;

namespace FModel.ViewModels.Nodify;

public class PackagedNodeViewModel(IPackage package) : BaseIndexedNodeViewModel(package.ExportsLazy.Length)
{
    protected override void OnArrayIndexChanged()
    {
        Input.Clear();
        Output.Clear();

        var export = package.ExportsLazy[ArrayIndex].Value;
        Title = export.Name;

        foreach (var property in export.Properties)
        {
            Input.Add(new ConnectorViewModel(property.Name.Text));
            Output.Add(new ConnectorViewModel(property.TagData));
            ConnectOutput(property.Tag);
        }
    }
}
