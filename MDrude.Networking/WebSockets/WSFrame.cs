
using System.Reflection.Emit;
using System.Text;

namespace MDrude.Networking.WebSockets;

public class WSFrame : TCPFrame<WSServerConnection> {

    public WSOpcode Opcode { get; set; } = WSOpcode.BinaryFrame;

    public override async Task<bool> Read(Stream stream) {

        try {

            bool bitFinSet = false;
            bool res = false;
            bool firstFragment = true;

            using MemoryStream ms = new MemoryStream();

            while (!bitFinSet) { // fragmented messages can be interrupted by ping pong close frames (not handled rn, clients normally wont do it) if that would happen then client just gets disconnected

                Memory<byte> firstData = new byte[1];
                await stream.ReadAsync(firstData);

                byte first = firstData.Span[0];

                byte bitFinFlag = 0x80;
                byte opcodeFlag = 0x0F;

                bitFinSet = (first & bitFinFlag) == bitFinFlag;
                WSOpcode opcode = (WSOpcode)(first & opcodeFlag);

                byte bitMaskFlag = 0x80;

                Memory<byte> secData = new byte[1];
                await stream.ReadAsync(secData);

                byte second = secData.Span[0];

                bool bitMaskSet = (second & bitMaskFlag) == bitMaskFlag;
                ulong length = await ReadLength(stream, second);

                if (length != 0) {

                    Memory<byte> decoded = new byte[length];

                    if (bitMaskSet) {

                        byte[] key = (await TCPReaderWriter.Read(stream, 4)).ToArray();
                        byte[] encoded = (await TCPReaderWriter.Read(stream, length)).ToArray();

                        for (int i = 0; i < encoded.Length; i++) {

                            decoded.Span[i] = (byte)(encoded[i] ^ key[i % 4]);

                        }

                    } else {

                        (await TCPReaderWriter.Read(stream, length)).CopyTo(decoded);

                    }

                    if (firstFragment) {
                        Opcode = opcode;
                        res = true;
                    }

                    await ms.WriteAsync(decoded);
                    firstFragment = false;

                } else {

                    return false;

                }

            }

            if(Opcode == WSOpcode.TextFrame || Opcode == WSOpcode.PingFrame
                || Opcode == WSOpcode.PongFrame || Opcode == WSOpcode.ConnectionCloseFrame) {

                ID = Opcode == WSOpcode.TextFrame ? "__text-frame" : null;
                Data = ms.ToArray();

                return true;

            }

            Memory<byte> dec = ms.ToArray();
            int idLength = TCPReaderWriter.ReadInt(dec.Slice(0, 4));
            Memory<byte> dataId = dec.Slice(4, idLength);
            string identifier = Encoding.UTF8.GetString(dataId.Span);

            ID = identifier;
            Data = dec.Slice(4 + idLength);

            return res;

        } catch (Exception er) {

            Logger.DebugWrite("INFO", $"WSFrame.Read error: {er.Message}");
            return false;

        }

    }

    public async Task<ulong> ReadLength(Stream stream, byte second) {

        byte dataLengthFlag = 0x7F;
        uint length = (uint)(second & dataLengthFlag);
        ulong res = 0;

        if (length == 126) {

            res = await TCPReaderWriter.ReadUShort(stream, false);

        } else if (length == 127) {

            res = await TCPReaderWriter.ReadULong(stream, false);

            if (length < 0) {

                return 0;

            }

        } else {

            res = length;

        }

        return res;

    }

    public override async Task<bool> Write(Stream stream) {

        try {

            using MemoryStream ms = new MemoryStream();

            byte bitFin = 0x80;
            byte first = (byte)(bitFin | (byte)Opcode);

            Memory<byte> firstData = new byte[] { first };
            await ms.WriteAsync(firstData);

            Memory<byte> idData = Encoding.UTF8.GetBytes(ID);
            Memory<byte> idLength = TCPReaderWriter.WriteInt(idData.Length);

            Memory<byte> dataBefore = Data;
            Data = new byte[dataBefore.Length + idData.Length + idLength.Length];

            idLength.CopyTo(Data[..]);
            idData.CopyTo(Data.Slice(4, idData.Length));
            dataBefore.CopyTo(Data[(4 + idData.Length)..]);

            if (Data.Length <= 125) {

                Memory<byte> secData = new byte[] { (byte)Data.Length };
                await ms.WriteAsync(secData);

            } else if (Data.Length <= 65535) {

                Memory<byte> secData = new byte[] { 126 };
                await ms.WriteAsync(secData);

                await TCPReaderWriter.WriteUShort(ms, (ushort)Data.Length, false);

            } else {

                Memory<byte> secData = new byte[] { 127 };
                await ms.WriteAsync(secData);

                await TCPReaderWriter.WriteULong(ms, (ulong)Data.Length, false);

            }

            await ms.WriteAsync(Data);
            ms.Position = 0;

            await stream.WriteAsync(ms.ToArray());

            return true;

        } catch (Exception er) {

            Logger.DebugWrite("INFO", $"WSFrame.Write error: {er.Message}");
            return false;

        }

    }

}
