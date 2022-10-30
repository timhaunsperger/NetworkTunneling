using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections;
using System.Diagnostics;
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

    private static string getBytePrefix(int numBytes)
    {
        var unit = "B";
        if (numBytes > 10000)
        {
            numBytes /= 1000;
            unit = "KB";
            if (numBytes > 10000)
            {
                numBytes /= 1000;
                unit = "MB";
            }
        }

        return $"{numBytes} {unit}";
    }
    
    private static void ThroughputMonitor()
    {
        var upstreamTotal = 0;
        var downstreamTotal = 0;
        while (Upstream == 0 && Downstream == 0)
        {
            Thread.Sleep(500);
        }
        while (true)
        {
            Upstream /= 5;
            Downstream /= 5;
            upstreamTotal += Upstream;
            downstreamTotal += Downstream;
            Console.Write(
                $"\r{DateTime.Now.ToShortTimeString()} | " +
                $"Upstream {getBytePrefix(upstreamTotal)}, {getBytePrefix(Upstream)}/s | " +
                $"Downstream {getBytePrefix(downstreamTotal)}, {getBytePrefix(Downstream)}/s         ");
            Downstream = 0;
            Upstream = 0;
            Thread.Sleep(5000);
        }
    }

    private static void ConnectClient()
    {
        Console.WriteLine("\nWaiting for Client ...");
        _client = _listener.AcceptTcpClient();
        Console.WriteLine("\nConnected to Client at " + _client.Client.RemoteEndPoint);
        Console.WriteLine("\nWaiting for Host confirmation ...");
        _host.GetStream().Write(_connReq); // Inform host that client connected
        var expectedResponse = Encoding.UTF8.GetBytes("TUNNEL ESTABLISHED");
        var length = _host.GetStream().Read(hostData, 0, expectedResponse.Length);
        if (Encoding.UTF8.GetString(new ReadOnlySpan<byte>(hostData, 0, length)) != "TUNNEL ESTABLISHED")
        {
            Console.WriteLine(Encoding.UTF8.GetString(new ReadOnlySpan<byte>(hostData, 0, length)));
            Console.WriteLine("\nInvalid Response from Host, Retrying");
            Thread.Sleep(5000);
            ConnectClient();
            return;
        }
        Console.WriteLine("\nTunnel Established");
    }

    private static async void DataHandler()
    {
        //Replace broken connections
        Task cliTask = Task.CompletedTask;
        Task hstTask = Task.CompletedTask;
        Task dcTask = Task.CompletedTask;
        while (true)
        {
            if (dcTask == hstTask) // replace host and client, host disconnect will always disconnect client
            {
                _host.Close();
                Console.WriteLine("\nWaiting for Host ...");
                _host = _listener.AcceptTcpClient();
                Console.WriteLine("\nConnected to Host at " + _host.Client.RemoteEndPoint);
                
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
            Console.WriteLine("\n--DISCONNECTED--");
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
                length = await origin.GetStream().ReadAsync(buffer, 0, buffer.Length);
                await target.GetStream().WriteAsync(buffer, 0, length);
            
                //Log data transfer
                if (isUpstream)
                { Upstream += length; }
                else
                { Downstream += length; }
                
                //Break if disconnected
                if (length == 0) { throw new Exception("\nNull Packet Exception"); }
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
                return;
            }

        }
    }
}