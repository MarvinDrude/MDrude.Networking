
namespace MDrude.Networking.Common;

public class TCPFrame<ServerConnection>
    where ServerConnection : TCPServerConnection {

    public string ID { get; set; }

    public Memory<byte> Data { get; set; }

    public virtual Task<bool> Read(Stream stream) {

        throw new NotImplementedException("Not implemented");

    }

    public virtual async Task<bool> Read(ServerConnection conn) {

        return await Read(conn.Stream);

    }

    public virtual Task<bool> Write(Stream stream) {

        throw new NotImplementedException("Not implemented");

    }

    public virtual Task<bool> WriteFaulty(Stream stream) {

        throw new NotImplementedException("Not implemented");

    }

}

public class TCPFrameDefault : TCPFrame<TCPServerConnection> {

    public override async Task<bool> Read(Stream stream) {

        try {

            int lengthId = await TCPReaderWriter.ReadInt(stream);
            ulong lengthData = await TCPReaderWriter.ReadULong(stream);

            if(lengthData == 0) {
                throw new TCPSocketException("TCPFrameDefault.Read 0 length data");
            }

            Memory<byte> dataId = await TCPReaderWriter.Read(stream, (ulong)lengthId);
            string identifier = Encoding.UTF8.GetString(dataId.Span);

            Memory<byte> data = await TCPReaderWriter.Read(stream, lengthData);

            ID = identifier;
            Data = data;

            return true;

        } catch(Exception er) {

            Logger.DebugWrite("FAILED", $"TCPFrameDefault.Read error: {er.Message}");
            return false;

        }

    }

    public override async Task<bool> Write(Stream stream) {

        try {

            Memory<byte> dataId = Encoding.UTF8.GetBytes(ID);
            await TCPReaderWriter.WriteInt(stream, dataId.Length);
            await TCPReaderWriter.WriteULong(stream, (ulong)Data.Length);

            await stream.WriteAsync(dataId);
            await stream.WriteAsync(Data);

            return true;

        } catch (Exception er) {

            Logger.DebugWrite("FAILED", $"TCPFrameDefault.Write error: {er.Message}");
            return false;

        }

    }

    public override async Task<bool> WriteFaulty(Stream stream) {

        try {

            Memory<byte> dataId = Encoding.UTF8.GetBytes(ID);
            await TCPReaderWriter.WriteInt(stream, dataId.Length);
            await TCPReaderWriter.WriteULong(stream, (ulong)Data.Length + 500);

            await stream.WriteAsync(dataId);
            await stream.WriteAsync(Data);

            return true;

        } catch (Exception er) {

            Logger.DebugWrite("FAILED", $"TCPFrameDefault.Write error: {er.Message}");
            return false;

        }

    }

}
