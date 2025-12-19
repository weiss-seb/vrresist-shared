using System;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Core message protocol for communication between Tablet and HMD
    /// Used by both WebSocketClient (tablet) and TCPServer (HMD)
    /// </summary>
    [Serializable]
    public class EventMessage
    {
        public string type;
        public string[] content;

        public EventMessage(string eventName, string[] parameters)
        {
            this.type = eventName;
            this.content = parameters;
        }

        public EventMessage(string eventName)
        {
            this.type = eventName;
            this.content = new string[] { };
        }

        // Default constructor for JSON deserialization
        public EventMessage()
        {
            this.type = "";
            this.content = new string[] { };
        }
    }
}
