using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class GameManager : MonoBehaviour
{

    public float updateThreshold = 0.1f; // Mínima distância para enviar atualização
    public GameObject playerPrefab;

    public WebSocket ws;
    private Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();
    private Vector3 lastSentPosition;
    private string loggedInUsername;

    // Start is called before the first frame update
    void Start()
    {
        #region Initializations

        if (GlobalManager.instance != null)
        {
            loggedInUsername = GlobalManager.instance.loggedInUsername;
        }

        var player = Instantiate(playerPrefab);
        var manager = player.GetComponent<PlayerManager>();
        manager.isLocalPlayer = true;

        #endregion

        #region WS

        ws = new WebSocket("ws://localhost:8080");
        ws.Connect();

        ws.OnMessage += (sender, e) =>
        {
            var data = JsonUtility.FromJson<PlayerPosition>(e.Data);

            Debug.Log(data.y);

            if (data.type == "updatePosition" && data.username != loggedInUsername)
            {
                if (!otherPlayers.ContainsKey(data.username))
                {
                    var newPlayer = Instantiate(playerPrefab);
                    newPlayer.GetComponent<PlayerManager>().isLocalPlayer = false;
                    otherPlayers[data.username] = newPlayer;
                }
                otherPlayers[data.username].transform.position = new Vector3(data.x, data.y, 0);
            }
        };

        lastSentPosition = transform.position;

        #endregion
    }
}

[System.Serializable]
public class PlayerPosition
{
    public string type;
    public string username;
    public string lobbyName;
    public float x;
    public float y;
}
