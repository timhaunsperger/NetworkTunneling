using System.Net;
using System.Net.Sockets;
using System.Text;

public class Server
{
    private List<Socket> clients = new();
    private Socket client;
    private Socket host;
    private Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

    public Server()
    {
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 25565));
        
        Console.WriteLine("Listening for Host ...");
        listenSocket.Listen(10);
        host = listenSocket.Accept();
        Console.WriteLine("Connected to Host!");
        
        //var clientsTask = Task.Run(AcceptClients);
        var hostListener = Task.Run(HostDataHandler);
        var clientListener = Task.Run(ClientDataHandler);
        Task.WaitAll(new Task[] { clientListener, hostListener, /*clientsTask*/});
    }
    
    private void ClientDataHandler()
    {
        byte[] data = new byte[65536];
        client = listenSocket.Accept();
        Console.WriteLine("Connected to " + (client.RemoteEndPoint as IPEndPoint).Address);
        while (true)
        {
            int k = client.Receive(data);
            Console.WriteLine($"<CLIENT DATA {k} BYTES>");
            foreach (var byt in data[..k])
            {
                Console.Write(byt);
            }
            Console.WriteLine("</DATA>");
            host.Send(data[..k]);
        }
    }

    private void HostDataHandler()
    {
        byte[] data = new byte[65536];
        while (true)
        {
            int k = host.Receive(data);
            Console.WriteLine($"<HOST DATA {k} BYTES>");
            Console.WriteLine(k);
            for (int i = 0; i < k; i++)
            {
                Console.Write(data[k]);
            }
            Console.WriteLine("</DATA>");
            clients[0].Send(data[..k]);

        }
    }
    // private void AcceptClients()
    // {
    //     Console.WriteLine("Listening for Clients ...");
    //     while (true)
    //     {
    //         
    //     }
    // }
    
}