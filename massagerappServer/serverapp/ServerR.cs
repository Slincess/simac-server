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
        private TcpListener server;
        private bool Isrunning = false;
        private List<DataPacks> SV_Message_All = new();
        private Users CCU = new();

        public async Task Run()
        {
            if (!Isrunning)
            {
                Isrunning = true;
                server = new TcpListener(IPAddress.Any, 5000);
                server.Start();
                CCU.SV_CCU.Clear();
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
                newUser.CL_ID = CCU.SV_CCU.Count;
                _ = Task.Run(() => HandleClients(newUser));
                
                /*
                datapack Joinmessage = new();
                Joinmessage.datapack = $"{newUser} joined the chat";
                SV_Message_All.Add(Joinmessage);
                */
            }
        }

        private async Task HandleClients(UserPack User)
        {
            int MessageCount = 0;
            string Sender = string.Empty;
            try
            {
                using NetworkStream stream = User.CL_Tcp.GetStream();
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
                        
                            Console.WriteLine($"{Sender} left the chat");
                            CCU.SV_CCU.Remove(User);
                            User.CL_Tcp.Close();
                            break;
                        
                    }

                    if (MessageCount == 0)
                    {
                        
                        HandleClientFirstNeeding(ref Sender, ref message_Recieved, ref message_byteCount, User,ref MessageCount);
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
                            SV_Message_All.Add(datapack);

                            DateTime now = DateTime.UtcNow;

                            while(User.MessageTimestamps.Count > 0 && (now - User.MessageTimestamps.Peek()).TotalSeconds > 4)
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
                        

                        //await stream.WriteAsync(message_Recieved, 0, message_byteCount);
                    }
                    //await Task.Delay(1000); // add protaction with max massage count per second and
                    //if someone detects to be spammer graylist his ip and if he does again send black list him.
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(User.CL_Name + ": " + e);
                CCU.SV_CCU.Remove(User);
                User.CL_Tcp.GetStream().Close();
                User.CL_Tcp.Close();
            }
        }

        private void DisconnectClient(UserPack user,string reason)
        {
            DataPacks datapack = new();
            datapack.Message = $"{user.CL_Name} {reason}";
            datapack.Sender = "SERVER";

            SV_Message_All.Add(datapack);
            CCU.SV_CCU.Remove(user);
            user.CL_Tcp.GetStream().Close();
            user.CL_Tcp.Close();
            CCU.SV_CCU.Remove(user);
            Broadcast_CCU();
        }

        private async Task Broadcast_CCU()
        {
            try
            {
                string CCU_Json = JsonSerializer.Serialize(CCU);
                byte[] CCU_byte = new byte[1025];
                CCU_byte = Encoding.UTF8.GetBytes(CCU_Json);

                List<UserPack> Problematic = new();
               
                await Task.Delay(15);
                foreach (var item in CCU.SV_CCU)
                {
                    try
                    {
                        item.CL_Tcp.GetStream().WriteAsync(CCU_byte, 0, CCU_byte.Length);
                    }
                    catch (Exception)
                    {
                        Problematic.Add(item);
                    }
                }

                if(Problematic.Count > 0)
                {
                    foreach (var item in Problematic)
                    {
                        CCU.SV_CCU.Remove(item);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
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

            foreach (var item in CCU.SV_CCU)
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
                CCU.SV_CCU.Remove(item);
                item.CL_Tcp.GetStream().Close();
                item.CL_Tcp.Close();
            }
        }

        private void HandleClientFirstNeeding(ref string Sender, ref byte[] message_Recieved,ref int message_byteCount,UserPack user,ref int MessageCount)
        {
            Sender = Encoding.UTF8.GetString(message_Recieved, 0, message_byteCount);
            MessageCount++;
            Console.WriteLine($"{Sender} joined the chat");
            user.CL_Name = Sender;
            NetworkStream Stream = user.CL_Tcp.GetStream();

            DataPacks message = new();
            message.Message = $"{user.CL_Name} joined the chat";
            message.Sender = "SERVER";

            SV_Message_All.Add(message);
            Broadcast_AllMessages(user.CL_Tcp.GetStream());

            UserPack newCL_User = new();
            newCL_User.CL_Name = user.CL_Name;
            newCL_User.CL_ID = user.CL_ID;
            newCL_User.CL_Tcp = user.CL_Tcp;

            CCU.SV_CCU.Add(newCL_User);
            Broadcast_CCU();
        }
    }
}

public class UserPack
{
    [JsonIgnore] public Queue<DateTime> MessageTimestamps = new();
    [JsonIgnore]  public TcpClient CL_Tcp { get; set; }
    public string? CL_Name { get; set; }
    public int CL_ID { get; set; }
}

public class DataPacks
{
    public string? Sender { get; set; }
    public string? Message { get; set; }
}

public class SV_Messages
{
    public List<DataPacks> SV_allMessages { get; set; } = new();
}


public class Users
{
    public List<UserPack> SV_CCU { get; set; } = new List<UserPack>();
}
