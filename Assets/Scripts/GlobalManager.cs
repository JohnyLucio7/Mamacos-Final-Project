using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    public static GlobalManager instance;
    public string loggedInUsername;
    public string lobbyName;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        instance = this;
    }
}
