

using Newtonsoft.Json;

namespace MDrude.Networking.Common;

public abstract class TCPSerializer {

    public abstract Memory<byte> Serialize<T>(T ob);

    public abstract T Deserialize<T>(Memory<byte> buffer);

    public abstract object Deserialize(Memory<byte> buffer, Type type);

}

public class TCPJsonSerializer : TCPSerializer {

    public override T Deserialize<T>(Memory<byte> buffer) {

        string text = Encoding.UTF8.GetString(buffer.Span);

        try {

            return JsonConvert.DeserializeObject<T>(text);

        } catch(Exception) {

            return default;

        }

    }

    public override object Deserialize(Memory<byte> buffer, Type type) {

        string text = Encoding.UTF8.GetString(buffer.Span);

        try {

            return JsonConvert.DeserializeObject(text, type);

        } catch (Exception) {

            return default;

        }

    }

    public override Memory<byte> Serialize<T>(T ob) {

        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ob));

    }

}