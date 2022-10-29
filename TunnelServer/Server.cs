using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;

public static class Server
{
    private static TcpClient _client = new TcpClient();
    private static TcpClient _host = new TcpClient();
    private static TcpListener _listener = new TcpListener(new IPEndPoint(IPAddress.Any, 25565));
    private static int Upstream = 0;
    private static int Downstream = 0;
    private static Thread _HandlerThread = new Thread(DataHandler);
    private static byte[] _connReq = Encoding.UTF8.GetBytes("CLIENT CONNECTED");
    private static byte[] clientData = new byte[_client.ReceiveBufferSize];
    private static byte[] hostData = new byte[_host.ReceiveBufferSize];

    public static void Main()
    {
        //Start to listener loops
        _listener.Start();
        _HandlerThread.Start();
        
        //Prevent program end and monitor throughput
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        ThroughputMonitor();
    }

    private static void ThroughputMonitor()
    {
        while (true)
        {
            Console.WriteLine($"{DateTime.Now.ToShortTimeString()}|Upstream {Upstream/10}B/s | Downstream {Downstream/10}B/s         ");
            Downstream = 0;
            Upstream = 0;
            Thread.Sleep(10000);
        }
    }

    private static void ConnectClient()
    {
        Console.WriteLine("Waiting for Client ...");
        _client = _listener.AcceptTcpClient();
        Console.WriteLine("Connected to Client at " + _client.Client.RemoteEndPoint);
        Console.WriteLine("Waiting for Host confirmation ...");
        _host.GetStream().Write(_connReq); // Inform host that client connected
        var expectedResponse = Encoding.UTF8.GetBytes("TUNNEL ESTABLISHED");
        var length = _host.GetStream().Read(hostData, 0, expectedResponse.Length);
        if (Encoding.UTF8.GetString(hostData[0..length]) != "TUNNEL ESTABLISHED")
        {
            Console.WriteLine(Encoding.UTF8.GetString(hostData[0..length]));
            Console.WriteLine("Invalid Response from Host, Retrying");
            Thread.Sleep(5000);
            ConnectClient();
            return;
        }
        Console.WriteLine("Tunnel Established");
    }

    private static async void DataHandler()
    {
        //Replace broken connections
        Task? cliTask = null;
        Task? hstTask = null;
        Task? dcTask = null;
        while (true)
        {
            if (dcTask == hstTask) // replace host and client, host disconnect will always disconnect client
            {
                _host.Close();
                Console.WriteLine("Waiting for Host ...");
                _host = _listener.AcceptTcpClient();
                Console.WriteLine("Connected to Host at " + _host.Client.RemoteEndPoint);
                
                ConnectClient();
                
                cliTask = ForwardDataAsync(clientData, _client, _host, true);
                hstTask = ForwardDataAsync(hostData, _host, _client, false);
            }
            if (dcTask == cliTask) // replace client
            {
                _host.GetStream().Write(Encoding.UTF8.GetBytes("CLIENT DISCONNECT"));
                _client.Close();
                DataHandler();
                return;
            }
            dcTask = await Task.WhenAny(cliTask, hstTask); // detect disconnection
            Console.WriteLine("--DISCONNECTED--");
        }
    }
    
    private static async Task ForwardDataAsync(byte[] buffer, TcpClient origin, TcpClient target, bool isUpstream)
    {
        int length;
        while (true)
        {
            try
            {
                //forward data
                length = await origin.GetStream().ReadAsync(buffer);
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