using System.Text.Json;
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
            return JsonSerializer.Serialize(this);
        }

        public static GameMessage FromJson(string json)
        {
            return JsonSerializer.Deserialize<GameMessage>(json);
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
        public CellState Result { get; set; }
        public bool IsShipDestroyed { get; set; }
    }

    public class ConnectionData
    {
        public string PlayerName { get; set; }
    }
}