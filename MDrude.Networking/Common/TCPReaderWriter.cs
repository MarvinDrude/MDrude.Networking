
namespace MDrude.Networking.Common;

public class TCPReaderWriter {

    public static async Task WriteDouble(Stream stream, double number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static async Task WriteFloat(Stream stream, float number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static async Task WriteUShort(Stream stream, ushort number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static async Task WriteShort(Stream stream, short number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static async Task WriteUInt(Stream stream, uint number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static Memory<byte> WriteInt(int number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        return buffer;

    }

    public static async Task WriteInt(Stream stream, int number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static async Task WriteULong(Stream stream, ulong number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static async Task WriteLong(Stream stream, long number, bool littleEndian = false) {

        Memory<byte> buffer = BitConverter.GetBytes(number);

        if (BitConverter.IsLittleEndian && !littleEndian) {
            buffer.Span.Reverse();
        }

        await stream.WriteAsync(buffer);

    }

    public static async Task<double> ReadDouble(Stream stream, bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 8);

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToDouble(buffer.Span);

    }

    public static async Task<float> ReadFloat(Stream stream, bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 4);

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToSingle(buffer.Span);

    }

    public static async Task<ushort> ReadUShort(Stream stream, bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 2);

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToUInt16(buffer.Span);

    }

    public static async Task<short> ReadShort(Stream stream, bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 2);

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToInt16(buffer.Span);

    }

    public static async Task<uint> ReadUInt(Stream stream, bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 4);

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToUInt32(buffer.Span);

    }

    public static int ReadInt(Memory<byte> buffer, bool littleEndian = false) {

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToInt32(buffer.Span);

    }

    public static async Task<int> ReadInt(Stream stream, bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 4);

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToInt32(buffer.Span);

    }

    public static async Task<ulong> ReadULong(Stream stream, bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 8);

        if (!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToUInt64(buffer.Span);

    }

    public static async Task<long> ReadLong(Stream stream , bool littleEndian = false) {

        Memory<byte> buffer = await Read(stream, 8);

        if(!littleEndian) {
            buffer.Span.Reverse();
        }

        return BitConverter.ToInt64(buffer.Span);

    }

    public static async Task<Memory<byte>> Read(Stream stream, ulong len) {

        Memory<byte> buffer = new byte[len];

        int read;
        int offset = 0;
        int length = (int)len;

        while(offset < (long)len) {

            read = await stream.ReadAsync(buffer[offset..]);
            length -= read;
            offset += read;

        }

        if(offset < (long)len) {
            return Memory<byte>.Empty;
        }

        return buffer;

    }

}
