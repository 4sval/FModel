using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public class FIoGlobalData
    {
        public readonly FNameEntrySerialized[] GlobalNameMap;
        public readonly List<ulong> GlobalNameHashes;
        public readonly Dictionary<FPackageObjectIndex, FScriptObjectDesc> ScriptObjectByGlobalId;

        public FIoGlobalData(FFileIoStoreReader globalReader, IReadOnlyCollection<FFileIoStoreReader> allReaders)
        {
            var globalNamesIoBuffer = globalReader.Read(new FIoChunkId(0, 0, EIoChunkType.LoaderGlobalNames));
            var globalNameHashesIoBuffer = globalReader.Read(new FIoChunkId(0, 0, EIoChunkType.LoaderGlobalNameHashes));
            var globalNameMap = new List<FNameEntrySerialized>();
            GlobalNameHashes = new List<ulong>();
            FNameEntrySerialized.LoadNameBatch(globalNameMap, GlobalNameHashes, globalNamesIoBuffer, globalNameHashesIoBuffer);
            GlobalNameMap = globalNameMap.ToArray();

            var initialLoadIoBuffer = globalReader.Read(new FIoChunkId(0, 0, EIoChunkType.LoaderInitialLoadMeta));
            var initialLoadIoReader = new BinaryReader(new MemoryStream(initialLoadIoBuffer, false));
            var numScriptObjects = initialLoadIoReader.ReadInt32();

            var scriptObjectByGlobalIdKeys = new FPackageObjectIndex[numScriptObjects];
            var scriptObjectByGlobalIdValues = new FScriptObjectDesc[numScriptObjects];
            var globalIndices = new Dictionary<FPackageObjectIndex, int>(numScriptObjects);

            for (var i = 0; i < numScriptObjects; i++)
            {
                var scriptObjectEntry = new FScriptObjectEntry(initialLoadIoReader);
                globalIndices.TryAdd(scriptObjectEntry.GlobalIndex, i);
                var mappedName = new FMappedName(scriptObjectEntry.ObjectName, GlobalNameMap, null);

                if (!mappedName.IsGlobal())
                {
                    Debug.WriteLine(i);
                }

                scriptObjectByGlobalIdKeys[i] = scriptObjectEntry.GlobalIndex;
                scriptObjectByGlobalIdValues[i] = new FScriptObjectDesc(GlobalNameMap[(int)mappedName.GetIndex()], mappedName, scriptObjectEntry);
            }

            for (var i = 0; i < numScriptObjects; i++)
            {
                var scriptObjectDesc = scriptObjectByGlobalIdValues[i];

                if (!scriptObjectDesc.FullName.IsNone)
                {
                    continue;
                }

                var scriptObjectStack = new Stack<FScriptObjectDesc>();
                var current = i;
                string fullName = string.Empty;

                while (current > 0)
                {
                    var currentDesc = scriptObjectByGlobalIdValues[current];

                    if (!currentDesc.FullName.IsNone)
                    {
                        fullName = currentDesc.FullName.String;
                        break;
                    }

                    scriptObjectStack.Push(currentDesc);
                    globalIndices.TryGetValue(currentDesc.OuterIndex, out current);
                }

                while (scriptObjectStack.Count != 0)
                {
                    var currentStack = scriptObjectStack.Pop();

                    if (fullName.Length == 0 || fullName.EndsWith('/'))
                    {
                        fullName = string.Concat(fullName, currentStack.Name.String);
                    }
                    else
                    {
                        fullName = string.Concat(fullName, "/", currentStack.Name.String);
                    }

                    currentStack.FullName = new FName(fullName);
                }
            }
            
            ScriptObjectByGlobalId = Enumerable.Range(0, numScriptObjects).ToDictionary(i => scriptObjectByGlobalIdKeys[i], i => scriptObjectByGlobalIdValues[i]);
            
            var packageByPackageIdMap = new Dictionary<FPackageId, FPackageStoreEntry>();
            foreach (var reader in allReaders)
            {
                var headerChunkId = new FIoChunkId(reader.ContainerId.Id, 0, EIoChunkType.ContainerHeader);
                if (reader.DoesChunkExist(headerChunkId) && !reader.IsEncrypted)
                {
                    var buffer = reader.Read(headerChunkId);
                    using var headerReader = new BinaryReader(new MemoryStream(buffer, false));
                
                    var containerHeader = new FContainerHeader(headerReader);
                    
                    using var storeEntryReader = new BinaryReader(new MemoryStream(containerHeader.StoreEntries));
                    
                    var containerPackages = new List<FPackageDesc>();
                    
                    for (var i = 0; i < containerHeader.PackageCount; i++)
                    {
                        var containerEntry = new FPackageStoreEntry(storeEntryReader);

                        var packageId = containerHeader.PackageIds[i];
                        if (!packageByPackageIdMap.TryGetValue(packageId, out var packageDesc))
                        {
                            /*
                            packageDesc = new FPackageDesc();
                            packageDesc.PackageId = packageId;
                            packageDesc.Size = containerEntry.ExportBundlesSize;
                            packageDesc.Exports = new FExportDesc[containerEntry.ExportCount];
                            packageDesc.ExportBundleCount = containerEntry.ExportBundleCount;
                            packageDesc.LoadOrder = containerEntry.LoadOrder;
                            */
                            packageByPackageIdMap[packageId] = containerEntry;
                        }
                        //containerPackages.Add(packageDesc);
                    }
                    
                    
                }
            }
        }
    }
}