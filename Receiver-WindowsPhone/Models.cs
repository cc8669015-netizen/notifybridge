using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace NotifyBridge
{
    /// <summary>One notification forwarded from the phone. Matches the JSON the Android app sends.</summary>
    [DataContract]
    public class BridgeNotification
    {
        [DataMember(Name = "app")] public string AppName { get; set; }      // e.g. "Messages", "MyBank"
        [DataMember(Name = "title")] public string Title { get; set; }
        [DataMember(Name = "text")] public string Text { get; set; }
        [DataMember(Name = "category")] public string Category { get; set; } // "message", "banking", "other"
        [DataMember(Name = "ticks")] public long SentTicksUtc { get; set; }

        public DateTime ReceivedLocal { get; set; } // set by the receiver, not sent by the phone

        public string TimeText
        {
            get { return ReceivedLocal.ToString("HH:mm"); }
        }

        public string Heading
        {
            get { return string.IsNullOrEmpty(AppName) ? "Notification" : AppName; }
        }
    }

    public static class Json
    {
        public static string Write<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static T Parse<T>(string json) where T : class
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
            }
        }
    }
}
