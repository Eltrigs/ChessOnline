using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Server : MonoBehaviour
{
    #region Singleton implementation
    public static Server Instance { set; get; }

    private void Awake()
    {
        Instance = this;
    }
    #endregion

    public NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    private bool isActive = false;
    //This is to make sure a message is sent every 20 seconds
    private const float keepAliveTickRate = 20.0f;
    private float lastKeepAlive;

    public Action connectionDropped;

    //Methods
    public void init(ushort port)
    {
        driver = NetworkDriver.Create();
        
        //This variable is here so anyone can connect to the server
        NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = port;

        if (driver.Bind(endpoint) != 0)
        {
            Debug.Log("Unable to bind on port " + endpoint.Port);
            return;
        }
        else
        {
            driver.Listen();
            Debug.Log("Currently listening on port " + endpoint.Port);
        }

        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
        isActive = true;
    }
    public void shutDown()
    {
        if(isActive)
        {
            driver.Dispose();
            connections.Dispose();
            isActive = false;
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

        keepAlive();

        //Empty up queue of messages coming in
        driver.ScheduleUpdate().Complete();

        //If there is anyone that left connection, but we still have reference
        cleanupConnections();

        //Is there someone trying to connect?
        acceptNewConnections();

        //Is a connection sending a message and do we have to reply?
        updateMessagePump();
    }

    private void keepAlive()
    {
        if(Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            broadcast(new NetKeepAlive());
        }
    }

    private void updateMessagePump()
    {
        DataStreamReader stream;
        for (int i = 0; i < connections.Length; i++)
        {
            NetworkEvent.Type command;
            while ((command = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if(command == NetworkEvent.Type.Data)
                {
                    NetUtility.onData(stream, connections[i], this);
                }
                else if (command == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from the server");
                    connections[i] = default(NetworkConnection);
                    connectionDropped?.Invoke();
                    shutDown(); //We shutdown because this is only a 2-person game
                }
            }
        }
    }

    private void acceptNewConnections()
    {
        //Accept new connections
        NetworkConnection newConnection;
        while ((newConnection = driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(newConnection);
        }
    }

    private void cleanupConnections()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if(!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
    }

    //Server specific
    public void sendToClient(NetworkConnection connection, NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.serialize(ref writer);
        driver.EndSend(writer);
    }
    public void broadcast(NetMessage msg)
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if (connections[i].IsCreated)
            {
                //Debug.Log($"Sending {msg.Code} to: {connections[i].InternalId}");
                sendToClient(connections[i], msg);
            }
        }
    }
}
