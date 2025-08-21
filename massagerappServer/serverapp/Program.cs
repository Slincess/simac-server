using server;
using serverapp;
using System;
using System.Threading.Tasks;

namespace MyApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            AdminPanelScript adminpanel = new();
            await Task.Run(() => adminpanel.Run());
            /*
            serverR server = new();
            await Task.Run(() => server.Run());
            */
        }
    }
}