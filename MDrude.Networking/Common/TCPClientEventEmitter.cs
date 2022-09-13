
namespace MDrude.Networking.Common;

public class TCPClientEventEmitter {

    public string UID { get; set; }

    public List<TCPEventEmitterEntry> Listeners { get; private set; }

    public TCPClientEventEmitter(string uid) {

        UID = uid;
        Listeners = new List<TCPEventEmitterEntry>();

    }

    public void AddListener<T>(Func<T, Task> listener) {

        Listeners.Add(new TCPEventEmitterEntry() {
            Type = typeof(T),
            Function = listener
        });

    }

    public bool RemoveListener<T>(Func<T, Task> listener) {

        bool found = false;

        for (int e = Listeners.Count - 1; e >= 0; e--) {

            var curr = Listeners[e];

            if (curr.Function == listener) {
                found = true;
                Listeners.RemoveAt(e);
            }

        }

        return found;

    }

}

public class TCPEventEmitterEntry {

    public Type Type { get; set; }

    public dynamic Function { get; set; }

}
