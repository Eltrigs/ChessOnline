using UnityEngine;
using TMPro;
using System;

public enum CameraAngle
{
    menu = 0,
    whiteTeam = 1,
    blackTeam = 2,
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; }

    [SerializeField] public Server server;
    [SerializeField] public Client client;
    
    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField TxtAddressInput;
    [SerializeField] private GameObject[] cameraAngles;

    public Action<bool> SetLocalGame;

    private void Awake()
    {
        Instance = this;
        registerEvents();
    }

    //Cameras
    public void changeCamera(CameraAngle index)
    {
        for (int i = 0; i < cameraAngles.Length; i++)
        {
            cameraAngles[i].SetActive(false);
        }
        cameraAngles[(int)index].SetActive(true);
    }


    //Buttons
    public void onBtnLocalGame()
    {
        menuAnimator.SetTrigger("TriggerInGameMenu");
        SetLocalGame?.Invoke(true);
        server.init(8007);
        client.init("127.0.0.1", 8007);
        Debug.Log("BtnLocalGame");
    } 
    public void onBtnOnlineGame()
    {
        menuAnimator.SetTrigger("TriggerOnlineMenu");
        Debug.Log("BtnOnlineGame");
    }
    public void onBtnHostGame()
    {
        menuAnimator.SetTrigger("TriggerHostMenu");
        SetLocalGame?.Invoke(false);
        server.init(8007);
        client.init("127.0.0.1", 8007);
        Debug.Log("BtnOnlineGameHost");
    }
    public void onBtnConnect()
    {
        SetLocalGame?.Invoke(false);
        client.init(TxtAddressInput.text, 8007);
        Debug.Log("BtnOnlineGameConnect");
    }
    public void onBtnReturnToMenu()
    {
        menuAnimator.SetTrigger("TriggerStartMenu");
        Debug.Log("BtnOnlineGameToMainMenu");
    }

    public void onBtnCancelHosting()
    {
        menuAnimator.SetTrigger("TriggerOnlineMenu");
        server.shutDown();
        client.shutDown();
    }

    //Events register
    private void registerEvents()
    {
        //Start listening to an action: whether a welcome message is sent
        NetUtility.C_START_GAME += onStartGameClient;
    }

    private void unRegisterEvents()
    {
        NetUtility.C_START_GAME -= onStartGameClient;
    }
    private void onStartGameClient(NetMessage obj)
    {
        menuAnimator.SetTrigger("TriggerInGameMenu");
    }

    internal void onLeaveFromGameMenu()
    {
        changeCamera(CameraAngle.menu);
        menuAnimator.SetTrigger("TriggerStartMenu");
    }
}
