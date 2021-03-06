using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Colyseus;
using GameDevWare.Serialization;

public class LoginGUI : MonoBehaviour
{
    public static string hostName = "localhost";
    public static int port = 2657;
    public static string userName = "User";
    public static string roomName = "chat";

    public GameObject loginPanel;
    public InputField serverInput;
    public InputField userInput;
    public InputField roomInput;

    public Text log;

    public static List<string> users = null;

    public static Client colyseus;
    public static Room chatRoom;


    public static string logText;

    void Start()
    {
        Application.runInBackground = true;

        userName = "User" + Random.Range(1, 1000000);

        serverInput.text = hostName;
        userInput.text = userName;
        roomInput.text = roomName;

        loginPanel.SetActive(true);
    }

    void Update()
    {
        log.text = logText;
    }

    void Log(string message)
    {
        logText += message + "\n";
        Debug.Log(message);
    }

    public void OnLoginClick()
    {
        hostName = serverInput.text;
        userName = userInput.text;
        roomName = roomInput.text;
        StartCoroutine(Login());
    }

    IEnumerator Login()
    {
        String uri = "ws://" + hostName + ":" + port;

        Log("Connecting to " + hostName + ":" + port + " ...");

        colyseus = new Client(uri);
        colyseus.OnOpen += OnOpenHandler;

        yield return StartCoroutine(colyseus.Connect());

        chatRoom = colyseus.Join(roomName);
        chatRoom.OnJoin += OnRoomJoined;
        // chatRoom.OnUpdate += OnUpdateHandler;
        chatRoom.OnLeave += OnRoomLeave;

        chatRoom.state.Listen("messsages", "add", this.OnAddMessages);
        chatRoom.state.Listen("messsages", "replace", this.OnAddMessages);

        chatRoom.state.Listen(this.OnChangeFallback);

        while (true)
        {
            colyseus.Recv();

            // string reply = colyseus.RecvString();
            if (colyseus.error != null)
            {
                Log("Error: " + colyseus.error);
                break;
            }
            yield return 0;
        }

        OnApplicationQuit();
    }

    private void OnAddMessages(string[] path, object value)
    {
        Debug.Log("OnAddMessages | " + ChatUtils.PathToString(path) + " | " + ChatUtils.ValueToString(value));
        List<object> messages = (List<object>)value;
        foreach (string m in messages)
        {
            Log(m);
        }
    }


    void OnOpenHandler(object sender, EventArgs e)
    {
        Log("Connected to server. Client id: " + colyseus.id);
    }

    void OnRoomJoined(object sender, EventArgs e)
    {
        Log("Joined room successfully.");
        loginPanel.SetActive(false);

        chatRoom.state.RemoveAllListeners();
        GetComponent<ChatGUI>().StartChat(chatRoom);
        chatRoom.state.Listen(this.OnChangeFallback);
    }

    private void OnRoomLeave(object sender, EventArgs e)
    {
        Log("Leave room.");
        loginPanel.SetActive(true);
    }

    void OnChangeFallback(string[] path, string operation, object value)
    {
        Log("OnChangeFallback | " + operation + " | " + ChatUtils.PathToString(path) + " | " + ChatUtils.ValueToString(value));
    }

    void OnUpdateHandler(object sender, RoomUpdateEventArgs e)
    {
        //Log(e.state);
    }

    void OnApplicationQuit()
    {
        // Ensure the connection with server is closed immediatelly
        if (colyseus != null)
            colyseus.Close();
    }


}