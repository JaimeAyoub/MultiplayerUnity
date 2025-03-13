using UnityEngine;
using TMPro;
using Unity.Netcode;
using System;
using System.Collections;
using UnityEngine.Networking;


public class GameManager : NetworkBehaviour
{
    [Header("Cosas del HUD")] public RectTransform panelHUD;
    public RectTransform panelMainMenu;
    public RectTransform panelLogin;

    public TMP_Text lblcountdown;

    public TMP_Text lblHealth;

    public TMP_Dropdown dropdownNames;

    public TMP_Text playerNameTemplate;


    NetworkVariable<float> countDown = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    //Id nickname jugador

    enum GameState
    {
        lobby, //Esperando que se conecten jugadores

        griefing //Da�ar players
    }

    GameState state;

    public struct NamesData
    {
        public string[] names;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lblcountdown.text = "";
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConncected;
        lblHealth.text = "";


        //Descargar lista de nombres permitidos
        StartCoroutine(TryGetNames());


        playerNameTemplate.enabled = false;
    }

    IEnumerator TryGetNames()
    {
        //Crear la peticion al endpoint
        UnityWebRequest www = UnityWebRequest.Get("http://monsterballgo.com/api/names");
        //Enviar la peticion
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            //parsear la respuesta
            NamesData namesData = JsonUtility.FromJson<NamesData>(www.downloadHandler.text);
            //Mostrar los nombrs en consola
            dropdownNames.options.Clear();
            foreach (var name in namesData.names)
            {
                dropdownNames.options.Add(new TMP_Dropdown.OptionData(name));
            }

            dropdownNames.RefreshShownValue();
            dropdownNames.onValueChanged.AddListener(OnNameChanged);
        }
        else
        {
            Debug.LogError("Error al descargar los nombres: " + www.error);
        }
    }

    void OnNameChanged(int index)
    {
        Debug.Log("Cambio nombre a " + dropdownNames.options[index].text + " con indice" + index);
    }

    private void OnClientConncected(ulong clientId)
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClientsList.Count == 1)
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
                    // state = GameState.griefing;
                    // StartCoroutine(GriefPlayer());
                    countDown.Value = 0;
                }
            }
        }

        if (countDown.Value > 0)
        {
            lblcountdown.text = string.Format("{0:F1}", countDown.Value);
        }
    }


    //Prueba del sistema de vida y da�o
    IEnumerator GriefPlayer()
    {
        while (true)
        {
            foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
            {
                player.PlayerObject.GetComponent<PlayerControler>().TakeDamage(UnityEngine.Random.Range(1, 10));
            }

            yield return new WaitForSeconds(1);
        }
    }

    public void OnPlayerNameChoosed()
    {
        Debug.Log("Nombre: " + dropdownNames.options[dropdownNames.value].text);
        panelMainMenu.gameObject.SetActive(true);
        panelLogin.gameObject.SetActive(false);
    }
}