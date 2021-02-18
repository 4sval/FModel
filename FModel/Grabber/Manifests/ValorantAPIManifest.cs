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
using FModel.PakReader;
using FModel.Utils;

using Ionic.Zlib;

namespace FModel.Grabber.Manifests
{
    public class ValorantAPIManifestV1
    {
        private const string _url = "https://fmodel.fortnite-api.com/valorant/v1/manifest";

        private readonly HttpClient _client;
        private readonly DirectoryInfo _chunkDirectory;

        public readonly ulong Id;
        public readonly Dictionary<ulong, ValorantChunkV1> Chunks;
        public readonly ValorantPakV1[] Paks;

        public ValorantAPIManifestV1(byte[] data, DirectoryInfo directoryInfo) : this(new MemoryStream(data, false), directoryInfo) { }
        public ValorantAPIManifestV1(Stream stream, DirectoryInfo directoryInfo) : this(new BinaryReader(stream), directoryInfo) { }
        public ValorantAPIManifestV1(BinaryReader reader, DirectoryInfo directoryInfo)
        {
            using (reader)
            {
                Id = reader.ReadUInt64();
                var chunks = reader.ReadInt32();
                Chunks = new Dictionary<ulong, ValorantChunkV1>(chunks);

                for (var i = 0; i < chunks; i++)
                {
                    var chunk = new ValorantChunkV1(reader);
                    Chunks.Add(chunk.Id, chunk);
                }

                Paks = reader.ReadTArray(() => new ValorantPakV1(reader));
            }

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
            return new ValorantPakV1Stream(this, index);
        }

        public async Task PrefetchChunk(ValorantChunkV1 chunk, CancellationToken cancellationToken)
        {
            var chunkPath = Path.Combine(_chunkDirectory.FullName, $"{chunk.Id}.valchunk");

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

        public async Task<byte[]> GetChunkBytes(ValorantChunkV1 chunk, CancellationToken cancellationToken)
        {
            var chunkPath = Path.Combine(_chunkDirectory.FullName, $"{chunk.Id}.valchunk");
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

        public static async Task<ValorantAPIManifestV1> DownloadAndParse(DirectoryInfo directoryInfo)
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

            try
            {
                using var response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                var responseStream = await response.Content.ReadAsStreamAsync();
                return new ValorantAPIManifestV1(responseStream, directoryInfo);
            }
            catch
            {
                return null;
            }
        }
    }

    public class ValorantAPIManifestV2
    {
        private const string _url = "https://fmodel.fortnite-api.com/valorant/v2/manifest";

        private readonly HttpClient _client;
        private readonly DirectoryInfo _chunkDirectory;

        public readonly ValorantAPIManifestHeaderV2 Header;
        public readonly ValorantChunkV2[] Chunks;
        public readonly ValorantPakV2[] Paks;

        public ValorantAPIManifestV2(byte[] data, DirectoryInfo directoryInfo) : this(new MemoryStream(data, false), directoryInfo) { }
        public ValorantAPIManifestV2(Stream stream, DirectoryInfo directoryInfo) : this(new BinaryReader(stream), directoryInfo) { }
        public ValorantAPIManifestV2(BinaryReader reader, DirectoryInfo directoryInfo)
        {
            using (reader)
            {
                Header = new ValorantAPIManifestHeaderV2(reader);

                var compressedBuffer = reader.ReadBytes((int)Header.CompressedSize);
                var uncompressedBuffer = ZlibStream.UncompressBuffer(compressedBuffer);

                if (uncompressedBuffer.Length != Header.UncompressedSize)
                {
                    throw new FileLoadException("invalid decompressed manifest body");
                }

                using var bodyMs = new MemoryStream(uncompressedBuffer, false);
                using var bodyReader = new BinaryReader(bodyMs);

                Chunks = new ValorantChunkV2[Header.ChunkCount];

                for (var i = 0u; i < Header.ChunkCount; i++)
                {
                    Chunks[i] = new ValorantChunkV2(bodyReader);
                }

                Paks = new ValorantPakV2[Header.PakCount];

                for (var i = 0u; i < Header.PakCount; i++)
                {
                    Paks[i] = new ValorantPakV2(bodyReader);
                }
            }

            _client = new HttpClient(new HttpClientHandler
            {
                UseProxy = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                CheckCertificateRevocationList = false,
                PreAuthenticate = false,
                MaxConnectionsPerServer = 1337,
                UseDefaultCredentials = false,
                AllowAutoRedirect = false
            });
            _chunkDirectory = directoryInfo;
        }

        public Stream GetPakStream(int index)
        {
            return new ValorantPakV2Stream(this, index);
        }

