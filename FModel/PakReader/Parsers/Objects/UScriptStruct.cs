using System.Runtime.CompilerServices;
using FModel.PakReader.Parsers.Class;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct UScriptStruct
    {
        public readonly IUStruct Struct;
        
        // Binary serialization, tagged property serialization otherwise
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L2146
        internal UScriptStruct(PackageReader reader, FName structName) : this(reader, structName.String) { }
        internal UScriptStruct(PackageReader reader, string structName)
        {
#if DEBUG
            //System.Diagnostics.Debug.WriteLine(structName);
#endif
            Struct = structName switch
            {
                "LevelSequenceObjectReferenceMap" => new FLevelSequenceObjectReferenceMap(reader),
                "GameplayTagContainer" => new FGameplayTagContainer(reader),
                //"GameplayTag" => new FGameplayTagContainer(reader),
                "NavAgentSelector" => new FNavAgentSelectorCustomization(reader),
                "Quat" => new FQuat(reader),
                "Vector4" => new FVector4(reader),
                "Vector2D" => new FVector2D(reader),
                "Box2D" => new FBox2D(reader),
                "Box" => new FBox(reader),
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
                "MovieSceneFloatChannel" => new FMovieSceneFloatChannel(reader),
                "MovieSceneEvaluationTemplate" => new FMovieSceneEvaluationTemplate(reader),
                //"SkeletalMeshSamplingLODBuiltData" => new FSkeletalMeshSamplingLODBuiltData(reader),
                "VectorMaterialInput" => new FVectorMaterialInput(reader),
                "ColorMaterialInput" => new FColorMaterialInput(reader),
                "ExpressionInput" => new FMaterialInput(reader),
                //
                //"PrimaryAssetType" => new FPrimaryAssetType(reader),
                //"PrimaryAssetId" => new FPrimaryAssetId(reader),
                _ => Fallback(reader, structName)
            };
        }
        
        internal UScriptStruct(string structName)
        {
            Struct = structName switch
            {
                "LevelSequenceObjectReferenceMap" => new FLevelSequenceObjectReferenceMap(),
                "GameplayTagContainer" => new FGameplayTagContainer(),
                //"GameplayTag" => new FGameplayTagContainer(reader),
                "NavAgentSelector" => new FNavAgentSelectorCustomization(),
                "Quat" => new FQuat(),
                "Vector4" => new FVector4(),
                "Vector2D" => new FVector2D(),
                "Box2D" => new FBox2D(),
                "Box" => new FBox(),
                "Vector" => new FVector(),
                "Rotator" => new FRotator(),
                "IntPoint" => new FIntPoint(),
                "Guid" => new FGuid(),
                "SoftObjectPath" => new FSoftObjectPath(),
                "SoftClassPath" => new FSoftObjectPath(),
                "Color" => new FColor(),
                "LinearColor" => new FLinearColor(),
                "SimpleCurveKey" => new FSimpleCurveKey(),
                "RichCurveKey" => new FRichCurveKey(),
                "FrameNumber" => new FFrameNumber(),
                "SmartName" => new FSmartName(),
                "PerPlatformFloat" => new FPerPlatformFloat(),
                "PerPlatformInt" => new FPerPlatformInt(),
                "DateTime" => new FDateTime(),
                "Timespan" => new FDateTime(),
                "MovieSceneTrackIdentifier" => new FFrameNumber(),
                "MovieSceneSegmentIdentifier" => new FFrameNumber(),
                "MovieSceneSequenceID" => new FFrameNumber(),
                "MovieSceneSegment" => new FMovieSceneSegment(),
                "SectionEvaluationDataTree" => new FSectionEvaluationDataTree(),
                "MovieSceneFrameRange" => new FMovieSceneFrameRange(),
                "MovieSceneEvaluationKey" => new FMovieSceneEvaluationKey(),
                "MovieSceneFloatValue" => new FRichCurveKey(),
                "MovieSceneFloatChannel" => new FMovieSceneFloatChannel(),
                "MovieSceneEvaluationTemplate" => new FMovieSceneEvaluationTemplate(),
                //"SkeletalMeshSamplingLODBuiltData" => new FSkeletalMeshSamplingLODBuiltData(reader),
                "VectorMaterialInput" => new FVectorMaterialInput(),
                "ColorMaterialInput" => new FColorMaterialInput(),
                "ExpressionInput" => new FMaterialInput(),
                //
                "PrimaryAssetType" => new FPrimaryAssetType(),
                "PrimaryAssetId" => new FPrimaryAssetId(),
                _ => new UObject()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IUStruct Fallback(PackageReader reader, string structName)
        {
            if (reader is IoPackageReader ioReader)
            {
                return new UObject(ioReader, structName, true);
            }

            return new UObject(reader, true);
        }
    }
}
