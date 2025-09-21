
using System;
using System.IO;

namespace PhonieCore.OS.Audio.Wave
{

    public partial struct WavAudioFile
    {
        // http://soundfile.sapp.org/doc/WaveFormat/
        // http://soundfile.sapp.org/doc/WaveFormat/
        // http://soundfile.sapp.org/doc/WaveFormat/
        // http://soundfile.sapp.org/doc/WaveFormat/
        // http://soundfile.sapp.org/doc/WaveFormat/

        // to test:
        // ffmpeg -i input.mp3 -acodec pcm_s16le -ar 44100 -ac 2 output.wav
        // ffmpeg -i input.mp3 -acodec pcm_s16le -ar 44100 -ac 2 output.wav
        // ffmpeg -i input.mp3 -acodec pcm_s16le -ar 44100 -ac 2 output.wav

        // Contains the letters "RIFF" in ASCII form (0x52494646 big-endian form).
        public string ChunkID;

        // 36 + SubChunk2Size, or more precisely: 4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
        // Size of the rest of the chunk following this number. Size of the entire file minus 8 bytes for ChunkID and ChunkSize.
        public uint ChunkSize;

        // Contains the letters "WAVE" (0x57415645 big-endian form).
        public string Format;

        // Contains the letters "fmt " (0x666d7420 big-endian form).
        public string Subchunk1ID;

        // 16 for PCM. Size of the rest of the Subchunk which follows this number.
        public uint Subchunk1Size;

        // PCM = 1 (Linear quantization). Other values indicate compression.
        public ushort AudioFormat;
        public string AudioFormatName => FormatCode(AudioFormat);

        // Mono = 1, Stereo = 2, etc.
        public ushort NumChannels;

        // 8000, 44100, etc.
        public uint SampleRate;

        // SampleRate * NumChannels * BitsPerSample / 8
        public uint ByteRate;

        // NumChannels * BitsPerSample / 8
        // The number of bytes for one sample including all channels.
        public ushort BlockAlign;

        // 8 bits = 8, 16 bits = 16, etc.
        public ushort BitsPerSample;

        // Contains the letters "data" (0x64617461 big-endian form).
        public string Subchunk2ID;

        // NumSamples * NumChannels * BitsPerSample / 8
        // Number of bytes in the data.
        public uint Subchunk2Size;

        // The actual sound data.
        public byte[] Data;

        public float[] DataAsFloat => getDataAsFloat();

        private float[] _dataAsFloat;
        float[] getDataAsFloat()
        {
            if (_dataAsFloat != null)
                return _dataAsFloat;
            Assert(BitsPerSample == 8 || BitsPerSample == 16
                || BitsPerSample == 24 || BitsPerSample == 32,
                BitsPerSample + " bit depth is not supported.");

            _dataAsFloat = ConvertNBitByteArrayToFloatArray(BitsPerSample, Data, (int)Subchunk2Size);
            return _dataAsFloat;
        }

        public static WavAudioFile Parse(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
                return Parse(reader);
        }

        public static WavAudioFile Parse(byte[] data)
        {
            using (var mStream = new MemoryStream(data))
            using (var reader = new BinaryReader(mStream))
                return Parse(reader);
        }