        public async Task PrefetchChunk(ValorantChunkV2 chunk, CancellationToken cancellationToken)
        {
            var chunkPath = Path.Combine(_chunkDirectory.FullName, $"{chunk.Id}.valchunk");

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

        public async Task<byte[]> GetChunkBytes(ValorantChunkV2 chunk, CancellationToken cancellationToken)
        {
            var chunkPath = Path.Combine(_chunkDirectory.FullName, $"{chunk.Id}.valchunk");
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

        public static async Task<ValorantAPIManifestV2> DownloadAndParse(DirectoryInfo directoryInfo)
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

            try
            {
                using var response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                var responseStream = await response.Content.ReadAsStreamAsync();
                return new ValorantAPIManifestV2(responseStream, directoryInfo);
            }
            catch
            {
                return null;
            }
        }
    }

    public readonly struct ValorantAPIManifestHeaderV2
    {
        public const uint MAGIC = 0xC3D088F7u;

        public readonly uint Magic;
        public readonly uint HeaderSize;
        public readonly ulong ManifestId;
        public readonly uint UncompressedSize;
        public readonly uint CompressedSize;
        public readonly uint ChunkCount;
        public readonly uint PakCount;
        public readonly string GameVersion;

        public ValorantAPIManifestHeaderV2(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();

            if (Magic != MAGIC)
            {
                throw new FileLoadException("invalid manifest magic");
            }

            HeaderSize = reader.ReadUInt32();
            ManifestId = reader.ReadUInt64();
            UncompressedSize = reader.ReadUInt32();
            CompressedSize = reader.ReadUInt32();
            ChunkCount = reader.ReadUInt32();
            PakCount = reader.ReadUInt32();

            var gameVersionLength = (int)reader.ReadByte();
            if (gameVersionLength == 0)
            {
                GameVersion = null;
            }
            else
            {
                var gameVersionBuffer = reader.ReadBytes(gameVersionLength);
                GameVersion = Encoding.ASCII.GetString(gameVersionBuffer);
            }

            reader.BaseStream.Position = HeaderSize;
        }
    }

    public readonly struct ValorantChunkV1
    {
        private const string _baseUrl = "https://fmodel.fortnite-api.com/valorant/v1/chunks/";

        public readonly ulong Id;
        public readonly uint Size;
        public string Url => _baseUrl + Id;

        public ValorantChunkV1(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Size = reader.ReadUInt32();
        }

        public override string ToString()
        {
            return $"{Id:X8} | {Strings.GetReadableSize(Size)}";
        }
    }

    public readonly struct ValorantChunkV2
    {
        private const string _baseUrl = "https://fmodel.fortnite-api.com/valorant/v2/chunks/";

        public readonly ulong Id;
        public readonly uint Size;
        public string Url => $"{_baseUrl}{Id}";

        public ValorantChunkV2(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Size = reader.ReadUInt32();
        }

        public override string ToString()
        {
            return $"{Id:X8} | {Strings.GetReadableSize(Size)}";
        }
    }

    public readonly struct ValorantPakV1
    {
        public readonly ulong Id;
        public readonly uint Size;
        public readonly string Name;
        public readonly ulong[] ChunkIds;

        public ValorantPakV1(BinaryReader reader)
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

    public readonly struct ValorantPakV2
    {
        public readonly ulong Id;
        public readonly uint Size;
        public readonly uint[] ChunkIndices;
        public readonly string Name;

        public ValorantPakV2(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Size = reader.ReadUInt32();

            var chunkIndicesLength = reader.ReadUInt32();
            ChunkIndices = new uint[chunkIndicesLength];
            for (uint i = 0; i < chunkIndicesLength; i++)
            {
                ChunkIndices[i] = reader.ReadUInt32();
            }

            var nameLength = (int)reader.ReadByte();
            var nameBytes = reader.ReadBytes(nameLength);
            Name = Encoding.ASCII.GetString(nameBytes);
        }

        public override string ToString()
        {
            return $"{Name} | {Strings.GetReadableSize(Size)}";
        }
    }

    public class ValorantPakV1Stream : Stream
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
        private readonly ValorantAPIManifestV1 _manifest;
        private readonly ValorantChunkV1[] _chunks;

        public ValorantPakV1Stream(ValorantAPIManifestV1 manifest, int pakIndex)
        {
            _manifest = manifest;
            var pak = manifest.Paks[pakIndex];
            FileName = pak.Name;
            Length = pak.Size;
            _chunks = new ValorantChunkV1[pak.ChunkIds.Length];

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

            async Task PrefetchChunkAsync(ValorantChunkV1 chunk)
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

    public class ValorantPakV2Stream : Stream
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
        private readonly ValorantAPIManifestV2 _manifest;
        private readonly ValorantChunkV2[] _chunks;

        public ValorantPakV2Stream(ValorantAPIManifestV2 manifest, int pakIndex)
        {
            _manifest = manifest;
            var pak = manifest.Paks[pakIndex];
            FileName = pak.Name;
            Length = pak.Size;

            _chunks = new ValorantChunkV2[pak.ChunkIndices.Length];
            for (var i = 0; i < pak.ChunkIndices.Length; i++)
            {
                _chunks[i] = manifest.Chunks[pak.ChunkIndices[i]];
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

            async Task PrefetchChunkAsync(ValorantChunkV2 chunk)
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