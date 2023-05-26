using System;

namespace Pomelo.UnityWebSocketPomelo
{

    public enum ProtocolState
    {
        start = 1,          // Just open, need to send handshaking
        handshaking = 2,    // on handshaking process
        working = 3,		// can receive and send data 
        closed = 4,		    // on read body
    }
}