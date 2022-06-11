using Unity.Networking.Transport;

public class NetWelcome : NetMessage
{
    public int AssignedTeam { set; get; }
    public NetWelcome()
    {
        Code = OpCode.WELCOME;
    }
    public NetWelcome(DataStreamReader reader)
    {
        Code = OpCode.WELCOME;
        //After getting the OpCode and getting to go here, this function just
        //Gets us a team
        deSerialize(reader);
    }
    public override void serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(AssignedTeam);
    }
    public override void deSerialize(DataStreamReader reader)
    {
        //Already read the byte in the onData function, so its not neccesary here
        //Now read the integer in the message since we know the OpCode
        AssignedTeam = reader.ReadInt();
    }

    public override void recievedOnClient()
    {
        //The question mark checks if anyone is subscribed to the C_WELCOME action
        NetUtility.C_WELCOME?.Invoke(this);
    }
    public override void recievedOnServer(NetworkConnection connection)
    {
        NetUtility.S_WELCOME?.Invoke(this, connection);
    }
}
