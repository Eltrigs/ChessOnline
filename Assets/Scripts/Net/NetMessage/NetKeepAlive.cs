using Unity.Networking.Transport;
public class NetKeepAlive : NetMessage
{
    public NetKeepAlive() //This is when you're making the message
    {
        Code = OpCode.KEEP_ALIVE;
    }
    
    public NetKeepAlive(DataStreamReader reader) //This is when you have recieved a message
    {
        Code = OpCode.KEEP_ALIVE;
        deSerialize(reader);
    }
    public OpCode Code { set; get; }

    public override void serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
    }
    public override void deSerialize(DataStreamReader reader)
    {
         
    }
    public override void recievedOnClient()
    {
        NetUtility.C_KEEP_ALIVE?.Invoke(this);
    }
    public override void recievedOnServer(NetworkConnection connection)
    {
        NetUtility.S_KEEP_ALIVE?.Invoke(this, connection);
    }
}
