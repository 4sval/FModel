using FModel.Utils;
using System;
using System.IO;
using System.Linq;
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

                var latestUsmaps = new DirectoryInfo(Path.Combine(Properties.Settings.Default.OutputPath, "PakChunks")).GetFiles("*.usmap");
                if (Globals.Usmap == null && latestUsmaps.Length > 0)
                {
                    var latestUsmapInfo = latestUsmaps.OrderBy(f => f.LastWriteTime).Last();
                    byte[] mappingsData = await File.ReadAllBytesAsync(latestUsmapInfo.FullName);
                    FConsole.AppendText($"Mappings pulled from {latestUsmapInfo.Name}", FColors.Yellow, true);
                    Globals.Usmap = new Usmap(mappingsData);
                    return true;
                }
            }
            return false;
        }
    }
}
