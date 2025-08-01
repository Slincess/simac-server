using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace server
{
    public class serverR
    {
        private TcpListener server;
        private bool Isrunning = false;
        //private List<TcpClient> clients = new();
        private List<UserPack> clients = new();
        private List<massage> SV_Massage_All = new();

        public async Task Run()
        {
            if (!Isrunning)
            {
                Isrunning = true;
                server = new TcpListener(IPAddress.Any, 5000);
                server.Start();
                clients.Clear();
                Console.WriteLine("server started..");
                await AcceptClients();
            }
        }

        private async Task AcceptClients()
        {

            while (Isrunning)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine("someoneConnected");
                UserPack newUser = new();
                newUser.CL_Tcp = client;
                newUser.CL_ID = clients.Capacity;
                _ = Task.Run(() => HandleClients(newUser));
                clients.Add(newUser);
                /*
                massage Joinmassage = new();
                Joinmassage.Massage = $"{newUser} joined the chat";
                SV_Massage_All.Add(Joinmassage);
                */
            }
        }

        private async Task HandleClients(UserPack user)
        {
            int MassageCount = 0;
            string CL_name = string.Empty;
            try
            {
                using NetworkStream stream = user.CL_Tcp.GetStream();
                byte[] massage_Recieved = new byte[5000];

                while (true)
                {
                    int massage_byteCount = await stream.ReadAsync(massage_Recieved, 0, massage_Recieved.Length);
                    if (massage_byteCount == 0)
                    {
                        Console.WriteLine($"{CL_name} left the chat");
                        clients.Remove(user);
                        user.CL_Tcp.Close();
                        break;
                    }

                    if (MassageCount == 0)
                    {
                        CL_name = Encoding.UTF8.GetString(massage_Recieved, 0, massage_byteCount);
                        MassageCount++;
                        Console.WriteLine($"{CL_name} joined the chat");
                        user.CL_Name = CL_name;
                        NetworkStream Stream = user.CL_Tcp.GetStream();
                        massage massage = new();
                        massage.Massage = $"{user.CL_Name} joined the chat";
                        massage.sender = "SERVER";

                        SV_Massage_All.Add(massage);
                        Broadcast_AllMassages(user.CL_Tcp.GetStream());
                    }
                    else
                    {
                        string massage_Recieved_Json = Encoding.UTF8.GetString(massage_Recieved, 0, massage_byteCount);
                        DataPacks data = JsonSerializer.Deserialize<DataPacks>(massage_Recieved_Json);
                        
                        if (data.Massage == "__DISCONNECT__" && data.CL_Name == "ADMIN")
                        {
                            massage massage = new();
                            massage.Massage = $"{user.CL_Name} left the chat";
                            massage.sender = "SERVER";

                            SV_Massage_All.Add(massage);
                            clients.Remove(user);
                            user.CL_Tcp.GetStream().Close();
                            user.CL_Tcp.Close();
                        }
                        else
                        {
                            massage massage = new();
                            massage.Massage = data.Massage;
                            massage.sender = data.CL_Name;
                            SV_Massage_All.Add(massage);
                        }

                        Broadcast(massage_Recieved, massage_byteCount);
                        Console.WriteLine($"{data.CL_Name}: {data.Massage}");
                        

                        //await stream.WriteAsync(massage_Recieved, 0, massage_byteCount);
                    }
                }
            }
            catch
            {
                clients.Remove(user);
                user.CL_Tcp.GetStream().Close();
                user.CL_Tcp.Close();
            }
        }

        private void Broadcast_AllMassages(Stream stream)
        {
            SV_Massages sV_Massages = new();
            sV_Massages.SV_allMassages = SV_Massage_All;
            string AllMassages_Json = JsonSerializer.Serialize(sV_Massages);
            byte[] Allmassages_byte = Encoding.UTF8.GetBytes(AllMassages_Json);
            Console.WriteLine(AllMassages_Json);
            stream.Write(Allmassages_byte);
        }

        private void Broadcast(byte[] massage, int lenght)
        {
            List<UserPack> discClient = new();

            foreach (var item in clients)
            {
                try
                {
                    NetworkStream Stream = item.CL_Tcp.GetStream();
                    Stream.WriteAsync(massage, 0, lenght);
                }
                catch
                {
                    discClient.Add(item);
                }
            }

            foreach (var item in discClient)
            {
                clients.Remove(item);
                item.CL_Tcp.GetStream().Close();
                item.CL_Tcp.Close();
            }
        }
    }
}

class UserPack
{
    public int CL_ID;
    public TcpClient CL_Tcp;
    public string? CL_Name;
}

public class MassageData
{
    public string massage;
    public string massage_sender;
}

public class DataPacks
{
    public string? CL_Name { get; set; }
    public string? Massage { get; set; }
}

public class SV_Massages
{
    public List<massage> SV_allMassages { get; set; }
}

public class massage
{
    public string? Massage { get; set; }
    public string? sender { get; set; }
    public string? Hour { get; set; }
}