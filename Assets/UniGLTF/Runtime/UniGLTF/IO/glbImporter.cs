using System;
using System.Collections.Generic;
using System.Text;


namespace UniGLTF
{
    public static class glbImporter
    {
        public const string GLB_MAGIC = "glTF";
        public const uint GLB_VERSION = 2;
        
        public static readonly byte[] GLB_MAGIC_JSON = BitConverter.GetBytes((uint)GlbChunkType.JSON);
        public static readonly byte[] GLB_MAGIC_BIN = BitConverter.GetBytes((uint)GlbChunkType.BIN);

        public static GlbChunkType ToChunkType(this string src)
        {
            switch (src)
            {
                case "BIN":
                    return GlbChunkType.BIN;

                case "JSON":
                    return GlbChunkType.JSON;

                default:
                    throw new FormatException("unknown chunk type: " + src);
            }
        }

        public static string ToChunkTypeString(this GlbChunkType type)
        {
            switch (type)
            {
                case GlbChunkType.JSON:
                    return "JSON";
                case GlbChunkType.BIN:
                    return "BIN";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static GlbChunkType ToChunkType(this ReadOnlySpan<byte> src)
        {
            if (src.SequenceEqual(GLB_MAGIC_JSON))
            {
                return GlbChunkType.JSON;
            }
            if (src.SequenceEqual(GLB_MAGIC_BIN))
            {
                return GlbChunkType.BIN;
            }
            throw new FormatException("unknown chunk type");
        }

        [Obsolete("Use ParseGlbChunks(bytes)")]
        public static List<GlbChunk> ParseGlbChanks(Byte[] bytes)
        {
            return ParseGlbChunks(bytes, out _, out _);
        }

        public static List<GlbChunk> ParseGlbChunks(ReadOnlySpan<Byte> bytes, out GlbChunkRef jsonChunk, out GlbChunkRef binChunk)
        {
            //
            // glb header(12byte)
            //
            if (bytes.Length < 12)
            {
                throw new GlbParseException("glb header not found");
            }

            int pos = 0;
            if (Encoding.ASCII.GetString(bytes[..4]) != GLB_MAGIC)
            {
                throw new GlbParseException("invalid magic");
            }

            pos += 4;

            var version = BitConverter.ToUInt32(bytes[pos..]);
            if (version != GLB_VERSION)
            {
                throw new GlbParseException($"unknown version: {version}");
            }

            pos += 4;

            var totalLength = BitConverter.ToUInt32(bytes[pos..]);
            if (bytes.Length < totalLength)
            {
                throw new GlbParseException($"not enough size: {bytes.Length} < {totalLength}");
            }

            pos += 4;

            {
                var chunkDataSize = BitConverter.ToInt32(bytes[pos..]);
                pos += 4;

                var chunkTypeBytes = bytes.Slice(pos, 4);
                pos += 4;

                jsonChunk = new GlbChunkRef(chunkTypeBytes, bytes.Slice(pos, chunkDataSize));

                pos += chunkDataSize;
            }

            {
                var chunkDataSize = BitConverter.ToInt32(bytes[pos..]);
                pos += 4;

                var chunkTypeBytes = bytes.Slice(pos, 4);
                pos += 4;

                binChunk = new GlbChunkRef(chunkTypeBytes, bytes.Slice(pos, chunkDataSize));

                pos += chunkDataSize;
            }

            var chunks = new List<GlbChunk>();
            while (pos < bytes.Length)
            {
                var chunkDataSize = BitConverter.ToInt32(bytes[pos..]);
                pos += 4;

                //var type = (GlbChunkType)BitConverter.ToUInt32(bytes, pos);
                var chunkTypeBytes = GetChunkTypeBytes(bytes, pos);
                var chunkTypeStr = Encoding.ASCII.GetString(chunkTypeBytes);
                pos += 4;

                chunks.Add(new GlbChunk
                {
                    ChunkTypeString = chunkTypeStr,
                    Bytes = bytes.Slice(pos, chunkDataSize).ToArray()
                });

                pos += chunkDataSize;
            }

            return chunks;
        }

        private static ReadOnlySpan<byte> GetChunkTypeBytes(ReadOnlySpan<byte> bytes, int offset)
        {
            var slice = bytes.Slice(offset, 4);
            var result = new byte[4];
            var count = 0;
            foreach (var b in slice)
            {
                if (b != 0) result[count++] = b;
            }

            return new Span<byte>(result)[..count];
        }
    }
}