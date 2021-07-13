using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FModel.ViewModels;

namespace FModel.Views
{
    public partial class ModelViewer
    {
        public ModelViewer(UObject export)
        {
            DataContext = export switch
            {
                UStaticMesh st => new ModelViewerViewModel(st),
                USkeletalMesh sk => new ModelViewerViewModel(sk),
                _ => throw new NotImplementedException()
            };

            InitializeComponent();
        }
    }
}