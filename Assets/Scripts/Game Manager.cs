using UnityEngine;
using TMPro;
using Unity.Netcode;
using System;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    [Header("Cosas del HUD")]
    public RectTransform panelHUD;

    public TMP_Text lblcountdown;

    public TMP_Text lblHealth;

    NetworkVariable<float> countDown = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    enum GameState
    {
         lobby, //Esperando que se conecten jugadores

        griefing //Dañar players
    }
    GameState state;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lblcountdown.text = "";
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConncected;
        lblHealth.text = "";

       
    }

    private void OnClientConncected(ulong clientId)
    {
       if(IsServer && NetworkManager.Singleton.ConnectedClientsList.Count == 1)
        {
            countDown.Value = 15;
        }
    }

    public override void OnNetworkSpawn()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            //Checar contador
            if (state == GameState.lobby)
            {
                if (countDown.Value > 0)
                {
                    countDown.Value -= Time.deltaTime;
                }
                else
                {
                    state = GameState.griefing;
                    StartCoroutine(GriefPlayer());
                    countDown.Value = 0;
                }
            }
        }

        if(countDown.Value > 0)
        {
            lblcountdown.text = string.Format("{0:F1}", countDown.Value);
        }
    }
       


    //Prueba del sistema de vida y daño
    IEnumerator GriefPlayer()
    {
        while(true)
        {
            foreach(var player in NetworkManager.Singleton.ConnectedClientsList)
            {
                player.PlayerObject.GetComponent<PlayerControler>().TakeDamage(UnityEngine.Random.Range(1,10));
            }
            yield return new WaitForSeconds(1);
        }
    }
}
