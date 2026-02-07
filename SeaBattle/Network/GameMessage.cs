using Newtonsoft.Json;
using SeaBattle.Enums;

namespace SeaBattle.Network
{
    public class GameMessage
    {
        public MessageType Type { get; set; }
        public object Data { get; set; }

        public GameMessage() { }

        public GameMessage(MessageType type, object data)
        {
            Type = type;
            Data = data;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static GameMessage FromJson(string json)
        {
            return JsonConvert.DeserializeObject<GameMessage>(json);
        }
    }

    public class ShotData
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class ShotResultData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Result { get; set; }
    }
}