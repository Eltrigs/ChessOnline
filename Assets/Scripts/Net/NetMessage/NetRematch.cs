using Unity.Networking.Transport;


public class NetRematch : NetMessage
{

    public int teamID;
    public byte wantRematch;
    public NetRematch()
    {
        Code = OpCode.REMATCH;
    }
    public NetRematch(DataStreamReader reader)
    {
        Code = OpCode.REMATCH;
        //After getting the OpCode and getting to go here, this function just
        //Gets us a team
        deSerialize(reader);
    }
    public override void serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(teamID);
        writer.WriteByte(wantRematch);
    }
    public override void deSerialize(DataStreamReader reader)
    {
        teamID = reader.ReadInt();
        wantRematch = reader.ReadByte();
    }

    public override void recievedOnClient()
    {
        //The question mark checks if anyone is subscribed to the C_WELCOME action
        NetUtility.C_REMATCH?.Invoke(this);
    }
    public override void recievedOnServer(NetworkConnection connection)
    {
        NetUtility.S_REMATCH?.Invoke(this, connection);
    }
}
