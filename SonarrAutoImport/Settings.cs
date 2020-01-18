using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SonarrAuto
{
    [DataContract]
    public class EmailSettings
    {
        [DataMember]
        public string smtpserver { get; set; }
        [DataMember]
        public int smtpport { get; set;  }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public string toaddress { get; set; }
        [DataMember]    
        public string fromaddress { get; set; }
        [DataMember]
        public string toname { get; set; }
    }

    [DataContract]
    public class SonarrSettings
    {
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string downloadsFolder { get; set; }
        [DataMember]
        public string mappingPath { get; set; }
        [DataMember]
        public string apiKey { get; set; }
    }

    [DataContract]
    public class RadarrSettings
    {
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string downloadsFolder { get; set; }
        [DataMember]
        public string mappingPath { get; set; }
        [DataMember]
        public string apiKey { get; set; }
    }

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
    public class TransformSettings
    {
        [DataMember]
        public List<Transform> transforms { get; set; }
    }

    [DataContract]
    public class Settings
    {
        [DataMember]
        public SonarrSettings sonarr { get; set; }
        [DataMember]
        public RadarrSettings radar { get; set; }
        [DataMember]
        public TransformSettings transforms { get; set; }

        [DataMember]
        public string logLocation { get; set; }

        public static Settings Read(string path)
        {
            string json = File.ReadAllText(path);

            var settings = deserialiseJson<Settings>(json);

            return settings;
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
