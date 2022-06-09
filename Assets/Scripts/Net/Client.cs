using System;
using Unity.Networking.Transport;
using UnityEngine;

public class Client : MonoBehaviour
{
    #region Singleton implementation
    public static Client Instance { set; get; }

    private void Awake()
    {
        Instance = this;
    }
    #endregion

    public NetworkDriver driver;
    private NetworkConnection connection;

    private bool isActive = false;
    //This is to make sure a message is sent every 20 seconds
    private const float keepAliveTickRate = 20.0f;
    private float lastKeepAlive;

    public Action connectionDropped;

    //Methods
    public void init(string ip, ushort port)
    {
        driver = NetworkDriver.Create();

        //This variable is here so anyone can connect to the server
        NetworkEndPoint endpoint = NetworkEndPoint.Parse(ip, port);

        Debug.Log("Attempting to connect to Server on " + endpoint.Address);
        connection = driver.Connect(endpoint);

        isActive = true;

        //Is going to keep track of the keepAlive messages
        //registerToEvent();
    }
    public void shutDown()
    {
        if (isActive)
        {
            //unRegisterToEvent();
            driver.Dispose();
            isActive = false;
            connection = default(NetworkConnection);
        }
    }
    public void OnDestroy()
    {
        shutDown();
    }

    public void Update()
    {
        if (!isActive)
        {
            return;
        }

        //Empty up queue of messages coming in
        driver.ScheduleUpdate().Complete();
        checkAlive();

        //Is a connection sending a message and do we have to reply?
        updateMessagePump();
    }

    private void updateMessagePump()
    {
        DataStreamReader stream;
        NetworkEvent.Type command;
        while ((command = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (command == NetworkEvent.Type.Connect)
            {
                //sendToServer(new NetWelcome());
                Debug.Log("We're connected");
            }
            else if (command == NetworkEvent.Type.Data)
            {
                NetUtility.onData(stream, connection);
            }
            else if (command == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client disconnected from the server");
                connection = default(NetworkConnection);
                connectionDropped?.Invoke();
                shutDown(); //We shutdown because this is only a 2-person game
            }
        }

    }
    private void checkAlive()
    {
        if(!connection.IsCreated && isActive)
        {
            Debug.Log("Lost connection to server");
            connectionDropped?.Invoke();
            shutDown();
        }
    }

    public void sendToServer(NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.serialize(ref writer);
        driver.EndSend(writer);
    }

    //Event parsing
    private void registerToEvent()
    {
        NetUtility.C_KEEP_ALIVE += onKeepAlive;
    }
    private void unRegisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE -= onKeepAlive;
    }
    private void onKeepAlive(NetMessage nm)
    {
        // Send it back, to keep both sides alive
        sendToServer(nm);
    }
}
