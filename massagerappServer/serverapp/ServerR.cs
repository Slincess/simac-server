using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class serverR
    {
        private TcpListener server;
        private bool Isrunning = false;

        public async Task Run()
        {
            if (!Isrunning)
            {
                Isrunning = true;
                server = new TcpListener(IPAddress.Any, 5000);
                server.Start();
                await AcceptClients(); 
            }
        }

        private async Task AcceptClients()
        {
            while (Isrunning)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClients(client));
                Console.WriteLine("someoneConnected");
            }
        }

        private async Task HandleClients(TcpClient client)
        {
            int MassageCount = 0;
            string CL_name = string.Empty;
            try
            {

                using NetworkStream stream = client.GetStream();
                byte[] massage = new byte[1024];

                while (true)
                {
                    int massage_int = await stream.ReadAsync(massage, 0, massage.Length);
                    if (massage_int == 0) break;

                    if(MassageCount == 0)
                    {
                        CL_name = Encoding.UTF8.GetString(massage,0,massage_int);
                        MassageCount++;
                        Console.WriteLine($"{CL_name} joined the chat");
                    }
                    else
                    {
                        string massage_string = Encoding.UTF8.GetString(massage, 0, massage_int);
                        Console.WriteLine($"{CL_name}: {massage_string}");

                        await stream.WriteAsync(massage, 0, massage_int);
                    }
                }
            }
            catch
            {

            }
        }
    }
}
