using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SonarrAutoImport
{
    public class Utils
    {
        public static DateTime getFromEpoch(long epoch)
        {
            var stamp = new DateTime(1970, 1, 1, 0, 0, 0).ToUniversalTime().AddMilliseconds(Convert.ToDouble(epoch));
            return stamp;

        }

        public static long getEpochTime(DateTime time)
        {
            TimeSpan t = time.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0);
            return (long)t.TotalMilliseconds;
        }

        public static T deserializeJSON<T>(string json)
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
