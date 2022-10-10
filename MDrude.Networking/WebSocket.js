
var MD = MD || {};

(async () => {

    if (MD.EventEmitter) {
        return;
    }

    const EventEmitter = class EventEmitter {

        constructor() {

            this._events = {};

        }

        once(event, listener) {

            var func = async (ob) => {

                this.remove(event, func);
                await listener(ob);

            };

            this.on(event, func.bind(this));

        }

        on(event, listener) {

            var evt = null;

            if (!this._events.hasOwnProperty(event)) {
                this._events[event] = evt = [];
            } else {
                evt = this._events[event];
            }

            evt.push(listener);

        }

        async emit(event, ob) {

            if (!this._events.hasOwnProperty(event)) {
                return;
            }

            var arr = this._events[event];

            for (var e = 0; e < arr.length; e++) {

                await arr[e](ob);

            }

        }

        remove(event, listener) {

            if (!this._events.hasOwnProperty(event)) {
                return;
            }

            var arr = this._events[event];

            for (var e = arr.length - 1; e >= 0; e--) {

                if (arr[e] == listener) {
                    arr.splice(e, 1);
                }

            }

        }

    }

    MD.EventEmitter = EventEmitter;

})();

(async () => {

    if (MD.JsonWebSocket) {
        return;
    }

    const JsonWebSocket = class JsonWebSocket extends MD.EventEmitter {

        constructor() {

            super();

            var args = arguments[0];

            this.address = args.address;
            this.reconnect = typeof args.reconnect === "undefined" ? true : args.reconnect;
            this.interval = 2000;
            this.connected = false;
            this.shouldClose = false;

        }

        connect() {

            if (this.connected) {
                return;
            }

            this.connected = false;

            this.initSocket();

        }

        disconnect() {

            try {

                if (this._tid) {
                    clearTimeout(this._tid);
                }

                this.connected = false;
                this.shouldClose = true;

                this.socket.close();

            } catch (er) {
                console.log(er);
            }

        }

        send(uid, ob) {

            let idBytes = this.toUTF8Array(uid);
            let idLengthBytes = this.toBytesInt32(idBytes.length);
            idLengthBytes.reverse();

            let text = JSON.stringify(ob);
            let textBytes = this.toUTF8Array(text);

            let sending = new Uint8Array(idLengthBytes.length + idBytes.length + textBytes.length);

            sending.set(idLengthBytes, 0);
            sending.set(idBytes, idLengthBytes.length);
            sending.set(textBytes, idLengthBytes.length + idBytes.length);

            this.socket.send(sending);

        }

        sendBinary(uid, data) {

            let idBytes = this.toUTF8Array(uid);
            let idLengthBytes = this.toBytesInt32(idBytes.length);
            idLengthBytes.reverse();

            let sending = new Uint8Array(idLengthBytes.length + idBytes.length + data.length);

            sending.set(idLengthBytes, 0);
            sending.set(idBytes, idLengthBytes.length);
            sending.set(data, idLengthBytes.length + idBytes.length);

            this.socket.send(sending);

        }

        initSocket() {

            try {

                this.socket = new WebSocket(this.address);
                this.socket.binaryType = "arraybuffer";

                this.socket.onopen = () => {

                    this.connected = true;
                    this.emit("connect", {});

                };

                this.socket.onmessage = (message) => {

                    if (message.data instanceof ArrayBuffer) {

                        const view = new DataView(message.data.slice(0, 4));
                        const lenId = view.getInt32(0, false);

                        const uid = this.fromUTF8Array(message.data.slice(4, 4 + lenId));
                        const data = message.data.slice(4 + lenId);

                        if (uid.startsWith("binary-")) {

                            this.emit(uid, data);

                        } else {

                            const text = this.fromUTF8Array(data);

                            if (uid == "__inner-ping") {

                                const vi = new DataView(data);
                                this.rtt = vi.getFloat32(0);

                                this.emit("__inner-ping", this.rtt);
                                return this.send("__inner-pong", {});

                            }

                            const ob = JSON.parse(text);

                            if (location.href.indexOf("localhost") > -1) {
                                console.log("in", uid, ob);
                            }

                            this.emit(uid, ob);

                        }

                    }

                };

                this.socket.onclose = () => {

                    this.connected = false;
                    this.emit("disconnect", {});

                    if (this.reconnect && !this.shouldClose) {

                        this._tid = setTimeout(() => {

                            this.connect();

                        }, this.interval);

                    }

                };

                this.socket.onerror = () => { };

            } catch (e) {

            }

        }

        toBytesInt32(num) {

            let arr = new Int32Array(1);
            arr[0] = num;

            return new Uint8Array(arr.buffer);

        }

        fromUTF8Array(arr) {

            return new TextDecoder("utf-8").decode(arr);

        }

        toUTF8Array(str) {

            return new TextEncoder("utf-8").encode(str);

        }

    }

    MD.JsonWebSocket = JsonWebSocket;

})();

let test = new MD.JsonWebSocket({
    "address": "ws://127.0.0.1:27789"
});

test.on("example-message", async (ob) => {
    console.log(ob);
});

test.on("connect", async () => {

    let sending = new Uint8Array(32);

    test.send("example-message", { "payload-example": "x" });
    test.sendBinary("binary-example", sending);

});

test.connect();