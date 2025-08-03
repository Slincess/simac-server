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
        private List<message> SV_Message_All = new();
        private Users CCU = new();

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
                newUser.CL_ID = clients.Count;
                _ = Task.Run(() => HandleClients(newUser));
                clients.Add(newUser);
                /*
                message Joinmessage = new();
                Joinmessage.message = $"{newUser} joined the chat";
                SV_Message_All.Add(Joinmessage);
                */
            }
        }

        private async Task HandleClients(UserPack user)
        {
            int MessageCount = 0;
            string CL_name = string.Empty;
            try
            {
                using NetworkStream stream = user.CL_Tcp.GetStream();
                byte[] message_Recieved = new byte[5000];

                while (true)
                {
                    int message_byteCount = 1;
                    try
                    {
                        message_byteCount = await stream.ReadAsync(message_Recieved, 0, message_Recieved.Length);
                    }
                    catch (Exception)
                    {
                        
                            Console.WriteLine($"{CL_name} left the chat");
                            clients.Remove(user);
                            user.CL_Tcp.Close();
                            break;
                        
                    }

                    if (MessageCount == 0)
                    {
                        CL_name = Encoding.UTF8.GetString(message_Recieved, 0, message_byteCount);
                        MessageCount++;
                        Console.WriteLine($"{CL_name} joined the chat");
                        user.CL_Name = CL_name;
                        NetworkStream Stream = user.CL_Tcp.GetStream();
                        message message = new();
                        message.Message = $"{user.CL_Name} joined the chat";
                        message.sender = "SERVER";

                        SV_Message_All.Add(message);
                        Broadcast_AllMessages(user.CL_Tcp.GetStream());

                        CL_UserPack newCL_User = new();
                        newCL_User.CL_Name = user.CL_Name;
                        newCL_User.CL_ID = user.CL_ID;
                        user.CL_UserPack = newCL_User;
                        CCU.SV_CCU.Add(newCL_User);
                        Broadcast_CCU();
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
                            Console.WriteLine(user.CL_Name + "send a invalid Json (should be normal massage) ");
                            return;
                        }
                        
                        if (data.Message == "__DISCONNECT__" && data.CL_Name == "ADMIN")
                        {
                            DisconnectClient(user, "left the chat");
                        }
                        else
                        {
                            message message = new();
                            message.Message = data.Message;
                            message.sender = data.CL_Name;
                            SV_Message_All.Add(message);

                            DateTime now = DateTime.UtcNow;

                            while(user.MessageTimestamps.Count > 0 && (now - user.MessageTimestamps.Peek()).TotalSeconds > 4)
                            {
                                user.MessageTimestamps.Dequeue();
                            }
                            user.MessageTimestamps.Enqueue(now);

                            if (user.MessageTimestamps.Count >= 7)
                            {
                                message Kickmessage = new();
                                Kickmessage.Message = "__KICK__";
                                Kickmessage.sender = "__SERVER__";
                                string KickMessage_Json = JsonSerializer.Serialize(Kickmessage);
                                byte[] KickMessage_Byte = Encoding.UTF8.GetBytes(KickMessage_Json);

                                await user.CL_Tcp.GetStream().WriteAsync(KickMessage_Byte, 0, KickMessage_Byte.Length);
                                DisconnectClient(user, "was spamming and kicked out of the chat");
                                return;
                            }
                        }
                        Console.WriteLine(message_Recieved_Json);
                        Broadcast(message_Recieved, message_byteCount);
                        Console.WriteLine($"{data.CL_Name}: {data.Message}");
                        

                        //await stream.WriteAsync(message_Recieved, 0, message_byteCount);
                    }
                    //await Task.Delay(1000); // add protaction with max massage count per second and
                    //if someone detects to be spammer graylist his ip and if he does again send black list him.
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(user.CL_Name + ": " + e);
                clients.Remove(user);
                user.CL_Tcp.GetStream().Close();
                user.CL_Tcp.Close();
            }
        }

        private void DisconnectClient(UserPack user,string reason)
        {
            message message = new();
            message.Message = $"{user.CL_Name} {reason}";
            message.sender = "SERVER";

            SV_Message_All.Add(message);
            clients.Remove(user);
            user.CL_Tcp.GetStream().Close();
            user.CL_Tcp.Close();
            CCU.SV_CCU.Remove(user.CL_UserPack);
            Broadcast_CCU();
        }

        private async Task Broadcast_CCU()
        {
            try
            {
                string CCU_Json = JsonSerializer.Serialize(CCU);
                byte[] CCU_byte = new byte[1025];
                CCU_byte = Encoding.UTF8.GetBytes(CCU_Json);
               
                await Task.Delay(10);
                foreach (var item in clients)
                {
                    item.CL_Tcp.GetStream().WriteAsync(CCU_byte, 0, CCU_byte.Length);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private void Broadcast_AllMessages(Stream stream)
        {
            SV_Messages sV_Messages = new();
            sV_Messages.SV_allMessages = SV_Message_All;
            string AllMessages_Json = JsonSerializer.Serialize(sV_Messages);
            byte[] Allmessages_byte = Encoding.UTF8.GetBytes(AllMessages_Json);
            Console.WriteLine(AllMessages_Json);
            stream.WriteAsync(Allmessages_byte,0,Allmessages_byte.Length);
        }

        private void Broadcast(byte[] message, int lenght)
        {
            List<UserPack> discClient = new();

            foreach (var item in clients)
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
                clients.Remove(item);
                item.CL_Tcp.GetStream().Close();
                item.CL_Tcp.Close();
            }
        }
    }
}

public class UserPack
{
    public Queue<DateTime> MessageTimestamps = new();
    public int CL_ID { get; set; }
    public TcpClient CL_Tcp { get; set; }
    public string? CL_Name { get; set; }
    public CL_UserPack CL_UserPack { get; set; }
}

public class CL_UserPack
{
    public int CL_ID { get; set; }
    public string? CL_Name { get; set; }
}

public class MessageData
{
    public string message;
    public string message_sender;
}

public class DataPacks
{
    public string? CL_Name { get; set; }
    public string? Message { get; set; }
}

public class SV_Messages
{
    public List<message> SV_allMessages { get; set; }
}

public class message
{
    public string? Message { get; set; }
    public string? sender { get; set; }
    public string? Hour { get; set; }
}

public class Users
{
    public List<CL_UserPack> SV_CCU { get; set; } = new List<CL_UserPack>();
}
