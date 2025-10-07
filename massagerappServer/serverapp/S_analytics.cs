using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        private analytics_var variable;

        /* delete this if you are sure everything works
        private List<DataPacks> Messages = new();
        private Users CCU = new();
        private Users UAU = new();
        private List<Image> Images = new();

        Dictionary<string, Image> imagesDictionary = new();
        private string ImagesPath;
        */


        public S_analytics()
        {
          LoadAnalytics();
        }

        public void LoadAnalytics()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "Analytics");
            string path_ot_json = Path.Combine(path, "AnalyticsJson.json");
            if (!File.Exists(path_ot_json))
            {
                string newJson = JsonSerializer.Serialize(new analytics_var());
                if (!File.Exists(Path.Combine(path, "AnalyticsJson.json")))
                    Directory.CreateDirectory(path);

                File.WriteAllText(@$"{path}\AnalyticsJson.json", newJson);
            }
            try
            {
                variable = JsonSerializer.Deserialize<analytics_var>(File.ReadAllText(path_ot_json));
            }
            catch (Exception)
            {
                Debug.WriteLine("couldnt LoadAnalytics. is json file corrupted?");
                throw;
            }
        }

        public void SaveInfo()
        {
            string AppDataPath = Path.Combine(Environment.CurrentDirectory, "Analytics");
            if (Directory.Exists(AppDataPath) && Path.Exists(Path.Combine(AppDataPath, "AnalyticsJson.json")))
            {
                string infojson = JsonSerializer.Serialize(variable);

                File.WriteAllText(Path.Combine(AppDataPath, "AnalyticsJson.json"), infojson);
            }
            else
            {
                analytics_var infoNew = new();
                string newJson = JsonSerializer.Serialize(infoNew);
                if (!Path.Exists(Path.Combine(AppDataPath, "AnalyticsJson.json")))
                    Directory.CreateDirectory(AppDataPath);

                File.WriteAllText(@$"{AppDataPath}\AnalyticsJson.json", newJson);
            }
        }

        #region file handling
        /// <summary>
        /// this will get image as array and turn into normal image
        /// and save it into Images list BOMBOCLAT
        /// </summary>
        public string AddImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            string key = DateTime.UtcNow.Ticks + Guid.NewGuid().ToString();
            if (!Path.Exists(variable.ImagesPath)) { Directory.CreateDirectory(variable.ImagesPath); }
            string path = Path.Combine(variable.ImagesPath, file.Name + ".png");

            using (var image = Image.Load(file.OpenReadStream())) 
            { 
                image.Save(path);
                variable.imagesDictionary.Add(key, path);
            }
            SaveInfo();
            return key;
        }
        

        public byte[] GetImage(string Key)
        {

            using (var form = new MultipartFormDataContent())
            {
                string imagePath;
                byte[] imagebytes;
                if (variable.imagesDictionary.TryGetValue(Key, out imagePath))
                {
                    using var ms = new MemoryStream();
                    using (var image = Image.Load(imagePath))
                    {
                        image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                        imagebytes = ms.ToArray();
                    }
                    return imagebytes;

                }
                else
                {
                    Console.WriteLine("Couldnt find the pic");
                    return null;
                }

            }
        }
        
        #endregion

        #region Message
        public void AddMessage_List(DataPacks data)
        {
            variable.Messages.Add(data);
        }

        public void SaveMessages()
        {

        }

        public string GetMessages_Json()
        {
            SV_Messages sV_Messages = new();
            sV_Messages.SV_allMessages = variable.Messages;
            return JsonSerializer.Serialize(sV_Messages);
        }

        public void ClearChat()
        {
            variable.Messages.Clear();
            SaveInfo();
        }
        #endregion

        #region CCU
        public void AddCCU()
        {

        }

        public void removeCCU(UserPack UP)
        {
            variable.CCU.SV_CCU.Remove(UP);
        }

        public Users GetCCU()
        {
            return variable.CCU;
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

public class analytics_var
{
    public List<DataPacks> Messages { get; set; } = new();
    [JsonIgnore]public Users CCU { get; set; } = new();
    public Users UAU { get; set; } = new();

    public Dictionary<string, string> imagesDictionary { get; set; } = new();
    public string ImagesPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "images");
}