        private static WavAudioFile Parse(BinaryReader reader)
        {
            WavAudioFile wav = new();
            // by default, binary reader is little endian..
            wav.ChunkID = new string(reader.ReadChars(4));
            wav.ChunkSize = reader.ReadUInt32();
            wav.Format = new string(reader.ReadChars(4));
            wav.Subchunk1ID = new string(reader.ReadChars(4));
            wav.Subchunk1Size = reader.ReadUInt32();
            Assert(wav.Subchunk1Size == 16, "Currently only PCM supported");
            wav.AudioFormat = reader.ReadUInt16();
            Assert(wav.AudioFormat == 1 || wav.AudioFormat == 65534,
                $"Detected format code '{wav.Format}' {wav.AudioFormatName}, but only PCM and WaveFormatExtensable uncompressed formats are currently supported.");
            wav.NumChannels = reader.ReadUInt16();
            wav.SampleRate = reader.ReadUInt32();
            wav.ByteRate = reader.ReadUInt32();
            wav.BlockAlign = reader.ReadUInt16();
            wav.BitsPerSample = reader.ReadUInt16();

            var extraChunk = new string(reader.ReadChars(4));
            var extraChunkSize = reader.ReadUInt32();
            while (extraChunk != "data")
            {
                reader.ReadBytes((int)extraChunkSize);
                extraChunk = new string(reader.ReadChars(4));
                extraChunkSize = reader.ReadUInt32();
            }
            wav.Subchunk2ID = extraChunk;
            wav.Subchunk2Size = extraChunkSize;
            wav.Data = reader.ReadBytes((int)wav.Subchunk2Size);

            return wav;
        }
        public override string ToString()
        {
            return $"WavFile:\n" +
                   $"  ChunkID: {ChunkID}\n" +
                   $"  ChunkSize: {ChunkSize} ({ChunkSize / 1048576f}mb)\n" +
                   $"  Format: {Format}\n" +
                   $"  Subchunk1ID: {Subchunk1ID}\n" +
                   $"  Subchunk1Size: {Subchunk1Size}\n" +
                   $"  AudioFormat: {AudioFormatName} ({AudioFormat})\n" +
                   $"  NumChannels: {NumChannels}\n" +
                   $"  SampleRate: {SampleRate}Hz\n" +
                   $"  ByteRate: {ByteRate}\n" +
                   $"  BlockAlign: {BlockAlign}\n" +
                   $"  BitsPerSample: {BitsPerSample}\n" +
                   $"  Subchunk2ID: {Subchunk2ID}\n" +
                   $"  Subchunk2Size: {Subchunk2Size}";
        }

    }

    // static utils
    public partial struct WavAudioFile
    {
        private static string FormatCode(UInt16 code)
        {
            switch (code)
            {
                case 1:
                    return "PCM";
                case 2:
                    return "ADPCM";
                case 3:
                    return "IEEE";
                case 7:
                    return "μ-law";
                case 65534:
                    return "WaveFormatExtensable";
                default:
                    return $"Uknown ({code})";
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }
        static float[] ConvertNBitByteArrayToFloatArray(int bitCount, byte[] data, int dataSize)
        {
            int samplesCount = dataSize / (bitCount / 8);
            float[] floatData = new float[samplesCount];

            // Max value based on bit depth
            float maxValue = (1 << (bitCount - 1)) - 1; // e.g., for 16-bit: 32767.0

            for (int i = 0; i < samplesCount; i++)
            {
                int byteOffset = i * (bitCount / 8);
                int sampleValue = 0;

                // Read sample value based on bit depth
                switch (bitCount)
                {
                    case 8:
                        sampleValue = data[byteOffset]; // 8-bit
                        floatData[i] = (sampleValue - 128) / 128.0f; // Normalize to [-1, 1]
                        break;
                    case 16:
                        sampleValue = BitConverter.ToInt16(data, byteOffset); // 16-bit
                        floatData[i] = sampleValue / maxValue;
                        break;
                    case 24:
                        // 24-bit needs to be handled as 32-bit
                        sampleValue = (data[byteOffset] | (data[byteOffset + 1] << 8) | (data[byteOffset + 2] << 16));
                        if (sampleValue > maxValue) sampleValue -= (1 << 24); // Convert to signed value
                        floatData[i] = sampleValue / maxValue;
                        break;
                    case 32:
                        sampleValue = BitConverter.ToInt32(data, byteOffset); // 32-bit
                        floatData[i] = sampleValue / (float)Int32.MaxValue; // Normalize
                        break;
                    default:
                        throw new NotSupportedException($"{bitCount} bit depth is not supported.");
                }
            }

            return floatData;
        }
    }
}