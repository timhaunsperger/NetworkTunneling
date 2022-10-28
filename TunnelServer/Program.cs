using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Xml.Schema;


public class TunnelServer
{
    static void Main()
    {
        if (Console.ReadLine() == "h")
        {
            var server = new Server();
        }
        else
        {
            var client = new Client();
        }
    }
}

