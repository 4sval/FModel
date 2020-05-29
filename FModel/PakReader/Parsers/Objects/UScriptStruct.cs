using PakReader.Parsers.Class;

namespace PakReader.Parsers.Objects
{
    public readonly struct UScriptStruct
    {
        public readonly IUStruct Struct;

        // Binary serialization, tagged property serialization otherwise
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L2146
        internal UScriptStruct(PackageReader reader, FName structName) : this(reader, structName.String) { }
        internal UScriptStruct(PackageReader reader, string structName)
        {
            Struct = structName switch
            {
                "LevelSequenceObjectReferenceMap" => new FLevelSequenceObjectReferenceMap(reader),
                "GameplayTagContainer" => new FGameplayTagContainer(reader),
                "NavAgentSelector" => new FNavAgentSelectorCustomization(reader),
                "Quat" => new FQuat(reader),
                "Vector4" => new FVector4(reader),
                "Vector2D" => new FVector2D(reader),
                "Box2D" => new FBox2D(reader),
                "Box" => new FVector(reader),
                "Vector" => new FVector(reader),
                "Rotator" => new FRotator(reader),
                "IntPoint" => new FIntPoint(reader),
                "Guid" => new FGuid(reader),
                "SoftObjectPath" => new FSoftObjectPath(reader),
                "SoftClassPath" => new FSoftObjectPath(reader),
                "Color" => new FColor(reader),
                "LinearColor" => new FLinearColor(reader),
                "SimpleCurveKey" => new FSimpleCurveKey(reader),
                "RichCurveKey" => new FRichCurveKey(reader),
                "FrameNumber" => new FFrameNumber(reader),
                "SmartName" => new FSmartName(reader),
                "PerPlatformFloat" => new FPerPlatformFloat(reader),
                "PerPlatformInt" => new FPerPlatformInt(reader),
                "DateTime" => new FDateTime(reader),
                "Timespan" => new FDateTime(reader),
                "MovieSceneTrackIdentifier" => new FFrameNumber(reader),
                "MovieSceneSegmentIdentifier" => new FFrameNumber(reader),
                "MovieSceneSequenceID" => new FFrameNumber(reader),
                "MovieSceneSegment" => new FMovieSceneSegment(reader),
                "SectionEvaluationDataTree" => new FSectionEvaluationDataTree(reader),
                "MovieSceneFrameRange" => new FMovieSceneFrameRange(reader),
                "MovieSceneEvaluationKey" => new FMovieSceneEvaluationKey(reader),
                "MovieSceneFloatValue" => new FRichCurveKey(reader),
                "MovieSceneEvaluationTemplate" => new FMovieSceneEvaluationTemplate(reader),
                "SkeletalMeshSamplingLODBuiltData" => new FSkeletalMeshSamplingLODBuiltData(reader),
                //"BodyInstance" => new FBodyInstance(reader), // if uncommented, can't parse .umap
                "VectorMaterialInput" => new FVectorMaterialInput(reader),
                "ColorMaterialInput" => new FColorMaterialInput(reader),
                "ExpressionInput" => new FMaterialInput(reader),
                //"RawCurveTracks" => new FRawCurveTracks(reader),
                _ => new UObject(reader, true),
            };
        }
    }
}
