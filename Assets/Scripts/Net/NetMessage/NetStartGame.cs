using Unity.Networking.Transport;

public class NetStartGame : NetMessage
{
    public NetStartGame()
    {
        Code = OpCode.START_GAME;
    }
    public NetStartGame(DataStreamReader reader)
    {
        Code = OpCode.START_GAME;
        //After getting the OpCode and getting to go here, this function just
        //Gets us a team
        deSerialize(reader);
    }
    public override void serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
    }
    public override void deSerialize(DataStreamReader reader)
    {

    }

    public override void recievedOnClient()
    {
        //The question mark checks if anyone is subscribed to the C_WELCOME action
        NetUtility.C_START_GAME?.Invoke(this);
    }
    public override void recievedOnServer(NetworkConnection connection)
    {
        NetUtility.S_START_GAME?.Invoke(this, connection);
    }
}
