
namespace MDrude.Networking.Common;

public class TCPServerEventEmitter<ServerConnection>
    where ServerConnection : TCPServerConnection, new() {

    public string UID { get; set; }

    public List<TCPServerEventEmitterEntry> Listeners { get; private set; }

    public TCPServerEventEmitter(string uid) {

        UID = uid;
        Listeners = new List<TCPServerEventEmitterEntry>();

    }

    public void AddListener<T>(Func<T, ServerConnection, Task> listener) {

        Listeners.Add(new TCPServerEventEmitterEntry() {
            Type = typeof(T),
            Function = listener
        });

    }

    public bool RemoveListener<T>(Func<T, ServerConnection, Task> listener) {

        bool found = false;

        for(int e = Listeners.Count - 1; e >= 0; e--) {

            var curr = Listeners[e];

            if(curr.Function == listener) {
                found = true;
                Listeners.RemoveAt(e);
            }

        }

        return found;

    }

}

public class TCPServerEventEmitterEntry {

    public Type Type { get; set; }

    public dynamic Function { get; set; }

}
