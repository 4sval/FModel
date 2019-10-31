using System.IO;

namespace PakReader
{
    public struct UScriptStruct
    {
        public string struct_name;
        public object struct_type;

        internal UScriptStruct(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string struct_name)
        {
            this.struct_name = struct_name;
            switch (struct_name)
            {
                case "Vector2D":
                    struct_type = new FVector2D(reader);
                    break;
                case "LinearColor":
                    struct_type = new FLinearColor(reader);
                    break;
                case "Color":
                    struct_type = new FColor(reader);
                    break;
                case "GameplayTagContainer":
                    struct_type = new FGameplayTagContainer(reader, name_map);
                    break;
                case "IntPoint":
                    struct_type = new FIntPoint(reader);
                    break;
                case "Guid":
                    struct_type = new FGuid(reader);
                    break;
                case "Quat":
                    struct_type = new FQuat(reader);
                    break;
                case "Vector":
                    struct_type = new FVector(reader);
                    break;
                case "Rotator":
                    struct_type = new FRotator(reader);
                    break;
                case "SoftObjectPath":
                    struct_type = new FSoftObjectPath(reader, name_map);
                    break;
                case "LevelSequenceObjectReferenceMap":
                    struct_type = new FLevelSequenceObjectReferenceMap(reader);
                    break;
                case "FrameNumber":
                    struct_type = reader.ReadSingle();
                    break;/*
                case "SectionEvaluationDataTree":
                    struct_type = new FSectionEvaluationDataTree(reader, name_map, import_map);
                    break;
                case "MovieSceneTrackIdentifier":
                    struct_type = reader.ReadSingle();
                    break;
                case "MovieSceneSegment":
                    struct_type = new FMovieSceneSegment(reader, name_map, import_map);
                    break;
                case "MovieSceneEvalTemplatePtr":
                    struct_type = new InlineUStruct(reader, name_map, import_map);
                    break;
                case "MovieSceneTrackImplementationPtr":
                    struct_type = new InlineUStruct(reader, name_map, import_map);
                    break;
                case "MovieSceneSequenceInstanceDataPtr":
                    struct_type = new InlineUStruct(reader, name_map, import_map);
                    break;
                case "MovieSceneFrameRange":
                    struct_type = new FMovieSceneFrameRange(reader, name_map, import_map);
                    break;
                case "MovieSceneSegmentIdentifier":
                    struct_type = reader.ReadSingle();
                    break;
                case "MovieSceneSequenceID":
                    struct_type = reader.ReadSingle();
                    break;
                case "MovieSceneEvaluationKey":
                    struct_type = new FMovieSceneEvaluationKey(reader, name_map, import_map);
                    break;*/
                case "SmartName":
                    struct_type = new FSmartName(reader, name_map);
                    break;
                case "RichCurveKey":
                    struct_type = new FRichCurveKey(reader);
                    break;
                case "SimpleCurveKey":
                    struct_type = new FSimpleCurveKey(reader);
                    break;
                case "DateTime":
                    struct_type = new FDateTime(reader);
                    break;
                case "Timespan":
                    struct_type = new FDateTime(reader);
                    break;
                default:
                    struct_type = new FStructFallback(reader, name_map, import_map);
                    break;
            }
        }
    }
}
