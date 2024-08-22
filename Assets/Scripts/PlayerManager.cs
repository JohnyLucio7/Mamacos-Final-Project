using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float updateInterval = 0.1f;
    private float timeSinceLastUpdate = 0f;
    private string loggedInUsername = "";
    private string lobbyName = "";

    public bool isLocalPlayer = false;

    GameManager gameManager;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (GlobalManager.instance != null)
        {
            loggedInUsername = GlobalManager.instance.loggedInUsername;
            lobbyName = GlobalManager.instance.lobbyName;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");

            transform.position += new Vector3(moveX, moveY, 0) * moveSpeed * Time.deltaTime;

            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                var position = new PlayerPosition
                {
                    type = "updatePosition",
                    username = loggedInUsername,
                    lobbyName = lobbyName,
                    x = transform.position.x,
                    y = transform.position.y
                };

                gameManager.ws.Send(JsonUtility.ToJson(position));
                timeSinceLastUpdate = 0f;

                Debug.Log("Chamou!");
            }
        }
    }
}
