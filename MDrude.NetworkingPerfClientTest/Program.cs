
using MDrude.Networking.Utils;
using MDrude.Networking.WebSockets;
using MDrude.NetworkingTest;

Logger.AddDefaultConsoleLogging();

string adr = "46.4.114.216";
//adr = "127.0.0.1";

while (true) {

    for (int e = 0; e < 10; e++) {

        WSClient client = new WSClient(adr, 54342, new WSClientOptions() {
            SslEnabled = false
        });

        client.On<TestDataMessage>("perf-message", async (mess) => {

            mess.Data = mess.Data.Reverse().ToString();
            mess.Number += 50;

            mess.Number = (int)Math.Sqrt(mess.Number);

            mess.Name = mess.Name.ToLower();

            await Task.Delay(2000);

            await client.Write("perf-message", mess);

        });

        client.Connect();

    }

    await Task.Delay(5000);

}

Console.ReadLine();