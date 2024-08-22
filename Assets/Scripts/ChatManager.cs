using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class ChatManager : MonoBehaviour
{
    public InputField messageInputField;
    public Button sendButton;
    public RectTransform contentTransform;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    private WebSocket ws;
    public string lobbyName = "";
    public string username = "";
    private Queue<string> messageQueue = new Queue<string>();

    void Start()
    {
        ws = new WebSocket("ws://localhost:8080");
        ws.Connect();
        //ws.OnMessage += (sender, e) =>
        //{
        //    Debug.Log("Mensagem Recebida: " + e.Data);
        //    EnqueueMessage(e.Data);
        //};
        sendButton.onClick.AddListener(SendMessage);
        StartCoroutine(ProcessMessages());
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }

    void SendMessage()
    {
        if (string.IsNullOrEmpty(messageInputField.text))
            return;

        var message = messageInputField.text;
        var data = new ChatRequest { type = "lobbyMessage", lobbyName = lobbyName, message = message, username = username };
        var json = JsonUtility.ToJson(data);

        ws.Send(json);

        // Adicione a mensagem à UI
        AddMessageToUI("Você: " + message);
        messageInputField.text = "";
    }

    public void EnqueueMessage(string message)
    {
        lock (messageQueue)
        {
            messageQueue.Enqueue(message);
        }
    }

    IEnumerator ProcessMessages()
    {
        while (true)
        {
            lock (messageQueue)
            {
                while (messageQueue.Count > 0)
                {
                    var message = messageQueue.Dequeue();
                    AddMessageToUI(message);
                }
            }
            yield return null;
        }
    }

    public void AddMessageToUI(string message)
    {
        var newMessage = Instantiate(messagePrefab, contentTransform);
        var messageText = newMessage.GetComponentInChildren<Text>();
        messageText.text = message;

        // Role para o fim
        // Canvas.ForceUpdateCanvases();
        // scrollRect.verticalNormalizedPosition = 0f;
    }
}

[System.Serializable]
public class ChatRequest
{
    public string type;
    public string lobbyName;
    public string message;
    public string username;
}

