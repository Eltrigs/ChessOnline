using Unity.Networking.Transport;

public enum OpCode
{
    KEEP_ALIVE = 1,
    WELCOME = 2,
    START_GAME = 3,
    MAKE_MOVE = 4,
    REMATCH = 5,
}

public class NetMessage
{
    public OpCode Code { set; get; }

    public virtual void serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
    }
    public virtual void deSerialize(DataStreamReader reader)
    {

    }
    public virtual void recievedOnClient()
    {

    }
    public virtual void recievedOnServer(NetworkConnection connection)
    {

    }
}
