using server;
using System;
using System.Threading.Tasks;

namespace MyApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            serverR server = new();
             await server.Run();
        }
    }
}