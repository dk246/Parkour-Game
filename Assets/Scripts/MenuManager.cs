using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button quickPlayButton;
    public TextMeshProUGUI statusText;

    [Header("Settings")]
    public string gameSceneName = "Game";

    void Start()
    {
        // Add button listeners
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        quickPlayButton.onClick.AddListener(OnQuickPlayClicked);

        // Set default room name
        roomNameInput.text = "Room_" + Random.Range(1000, 9999);

        UpdateStatus("Ready to connect", Color.white);
    }

    void OnCreateRoomClicked()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            UpdateStatus("Please enter a room name!", Color.red);
            return;
        }

        UpdateStatus("Creating room: " + roomName + "...", Color.yellow);

        // Store room name for game scene
        PlayerPrefs.SetString("RoomId", roomName);
        PlayerPrefs.SetString("JoinMode", "create");
        PlayerPrefs.Save();

        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }

    void OnJoinRoomClicked()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            UpdateStatus("Please enter a room name!", Color.red);
            return;
        }

        UpdateStatus("Joining room: " + roomName + "...", Color.yellow);

        // Store room name for game scene
        PlayerPrefs.SetString("RoomId", roomName);
        PlayerPrefs.SetString("JoinMode", "join");
        PlayerPrefs.Save();

        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }

    void OnQuickPlayClicked()
    {
        UpdateStatus("Finding available room...", Color.yellow);

        // Clear room ID for quick play
        PlayerPrefs.SetString("RoomId", "");
        PlayerPrefs.SetString("JoinMode", "quickplay");
        PlayerPrefs.Save();

        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }

    void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log("Status: " + message);
    }
}