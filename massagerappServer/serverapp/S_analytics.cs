using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing;
using SixLabors.ImageSharp;

namespace serverapp
{
    public class S_analytics
    {
        private static S_analytics instance;
        public static S_analytics Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new();
                }
                return instance;
            }
        }

        private List<DataPacks> Messages = new();
        private Users CCU = new();
        private Users UAU = new();
        private List<Image> Images = new();
        Dictionary<string, Image> imagesDictionary = new();

        /// <summary>
        /// this will get image as array and turn into normal image
        /// and save it into Images list BOMBOCLAT
        /// </summary>
        public string AddImage(byte[] ImageArray)
        {
            using var ms = new MemoryStream(ImageArray);
            Image image = Image.Load(ms);
            Images.Add(image);
            string key = DateTime.UtcNow.Ticks + Guid.NewGuid().ToString();
            imagesDictionary.Add(key, image);
            return key;
        }

        public byte[] GetImage(string Key)
        {
            Image image;
            if (imagesDictionary.TryGetValue(Key,out image))
            {
                using var ms = new MemoryStream();
                image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                return ms.ToArray();

            }
            else
            {
                return null;
            }
        }

        #region Message
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
        #endregion

        #region CCU
        public void AddCCU()
        {

        }

        public void removeCCU(UserPack UP)
        {
            CCU.SV_CCU.Remove(UP);
        }

        public Users GetCCU()
        {
            return CCU;
        }
        public string GetCCU_Json()
        {
            return JsonSerializer.Serialize(GetCCU());
        }
        #endregion

        #region UAU
        public void AddUAU()
        {

        }

        public void GetUAU()
        {

        }
        #endregion
    }
}
