using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

public static class Server
{
    private static TcpClient client = new();
    private static TcpClient host;
    private static TcpListener listener = new (new IPEndPoint(IPAddress.Loopback, 25565));
    private static int Upstream = 0;
    private static int Downstream = 0;

    public static void Main()
    {
        //Wait for host/client to connect
        listener.Start();
        Console.WriteLine("Listening for Host ...");
        host = listener.AcceptTcpClient();
        Console.WriteLine("Connected to Host at " + (host.Client.RemoteEndPoint as IPEndPoint).Address);

        Console.WriteLine("Listening for Client ...");
        client = listener.AcceptTcpClient();
        Console.WriteLine("Connected to Client at " + (client.Client.RemoteEndPoint as IPEndPoint).Address);

        //Start to listener loops and monitor throughput
        var clientHandler = Task.Run(ClientDataHandler);
        var hostHandler = Task.Run(HostDataHandler);
        var throughputMonitor = Task.Run(ThroughputMonitor);
        
        //Prevent program end
        Task.WaitAll(throughputMonitor, clientHandler, hostHandler);

    }

    private static async void ThroughputMonitor()
    {
        while (true)
        {
            var timer = Task.Delay(1000);
            Console.Write($"\r Upstream {Upstream}B/s | Downstream {Downstream}B/s         ");
            Downstream = 0;
            Upstream = 0;
            await timer;
        }
    }

    private static void ClientDataHandler()
    {
        byte[] data;
        while (true)
        {
            data = new byte[client.ReceiveBufferSize];
            var dataStream = client.GetStream();
            var l = dataStream.Read(data, 0, client.ReceiveBufferSize);

            //Console.WriteLine($"<CLIENT==>PRXCLI | {l} BYTES>");
            Upstream += l;
            
            host.GetStream().Write(data[..l]);
        }
    }

    private static void HostDataHandler()
    {
        byte[] data = new byte[host.ReceiveBufferSize];
        while (true)
        {
            var dataStream = host.GetStream();
            var l = dataStream.Read(data, 0, host.ReceiveBufferSize);

            //Console.WriteLine($"<HOST==>MCCLI | {l} BYTES >");
            Downstream += l;
            
            client.GetStream().Write(data[..l]);
        }
    }
}