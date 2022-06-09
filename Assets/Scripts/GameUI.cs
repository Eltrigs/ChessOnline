using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; }

    [SerializeField] private Animator menuAnimator;

    private void Awake()
    {
        Instance = this;
    }

    public void onBtnLocalGame()
    {
        menuAnimator.SetTrigger("TriggerInGameMenu");
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
        Debug.Log("BtnOnlineGameHost");
    }
    public void onBtnConnect()
    {
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
    }

}
