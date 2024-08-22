using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using UnityEngine.SceneManagement;

namespace AP101
{
    public class LoginManager : MonoBehaviour
    {
        [Header("Server Config")]
        public string serverUrl = "http://localhost:8080";
        public string websockectUrl = "ws://localhost:8080";

        [Header("UI References")]
        [HideInInspector] public ChatManager chatManager;
        public InputField usernameField;
        public InputField passwordField;
        public InputField lobbyNameField;
        public InputField joinLobbyField;
        public Text statusText;

        [Header("Panels References")]
        public GameObject loginPanel;
        public GameObject lobbyPanel;
        public GameObject lobbyChatPanel;
        public GameObject hostUI;
        public GameObject playersUI;

        private WebSocket ws;
        private string loggedInUsername = "";
        private string currentLobbyName = "";

        private void Awake()
        {
            chatManager = GetComponent<ChatManager>();
        }

        private void Start()
        {
            ws = new WebSocket(websockectUrl);

            ws.OnMessage += (sender, e) =>
            {
                Debug.Log("Pre-recebido: " + e.Data.ToString());

                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    try
                    {
                        Debug.Log("Mensagem Recebida: " + e.Data.ToString());

                        ChatRequest data = JsonUtility.FromJson<ChatRequest>(e.Data);

                        string message = data.username + ": " + data.message;

                        if (data.type == "lobbyMessage" && data.lobbyName == currentLobbyName && data.username != loggedInUsername)
                        {
                            chatManager.AddMessageToUI(message);
                        }

                        if (data.type == "startGame")
                        {
                            Debug.Log("Starting Game...");
                            SceneManager.LoadScene("002-Gameplay");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Erro ao desserializar a mensagem: " + ex.Message);
                    }
                });
            };

            ws.OnOpen += (sender, e) =>
            {
                Debug.Log("WebSocket conectado!");
            };

            ws.OnClose += (sender, e) =>
            {
                Debug.Log("WebSocket desconectado!");
            };

            ws.OnError += (sender, e) =>
            {
                Debug.LogError("Erro no WebSocket: " + e.Message.ToString());
            };
        }

        public void OnLoginButtonClicked()
        {
            StartCoroutine(Login(usernameField.text, passwordField.text));
        }

        public void OnCreateLobbyClicked()
        {
            StartCoroutine(CreateLobby(lobbyNameField.text));
        }

        public void OnJoinLobbyClicked()
        {
            StartCoroutine(JoinLobby(joinLobbyField.text));
        }

        IEnumerator Login(string username, string password)
        {
            var loginUrl = $"{serverUrl}/login";

            var loginRequest = new LoginRequest { username = username, password = password };

            var json = JsonUtility.ToJson(loginRequest);

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(loginUrl, "POST")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Erro no Login: " + request.error);
                statusText.text = "Erro no Login: " + request.error;
            }
            else
            {
                var response = request.downloadHandler.text;
                Debug.Log($"Resposta do Servidor: {response}");
                statusText.text = $"Resposta do Servidor: {response}";

                var result = JsonUtility.FromJson<LoginResponse>(response);

                if (result.success)
                {
                    ws.Connect();
                    SwitchToLobbyPanel();
                    loggedInUsername = username;
                    GlobalManager.instance.loggedInUsername = loggedInUsername;
                    chatManager.username = username;
                }
            }

            var loginMessage = new LoginMessage { type = "login", username = username };
            var message = JsonUtility.ToJson(loginMessage);
            ws.Send(message);
        }

        IEnumerator CreateLobby(string LobbyName)
        {
            currentLobbyName = LobbyName;

            var createLobbyURL = $"{serverUrl}/createLobby";

            var lobbyRequest = new LobbyRequest { lobbyName = LobbyName, hostUsername = loggedInUsername };

            var json = JsonUtility.ToJson(lobbyRequest);

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(createLobbyURL, "POST")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Erro ao criar Lobby: " + request.error);
            }
            else
            {
                var response = request.downloadHandler.text;
                Debug.Log($"Resposta do Servidor: {response}");

                var result = JsonUtility.FromJson<LobbyResponse>(response);

                if (result.success)
                {
                    Debug.Log("Lobby criado com sucesso!");
                    chatManager.lobbyName = currentLobbyName;
                    GlobalManager.instance.lobbyName = currentLobbyName;
                    SwitchToLobbyChatPanel(true);
                }
            }
        }

        IEnumerator JoinLobby(string LobbyName)
        {
            currentLobbyName = LobbyName;

            var joinLobbyURL = $"{serverUrl}/joinLobby";

            var joinLobbyRequest = new JoinRequest { lobbyName = LobbyName, username = loggedInUsername };

            var json = JsonUtility.ToJson(joinLobbyRequest);

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(joinLobbyURL, "POST")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();


            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Erro entrar no Lobby: " + request.error);
            }
            else
            {
                var response = request.downloadHandler.text;

                Debug.Log($"Resposta do Servidor: {response}");

                var result = JsonUtility.FromJson<LobbyResponse>(response);

                if (result.success)
                {
                    Debug.Log("Lobby conectado com sucesso!");
                    chatManager.lobbyName = LobbyName;
                    GlobalManager.instance.lobbyName = LobbyName;
                    SwitchToLobbyChatPanel(false);
                }
            }

            var joinMessage = new JoinMessage { type = "joinLobby", lobbyName = LobbyName };
            var message = JsonUtility.ToJson(joinMessage);
            ws.Send(message);

        }

        public void StartGame()
        {
            var startGameRequest = new GameRequest { type = "startGame", lobbyName = chatManager.lobbyName };
            var json = JsonUtility.ToJson(startGameRequest);

            ws.Send(json);
        }

        private void OnDestroy()
        {
            if (ws != null && ws.IsAlive)
            {
                ws.Close();
            }
        }

        void SwitchToLobbyPanel()
        {
            loginPanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }

        void SwitchToLobbyChatPanel(bool isHost)
        {
            lobbyPanel.SetActive(false);
            lobbyChatPanel.SetActive(true);

            if (isHost)
            {
                hostUI.SetActive(true);
                playersUI.SetActive(false);
            }
            else
            {
                hostUI.SetActive(false);
                playersUI.SetActive(true);
            }
        }

    }

    [System.Serializable]
    public class LoginResponse
    {
        public bool success;
        public string message;
    }

    [System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class LoginMessage
    {
        public string type;
        public string username;
    }

    public class LobbyResponse
    {
        public bool success;
        public string message;
    }

    [System.Serializable]
    public class LobbyRequest
    {
        public string lobbyName;
        public string hostUsername;
    }

    [System.Serializable]
    public class JoinRequest
    {
        public string lobbyName;
        public string username;
    }

    [System.Serializable]
    public class JoinMessage
    {
        public string type;
        public string lobbyName;
    }

    [System.Serializable]
    public class GameRequest
    {
        public string type;
        public string lobbyName;
    }


}
