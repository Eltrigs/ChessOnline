using Unity.Networking.Transport;
public class NetMakeMove : NetMessage
{
    public int originalX;
    public int originalY;
    public int destinationX;
    public int destinationY;
    public int teamID;

    public NetMakeMove()
    {
        Code = OpCode.MAKE_MOVE;
    }
    public NetMakeMove(DataStreamReader reader)
    {
        Code = OpCode.MAKE_MOVE;
        //After getting the OpCode and getting to go here, this function just
        //Gets us a team
        deSerialize(reader);
    }
    public override void serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(originalX);
        writer.WriteInt(originalY);
        writer.WriteInt(destinationX);
        writer.WriteInt(destinationY);
        writer.WriteInt(teamID);

    }
    public override void deSerialize(DataStreamReader reader)
    {
        originalX = reader.ReadInt();
        originalY = reader.ReadInt();
        destinationX = reader.ReadInt();
        destinationY = reader.ReadInt();
        teamID = reader.ReadInt();
    }

    public override void recievedOnClient()
    {
        //The question mark checks if anyone is subscribed to the C_WELCOME action
        NetUtility.C_MAKE_MOVE?.Invoke(this);
    }
    public override void recievedOnServer(NetworkConnection connection)
    {
        NetUtility.S_MAKE_MOVE?.Invoke(this, connection);
    }
}
