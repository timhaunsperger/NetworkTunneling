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
    private static byte[] proxyData = new byte[proxyClient.ReceiveBufferSize];
    private static byte[] serverData = new byte[serverClient.ReceiveBufferSize];
    private static Thread _HandlerThread = new Thread(DataHandler);
    private static IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback,  25564);
    private static IPEndPoint proxyEndpoint;
    
    public static void Main()
    {
        Console.WriteLine("Please Enter Proxy IP, Skip for default");
        
        try
        {
            var ip = Console.ReadLine();
            proxyEndpoint = new IPEndPoint(IPAddress.Parse(String.IsNullOrEmpty(ip) ? "18.216.178.230" : ip),  25565);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Main();
            return;
        }

        _HandlerThread.Start();
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        ThroughputMonitor();
    
    }
    
    private static void ThroughputMonitor()
    {
        while (true)
        {
            Thread.Sleep(1000);
            Console.Write($"\r Upstream {Upstream}B/s | Downstream {Downstream}B/s            ");
            Upstream = 0;
            Downstream = 0;
        }
    }

    private static void EstablishTunnel() // Wait for proxy to have client before continuing
    {
        byte[] connReq = Encoding.UTF8.GetBytes("CLIENT CONNECTED");
        while (Encoding.UTF8.GetString(proxyData[0..connReq.Length]) != "CLIENT CONNECTED")
        {
            var l = proxyClient.GetStream().Read(proxyData, 0, connReq.Length);
            Console.WriteLine(Encoding.UTF8.GetString(proxyData[0..connReq.Length]));
        }
        serverClient.Close();
        serverClient = new TcpClient();
        serverClient.Connect(serverEndpoint);
        proxyClient.GetStream().Write(Encoding.UTF8.GetBytes("TUNNEL ESTABLISHED"));
    }

    private static async void DataHandler()
    {
        while (true)
        {
            Console.WriteLine($"Connecting to Proxy ...");
            try
            {
                proxyClient.Close();
                proxyClient = new TcpClient();
                proxyClient.Connect(proxyEndpoint);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to connect to proxy at {proxyEndpoint.Address}, Retrying in 5 seconds");
                Thread.Sleep(5000);
                continue;
            }
            
            Console.WriteLine($"Connected to Proxy at {proxyClient.Client.RemoteEndPoint}\n");
            EstablishTunnel();
            
            Console.WriteLine("Ready");
            var prxTask = ForwardDataAsync(proxyData, proxyClient, serverClient, true);
            var srvTask = ForwardDataAsync(serverData, serverClient, proxyClient, false);
            await Task.WhenAny(prxTask, srvTask);
            Console.WriteLine("--DISCONNECTED--");
        }

    }
    
    private static async Task ForwardDataAsync(byte[] buffer, TcpClient origin, TcpClient target, bool isUpstream)
    {
        int length;
        var cliDscMsg = Encoding.UTF8.GetBytes("CLIENT DISCONNECT");
        while (true)
        {
            try
            {
                //Forward data
                length = await origin.GetStream().ReadAsync(buffer);
                if (Encoding.UTF8.GetString(buffer[0..cliDscMsg.Length]) == "CLIENT DISCONNECT")
                {
                    Console.WriteLine("CLIENT DISCONNECT");
                    serverClient.Close();
                    proxyClient.Close();
                    return;
                }
                await target.GetStream().WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, length));
            
                //Log data transfer
                if (isUpstream)
                { Upstream += length; }
                else
                { Downstream += length; }
                
                //Break if disconnected
                if (length == 0) { throw new Exception("Null Packet Exception"); }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }
    }
}