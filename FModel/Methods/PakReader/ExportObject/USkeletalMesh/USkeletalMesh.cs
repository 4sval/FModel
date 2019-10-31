using System;
using System.IO;
using System.Linq;

namespace PakReader
{
    public sealed class USkeletalMesh : ExportObject
    {
        public UObject BaseObject;
        public FBoxSphereBounds Bounds;
        public FSkeletalMaterial[] Materials;
        public FReferenceSkeleton RefSkeleton;
        public FStaticLODModel[] LODModels;
        public FSkeletalMeshLODInfo[] LODInfo;

        public string[] MaterialAssets;

        internal USkeletalMesh(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            BaseObject = new UObject(reader, name_map, import_map, "SkeletalMesh", true);
            bool has_vertex_colors = false;
            foreach (var prop in BaseObject.properties)
            {
                if (prop.name == "bHasVertexColors" && prop.tag == FPropertyTagType.BoolProperty)
                {
                    has_vertex_colors = (bool)prop.tag_data;
                }
                else if (prop.name == "LODInfo")
                {
                    var data = ((UScriptArray)prop.tag_data).data;
                    LODInfo = new FSkeletalMeshLODInfo[data.Length];
                    for (int i = 0; i < data.Length; i++)
                    {
                        var info = (UScriptStruct)data[i];
                        if (info.struct_name != "SkeletalMeshLODInfo")
                        {
                            throw new FileLoadException("Invalid lod info type");
                        }
                        var props = ((FStructFallback)info.struct_type).properties;
                        var newInfo = new FSkeletalMeshLODInfo();
                        foreach (var lodProp in props)
                        {
                            switch (lodProp.name)
                            {
                                case "DisplayFactor":
                                    newInfo.DisplayFactor = (float)lodProp.tag_data;
                                    break;
                                case "LODHysteresis":
                                    newInfo.LODHysteresis = (float)lodProp.tag_data;
                                    break;
                                case "LODMaterialMap":
                                    newInfo.LODMaterialMap = ((UScriptArray)lodProp.tag_data).data.Cast<int>().ToArray();
                                    break;
                                case "bEnableShadowCasting":
                                    newInfo.bEnableShadowCasting = ((UScriptArray)lodProp.tag_data).data.Cast<bool>().ToArray();
                                    break;
                            }
                        }
                        LODInfo[i] = newInfo;
                    }
                }
            }
            var flags = new FStripDataFlags(reader);
            Bounds = new FBoxSphereBounds(reader);
            Materials = reader.ReadTArray(() => new FSkeletalMaterial(reader, name_map, import_map));
            RefSkeleton = new FReferenceSkeleton(reader, name_map);

            if (!flags.editor_data_stripped)
            {
                Console.WriteLine("Editor data still present!");
            }

            if (reader.ReadUInt32() == 0)
            {
                throw new FileLoadException("No cooked data");
            }
            LODModels = reader.ReadTArray(() => new FStaticLODModel(reader, name_map, has_vertex_colors));

            uint serialize_guid = reader.ReadUInt32();

            MaterialAssets = new string[Materials.Length];
            for (int i = 0; i < Materials.Length; i++)
            {
                if (Materials[i].Material.import == null) continue;
                for (int j = 0; j < import_map.Length; j++)
                {
                    if (import_map[j].class_name != "MaterialInstanceConstant" && import_map[j].object_name.EndsWith(Materials[i].Material.import))
                    {
                        MaterialAssets[i] = import_map[j].object_name;
                        break;
                    }
                }
            }
        }
    }
}
