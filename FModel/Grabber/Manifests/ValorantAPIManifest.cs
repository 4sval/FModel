using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FModel.Utils;

using PakReader;

namespace FModel.Grabber.Manifests
{
    public class ValorantAPIManifest
    {
        private const string _url = "https://fmodel.fortnite-api.com/valorant/v1/manifest";

        private readonly HttpClient _client;
        private readonly DirectoryInfo _chunkDirectory;

        public readonly ulong Id;
        public readonly Dictionary<ulong, ValorantChunk> Chunks;
        public readonly ValorantPak[] Paks;

        public ValorantAPIManifest(byte[] data, DirectoryInfo directoryInfo) : this(new MemoryStream(data, false), directoryInfo) { }
        public ValorantAPIManifest(Stream stream, DirectoryInfo directoryInfo) : this(new BinaryReader(stream), directoryInfo) { }
        public ValorantAPIManifest(BinaryReader reader, DirectoryInfo directoryInfo)
        {
            Id = reader.ReadUInt64();
            var chunks = reader.ReadInt32();
            Chunks = new Dictionary<ulong, ValorantChunk>(chunks);

            for (var i = 0; i < chunks; i++)
            {
                var chunk = new ValorantChunk(reader);
                Chunks.Add(chunk.Id, chunk);
            }

            Paks = reader.ReadTArray(() => new ValorantPak(reader));

            _client = new HttpClient(new HttpClientHandler
            {
                UseProxy = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                CheckCertificateRevocationList = false,
                PreAuthenticate = false,
                MaxConnectionsPerServer = 1337
            });
            _chunkDirectory = directoryInfo;
        }

        public Stream GetPakStream(int index)
        {
            return new ValorantPakStream(this, index);
        }

