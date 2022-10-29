using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Client
{
    private static TcpClient proxyClient = new ();
    private static TcpClient serverClient = new ();
    private static bool connected = false;
    private static int Upstream = 0;
    private static int Downstream = 0;
    
    public static void Main()
    {
        Console.WriteLine("Please Enter Proxy IP, Skip for localhost");
        
        try
        {
            var ip = Console.ReadLine();
            var endPoint = new IPEndPoint(IPAddress.Parse(String.IsNullOrEmpty(ip) ? "127.0.0.1" : ip),  25565);
            Console.WriteLine("Connecting ...");
            proxyClient.Connect(endPoint);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Main();
        }
        
        Console.WriteLine($"Connected to Proxy at {proxyClient.Client.RemoteEndPoint}\n");
        
        Task.WaitAll(Task.Run(ThroughputMonitor), Task.Run(ProxyDataHandler));

    }
    
    private static async void ThroughputMonitor()
    {
        while (true)
        {
            var timer = Task.Delay(1000);
            Console.Write($"\r Upstream {Upstream}B/s | Downstream {Downstream}B/s");
            await timer;
        }
    }
    
    private static void ProxyDataHandler()
    {
        
        byte[] data = new byte[proxyClient.ReceiveBufferSize];
        while (true)
        {
            var l = proxyClient.GetStream().Read(data, 0, proxyClient.ReceiveBufferSize);
            //Console.WriteLine($"<PROXY==>MCSERVER | {l} BYTES>");
            Upstream += l;
            
            if (!connected)
            {
                serverClient.Connect(new IPEndPoint(IPAddress.Loopback, 25564));
                Console.WriteLine($"Connected to Server at {serverClient.Client.RemoteEndPoint}");
                connected = true;
                Task.Run(ServerDataHandler);
            }
            serverClient.GetStream().Write(data[..l]);
        }
    }
    
    private static void ServerDataHandler()
    {
        byte[] data = new byte[serverClient.ReceiveBufferSize];
        while (true)
        {
            var l = serverClient.GetStream().Read(data, 0, serverClient.ReceiveBufferSize);
            //Console.WriteLine($"<SERVER==>PROXY | {l} BYTES>");
            Downstream += 1;
            proxyClient.GetStream().Write(data[..l]);
        }
    }

    private int DecodeLength(byte[] data)
    {
        int value = 0;
        int position = 0;
        byte currentByte;

        for (int i = 0; i < data.Length; i++)
        {
            currentByte = data[i];
            value += (currentByte & 0x7f) << position;

            if ((currentByte & 0x80) == 0) break;

            position += 7;
            if (position >= 32)
            {
                throw new Exception("too big");
            }
        }
        return value;
    }
}