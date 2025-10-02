using serverapp;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace server
{
    public class serverR
    {
        private TcpListener? server;
        private bool Isrunning = false;
        private CancellationToken Ct;
        UserPack newUser = new();

        public async Task Run(CancellationToken ct)
        {

            if (!Isrunning)
            {
                Isrunning = true;
                Ct = ct;
                server = new TcpListener(IPAddress.Any, 5000);
                server.Start();
                Console.WriteLine("server started..");
                _ = AcceptClients();
            }
        }

        public async Task StopServer()
        {
            if (!Isrunning) return;
            Isrunning = false;
            server?.Stop();
            server = null;
            foreach (var item in S_analytics.Instance.GetCCU().SV_CCU.ToList())
            {
                DisconnectClient(item, "bad news server is down ✌️");
            }
        }

        private async Task AcceptClients()
        {
            try
            {
                while (Isrunning)
                {
                    if (Ct.IsCancellationRequested)
                    {
                        break;
                    }

                    TcpClient client = await server.AcceptTcpClientAsync();
                    Console.WriteLine("someoneConnected");
                    newUser = new();
                    newUser.CL_Tcp = client;
                    newUser.CL_ID = S_analytics.Instance.GetCCU().SV_CCU.Count;
                    _ = Task.Run(() => HandleClients(newUser));

                    /*
                    datapack Joinmessage = new();
                    Joinmessage.datapack = $"{newUser} joined the chat";
                    SV_Message_All.Add(Joinmessage);
                    */
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"AcceptClient error: {e}");
            }
        }

        private async Task HandleClients(UserPack User)
        {
            int MessageCount = 0;
            string Sender = string.Empty;
            try
            {
                using NetworkStream stream = User.CL_Tcp.GetStream();
                byte[] message_Recieved = new byte[15000000];

                while (Isrunning)
                {
                    if (Ct.IsCancellationRequested)
                    {
                        break;
                    }
                    int message_byteCount = 1;
                    try
                    {
                        message_byteCount = await stream.ReadAsync(message_Recieved, 0, message_Recieved.Length);
                    }
                    catch (Exception)
                    {

                        Console.WriteLine($"{Sender} left the chat SERVER");
                        DisconnectClient(User, "lost connection");
                        break;

                    }

                    if (MessageCount == 0)
                    {
                        MessageCount++;
                        HandleClientFirstNeeding(ref Sender, ref message_Recieved, ref message_byteCount, User, ref MessageCount);
                    }
                    else
                    {
                        string message_Recieved_Json = Encoding.UTF8.GetString(message_Recieved, 0, message_byteCount);
                        DataPacks data;

                        try
                        {
                            data = JsonSerializer.Deserialize<DataPacks>(message_Recieved_Json);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(User.CL_Name + "send a invalid Json (should be normal massage) ");
                            return;
                        }

                        if (data.Message == "__DISCONNECT__" && data.Sender == "ADMIN")
                        {
                            DisconnectClient(User, "left the chat");
                            return;
                        }
                        else
                        {
                            DataPacks datapack = new();
                            datapack.Message = data.Message;
                            datapack.Sender = data.Sender;
                            if (datapack.Picture != null)
                                datapack.Picture = data.Picture;
                            S_analytics.Instance.AddMessage_List(datapack);

                            DateTime now = DateTime.UtcNow;

                            while (User.MessageTimestamps.Count > 0 && (now - User.MessageTimestamps.Peek()).TotalSeconds > 4)
                            {
                                User.MessageTimestamps.Dequeue();
                            }
                            User.MessageTimestamps.Enqueue(now);

                            if (User.MessageTimestamps.Count >= 7)
                            {
                                DataPacks Kickmessage = new();
                                Kickmessage.Message = "__KICK__";
                                Kickmessage.Sender = "__SERVER__";
                                string KickMessage_Json = JsonSerializer.Serialize(Kickmessage);
                                byte[] KickMessage_Byte = Encoding.UTF8.GetBytes(KickMessage_Json);

                                await User.CL_Tcp.GetStream().WriteAsync(KickMessage_Byte, 0, KickMessage_Byte.Length);
                                DisconnectClient(User, "was spamming and kicked out of the chat");
                                return;
                            }

                        }
                        Console.WriteLine(message_Recieved_Json);
                        Broadcast(message_Recieved, message_byteCount);
                        Console.WriteLine($"{data.Sender}: {data.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(User.CL_Name + ": " + e);
                DisconnectClient(User, "server stopped");
            }
        }

        private void DisconnectClient(UserPack user, string reason)
        {

            try
            {
                if (user.CL_Tcp.Connected)
                {
                    DataPacks data = new()
                    {
                        Message = $"{user.CL_Name} {reason}",
                        Sender = "SERVER"
                    };
                    string leaveJson = JsonSerializer.Serialize(data);
                    byte[] leaveByte = Encoding.UTF8.GetBytes(leaveJson);

                    try
                    {
                        user.CL_Tcp.GetStream().Write(leaveByte, 0, leaveJson.Length);
                    }
                    catch
                    {
                    }
                    S_analytics.Instance.AddMessage_List(data);
                }
            }
            catch
            {
            }
            try { user.CL_Tcp.GetStream().Close(); } catch { }
            ;
            try { user.CL_Tcp.Close(); } catch { }
            S_analytics.Instance.removeCCU(user);
            if (!reason.Contains("server"))
                Broadcast_CCU();

            Console.WriteLine(S_analytics.Instance.GetCCU_Json());

        }

        private async Task Broadcast_CCU()
        {
            try
            {
                string CCU_Json = JsonSerializer.Serialize(S_analytics.Instance.GetCCU());
                byte[] CCU_byte = new byte[1025];
                CCU_byte = Encoding.UTF8.GetBytes(CCU_Json);

                List<UserPack> Problematic = new();
                await Task.Delay(15);
                foreach (var item in S_analytics.Instance.GetCCU().SV_CCU.ToList())
                {
                    try
                    {

                        if (item.CL_Tcp != null && item.CL_Tcp.Connected)
                            item.CL_Tcp.GetStream().WriteAsync(CCU_byte, 0, CCU_byte.Length);

                    }
                    catch
                    {
                        Problematic.Add(item);
                    }
                }

                if (Problematic.Count > 0)
                {
                    foreach (var item in Problematic)
                    {
                        DisconnectClient(item, "left the chat broudcast");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void Broadcast_AllMessages(Stream stream)
        {
            byte[] Allmessages_byte = Encoding.UTF8.GetBytes(S_analytics.Instance.GetMessages_Json());
            //Console.WriteLine(AllMessages_Json);
            stream.WriteAsync(Allmessages_byte, 0, Allmessages_byte.Length);
        }

        private void Broadcast(byte[] message, int lenght)
        {
            List<UserPack> discClient = new();

            foreach (var item in S_analytics.Instance.GetCCU().SV_CCU)
            {
                try
                {
                    NetworkStream Stream = item.CL_Tcp.GetStream();
                    Stream.WriteAsync(message, 0, lenght);
                }
                catch
                {
                    discClient.Add(item);
                }
            }

            foreach (var item in discClient)
            {
                DisconnectClient(item, "left the chat broadcast problem");
            }
        }

        private void HandleClientFirstNeeding(ref string Sender, ref byte[] message_Recieved, ref int message_byteCount, UserPack user, ref int MessageCount)
        {
            if (!S_analytics.Instance.GetCCU().SV_CCU.Contains(user))
            {
                NetworkStream Stream = user.CL_Tcp.GetStream();
                DataPacks message = new();
                UserPack newCL_User = new();

                Sender = Encoding.UTF8.GetString(message_Recieved, 0, message_byteCount);
                Console.WriteLine($"{Sender} joined the chat");
                user.CL_Name = Sender;

                message.Message = $"{user.CL_Name} joined the chat";
                message.Sender = "SERVER";

                S_analytics.Instance.AddMessage_List(message);

                S_analytics.Instance.GetCCU().SV_CCU.Add(user);
                Broadcast_CCU();
                Broadcast_AllMessages(user.CL_Tcp.GetStream());
            }
        }

        private async Task HandleSpams(UserPack User)
        {
            DateTime now = DateTime.UtcNow;

            while (User.MessageTimestamps.Count > 0 && (now - User.MessageTimestamps.Peek()).TotalSeconds > 4)
            {
                User.MessageTimestamps.Dequeue();
            }
            User.MessageTimestamps.Enqueue(now);

            if (User.MessageTimestamps.Count >= 7)
            {
                DataPacks Kickmessage = new();
                Kickmessage.Message = "__KICK__";
                Kickmessage.Sender = "__SERVER__";
                string KickMessage_Json = JsonSerializer.Serialize(Kickmessage);
                byte[] KickMessage_Byte = Encoding.UTF8.GetBytes(KickMessage_Json);

                await User.CL_Tcp.GetStream().WriteAsync(KickMessage_Byte, 0, KickMessage_Byte.Length);
                DisconnectClient(User, "was spamming and kicked out of the chat");
                return;
            }
        }

    }
}

public class UserPack
{
    [JsonIgnore] public Queue<DateTime> MessageTimestamps = new();
    [JsonIgnore] public TcpClient CL_Tcp { get; set; }
    public string? CL_Name { get; set; }
    public int CL_ID { get; set; }
}
public class DataPacks
{
    public string? Sender { get; set; }
    public string? Message { get; set; }
    public string? Picture { get; set; }
}

public class SV_Messages
{
    public List<DataPacks> SV_allMessages { get; set; } = new();
}


public class Users
{
    public List<UserPack> SV_CCU { get; set; } = new List<UserPack>();
}