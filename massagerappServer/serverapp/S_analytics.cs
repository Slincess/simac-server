using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace serverapp
{
    public class S_analytics
    {
        private List<DataPacks> Messages = new();
        private Users CCU = new();
        private Users UAU = new();

        public void AddMessage_List(DataPacks data)
        {
            Messages.Add(data);
        }

        public void SaveMessages()
        {

        }

        public string GetMessages_Json()
        {
            SV_Messages sV_Messages = new();
            sV_Messages.SV_allMessages = Messages;
            return JsonSerializer.Serialize(sV_Messages);
        }

        public void AddCCU()
        {

        }

        public Users GetCCU()
        {
            return CCU;
        }
        public string GetCCU_Json()
        {
            return JsonSerializer.Serialize(GetCCU());
        }

        public void AddUAU()
        {

        }

        public void GetUAU()
        {

        }
    }
}
