using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SonarrAuto
{
    [DataContract]
    public class Transform
    {
        [DataMember]
        public int order { get; set; }
        [DataMember]
        public string search { get; set; }
        [DataMember]
        public string replace { get; set; }
    }

    [DataContract]
    public class ServiceSettings
    {
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string downloadsFolder { get; set; }
        [DataMember]
        public string mappingPath { get; set; }
        [DataMember]
        public string apiKey { get; set; }
        [DataMember]
        public List<Transform> transforms { get; set; }
        [DataMember]
        public string importMode { get; set; } = "Move";
        [DataMember]
        public int timeoutSecs { get; set; }
        [DataMember]
        public bool trimFolders { get; set; }
    }

    [DataContract]
    public class Settings
    {
        [DataMember]
        public ServiceSettings sonarr { get; set; }
        [DataMember]
        public ServiceSettings radarr { get; set; }

        [DataMember]
        public string logLocation { get; set; }

        public static Settings Read(string path)
        {
            if (File.Exists(path))
            {

                string json = File.ReadAllText(path);

                var settings = deserialiseJson<Settings>(json);

                return settings;
            }
            else
            {
                return null;
            }
        }

        private static T deserialiseJson<T>(string json)
        {
            var instance = Activator.CreateInstance<T>();
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(instance.GetType());
                return (T)serializer.ReadObject(ms);
            }
        }
    }
}
