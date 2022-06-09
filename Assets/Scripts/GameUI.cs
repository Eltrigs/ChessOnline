using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; }

    [SerializeField] public Server server;
    [SerializeField] public Client client;
    
    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField TxtAddressInput;

    private void Awake()
    {
        Instance = this;
    }

    public void onBtnLocalGame()
    {
        menuAnimator.SetTrigger("TriggerInGameMenu");
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
        server.init(8007);
        client.init("127.0.0.1", 8007);
        Debug.Log("BtnOnlineGameHost");
    }
    public void onBtnConnect()
    {
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

}
