using System.Net;
using System.Net.Sockets;
using System.Text;

public class Client
{
    private Socket proxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
    private Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
    public Client()
    {
        Console.WriteLine("Connecting ...");
        proxySocket.Connect(new IPEndPoint(IPAddress.Loopback, 25565));
        Console.WriteLine($"Connected to Proxy at {proxySocket.RemoteEndPoint}");
        var proxyListener = Task.Run(ProxyDataHandler);
        var serverListener = Task.Run(ServerDataHandler);
        Task.WaitAll(new Task[] { proxyListener, serverListener});
        
    }
    private void ProxyDataHandler()
    {
        bool connected = false;
        byte[] data = new byte[65536];
        while (true)
        {
            
            int k = proxySocket.Receive(data);
            Console.WriteLine($"<PROXY DATA {k} BYTES>");
            Console.WriteLine(k);
            for (int i = 0; i < k; i++)
            {
                Console.Write(data[k]);
            }
            Console.WriteLine("</DATA>");
            if (!connected)
            {
                serverSocket.Connect(new IPEndPoint(IPAddress.Loopback, 25564));
                Console.WriteLine($"Connected to Server at {serverSocket.RemoteEndPoint}");
                connected = true;
            }
            serverSocket.Send(data[..k]);
        }
    }

    private void ServerDataHandler()
    {
        byte[] data = new byte[65536];
        while (true)
        {
            int k = serverSocket.Receive(data);
            Console.WriteLine($"<SERVER DATA {k} BYTES>");
            Console.WriteLine(k);
            for (int i = 0; i < k; i++)
            {
                Console.Write(data[k]);
            }
            Console.WriteLine("</DATA>");
            serverSocket.Send(data[..k]);
        }
    }
}