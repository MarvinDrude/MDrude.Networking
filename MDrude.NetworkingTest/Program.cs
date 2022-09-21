
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using MDrude.NetworkingTest;
using MDrude.Networking.Common;
using MDrude.Networking.Utils;
using System.Reflection;
using System.Text;
using MDrude.Networking.WebSockets;

Logger.AddDefaultConsoleLogging();

await Examples.ExampleTwo();

Console.ReadLine();