        public async Task PrefetchChunk(ValorantChunk chunk, CancellationToken cancellationToken)
        {
            var chunkPath = Path.Combine(_chunkDirectory.FullName, chunk.Id + ".valchunk");

            if (File.Exists(chunkPath))
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, chunk.Url);
            using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var chunkBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                await using var fs = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                await fs.WriteAsync(chunkBytes, 0, chunkBytes.Length, cancellationToken).ConfigureAwait(false);
            }
            #if DEBUG
            else
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debugger.Break();
            }
            #endif
        }

        public async Task<byte[]> GetChunkBytes(ValorantChunk chunk, CancellationToken cancellationToken)
        {
            var chunkPath = Path.Combine(_chunkDirectory.FullName, chunk.Id + ".valchunk");
            byte[] chunkBytes;

            if (File.Exists(chunkPath))
            {
                chunkBytes = new byte[chunk.Size];
                await using var fs = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fs.ReadAsync(chunkBytes, 0, chunkBytes.Length, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, chunk.Url);
                using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    chunkBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    await using var fs = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await fs.WriteAsync(chunkBytes, 0, chunkBytes.Length, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    #if DEBUG
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Debugger.Break();
                    #endif
                    chunkBytes = null;
                }
            }

            return chunkBytes;
        }

        public static async Task<ValorantAPIManifest> DownloadAndParse(DirectoryInfo directoryInfo)
        {
            using var client = new HttpClient(new HttpClientHandler
            {
                UseProxy = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                CheckCertificateRevocationList = false,
                PreAuthenticate = false
            });
            using var request = new HttpRequestMessage(HttpMethod.Get, _url);
            using var response = await client.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var responseStream = await response.Content.ReadAsStreamAsync();
            return new ValorantAPIManifest(responseStream, directoryInfo);
        }
    }

    public readonly struct ValorantChunk
    {
        private const string _baseUrl = "https://fmodel.fortnite-api.com/valorant/v1/chunks/";

        public readonly ulong Id;
        public readonly uint Size;
        public string Url => _baseUrl + Id;

        public ValorantChunk(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Size = reader.ReadUInt32();
        }

        public override string ToString()
        {
            return $"{Id:X8} | {Strings.GetReadableSize(Size)}";
        }
    }

    public readonly struct ValorantPak
    {
        public readonly ulong Id;
        public readonly uint Size;
        public readonly string Name;
        public readonly ulong[] ChunkIds;

        public ValorantPak(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Size = reader.ReadUInt32();
            var nameLength = reader.ReadInt32();
            var nameBytes = reader.ReadBytes(nameLength);
            Name = Encoding.ASCII.GetString(nameBytes);
            ChunkIds = new ulong[reader.ReadInt32()];

            for (var i = 0; i < ChunkIds.Length; i++)
            {
                ChunkIds[i] = reader.ReadUInt64();
            }
        }

        public override string ToString()
        {
            return $"{Name} | {Strings.GetReadableSize(Size)}";
        }
    }

    public class ValorantPakStream : Stream
    {
        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = true;
        public override bool CanWrite { get; } = false;
        public override long Length { get; }

        private long _position;

        public override long Position
        {
            get => _position;
            set
            {
                if (value >= Length || value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _position = value;
            }
        }

        public string FileName { get; }
        private readonly ValorantAPIManifest _manifest;
        private readonly ValorantChunk[] _chunks;

        public ValorantPakStream(ValorantAPIManifest manifest, int pakIndex)
        {
            _manifest = manifest;
            var pak = manifest.Paks[pakIndex];
            FileName = pak.Name;
            Length = pak.Size;
            _chunks = new ValorantChunk[pak.ChunkIds.Length];

            for (var i = 0; i < _chunks.Length; i++)
            {
                _chunks[i] = manifest.Chunks[pak.ChunkIds[i]];
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task PrefetchAsync(int i, uint startPos, long count, CancellationToken cancellationToken, int concurrentDownloads = 4)
        {
            var tasks = new List<Task>();
            var sem = new SemaphoreSlim(concurrentDownloads);

            while (count > 0)
            {
                await sem.WaitAsync(cancellationToken).ConfigureAwait(false);
                var chunk = _chunks[i++];
                tasks.Add(PrefetchChunkAsync(chunk));

                if (i == _chunks.Length)
                {
                    break;
                }

                count -= chunk.Size - startPos;
                startPos = 0u;
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            sem.Dispose();

            async Task PrefetchChunkAsync(ValorantChunk chunk)
            {
                await _manifest.PrefetchChunk(chunk, cancellationToken).ConfigureAwait(false);
                sem.Release();
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var (i, startPos) = GetChunkIndex(_position);

            if (i == -1)
            {
                return 0;
            }

            await PrefetchAsync(i, startPos, count, cancellationToken).ConfigureAwait(false);
            var bytesRead = 0;

            while (true)
            {
                var chunk = _chunks[i];
                var chunkData = await _manifest.GetChunkBytes(chunk, cancellationToken).ConfigureAwait(false);

                var chunkBytes = chunk.Size - startPos;
                var bytesLeft = count - bytesRead;

                if (bytesLeft <= chunkBytes)
                {
                    Unsafe.CopyBlockUnaligned(ref buffer[bytesRead + offset], ref chunkData[startPos], (uint)bytesLeft);
                    bytesRead += bytesLeft;
                    break;
                }

                Unsafe.CopyBlockUnaligned(ref buffer[bytesRead + offset], ref chunkData[startPos], chunkBytes);
                bytesRead += (int)chunkBytes;
                startPos = 0u;

                if (++i == _chunks.Length)
                {
                    break;
                }
            }

            _position += bytesRead;
            return bytesRead;
        }

        private (int Index, uint ChunkPos) GetChunkIndex(long position)
        {
            for (var i = 0; i < _chunks.Length; i++)
            {
                var size = _chunks[i].Size;

                if (position < size)
                {
                    return (i, (uint)position);
                }

                position -= size;
            }

            return (-1, 0u);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => offset + _position,
                SeekOrigin.End => Length + offset,
                _ => throw new ArgumentOutOfRangeException()
            };
            return _position;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}