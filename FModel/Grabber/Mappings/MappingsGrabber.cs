using FModel.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using UsmapNET.Classes;

namespace FModel.Grabber.Mappings
{
    static class MappingsGrabber
    {
        public static async Task<bool> Load(bool forceReload = false)
        {
            if (Globals.Game.ActualGame == EGame.Fortnite)
            {
                Mapping[] benMappings = await MappingsData.GetData().ConfigureAwait(false);
                if (benMappings != null)
                {
                    foreach (Mapping mapping in benMappings)
                    {
                        if (mapping.Meta.CompressionMethod == "Brotli")
                        {
                            DirectoryInfo chunksDir = Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.OutputPath, "PakChunks"));
                            string mappingPath = Path.Combine(chunksDir.FullName, mapping.FileName);

                            byte[] mappingsData;
                            if (!forceReload && File.Exists(mappingPath))
                            {
                                mappingsData = await File.ReadAllBytesAsync(mappingPath);
                            }
                            else
                            {
                                mappingsData = await Endpoints.GetRawDataAsync(new Uri(mapping.Url)).ConfigureAwait(false);
                                await File.WriteAllBytesAsync(mappingPath, mappingsData).ConfigureAwait(false);
                            }

                            FConsole.AppendText($"Mappings pulled from {mapping.FileName}", FColors.Yellow, true);
                            Globals.Usmap = new Usmap(mappingsData);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
