using UnityEngine;
using Colyseus;
using System.Collections.Generic;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    //COLYSEUS CONNECTION
    ColyseusClient client;                          
    ColyseusRoom<MyRoomState> room;                

    // PLAYER MANAGEMENT
    public GameObject playerPrefab;                  
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();  
    string myPlayerId = "";                          

    //MOVEMENT SETTINGS 
    public float smoothSpeed = 15f;                 
    private float updateRate = 0.05f;                
    private float nextUpdateTime = 0f;


    async void Start()
    {
        Application.runInBackground = true;
        client = new ColyseusClient("wss://serverofcolyseus-production-c543.up.railway.app");

        try
        {
            // ✅ GET PLAYER NAME AND ROOM NAME
            string myName = PlayerPrefs.GetString("PlayerName", "Player");
            string roomName = PlayerPrefs.GetString("RoomId", "");
            string joinMode = PlayerPrefs.GetString("JoinMode", "quickplay");

            Debug.Log($"=== CONNECTION INFO ===");
            Debug.Log($"Player Name: {myName}");
            Debug.Log($"Room Name: {roomName}");
            Debug.Log($"Join Mode: {joinMode}");

            // ✅ CREATE OPTIONS WITH CUSTOM ROOM NAME
            var options = new Dictionary<string, object>
        {
            { "name", myName },
            { "customRoomName", string.IsNullOrEmpty(roomName) ? "default" : roomName }
        };

            // ✅ ALWAYS USE JOINORCREATE - IT HANDLES BOTH CREATE AND JOIN
            room = await client.JoinOrCreate<MyRoomState>("my_room", options);

            myPlayerId = room.SessionId;

            Debug.Log($"✅ SUCCESS!");
            Debug.Log($"   Session ID: {myPlayerId}");
            Debug.Log($"   Room ID: {room.Id}");
            Debug.Log($"   Room Name: {roomName}");
            Debug.Log($"======================");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ CONNECTION FAILED!");
            Debug.LogError($"   Error: {e.Message}");
            Debug.LogError($"   Stack: {e.StackTrace}");
        }
    }

    void Update()
    {
       
        if (room == null || room.State == null) return;

        List<string> serverPlayerIds = GetAllPlayerIds();

        SpawnNewPlayers(serverPlayerIds);

        RemoveDisconnectedPlayers(serverPlayerIds);

        SendMyPosition();

        UpdateRemotePlayers(serverPlayerIds);
    }

    //GET ALL PLAYER IDs FROM SERVER
    List<string> GetAllPlayerIds()
    {
        List<string> ids = new List<string>();

        foreach (var key in room.State.players.Keys)
        {
            ids.Add(key.ToString());
        }

        return ids;
    }

    // SPAWN NEW PLAYERS
    void SpawnNewPlayers(List<string> serverPlayerIds)
    {
        foreach (string playerId in serverPlayerIds)
        {
            
            if (!players.ContainsKey(playerId))
            {
                Player playerData = room.State.players[playerId];

                CreatePlayerCharacter(playerId, playerData);
            }
        }
    }

    //REMOVE DISCONNECTED PLAYERS
    void RemoveDisconnectedPlayers(List<string> serverPlayerIds)
    {
        
        List<string> playersToRemove = new List<string>();

        foreach (string playerId in players.Keys)
        {
            if (!serverPlayerIds.Contains(playerId))
            {
                playersToRemove.Add(playerId);
            }
        }

        // Remove each disconnected player
        foreach (string playerId in playersToRemove)
        {
            Debug.Log("Player disconnected: " + playerId);
            Destroy(players[playerId]);
            players.Remove(playerId);
        }
    }

    // SEND MY POSITION TO SERVER
    void SendMyPosition()
    {        
        if (Time.time < nextUpdateTime) return;

        if (!players.ContainsKey(myPlayerId)) return;

        GameObject myPlayer = players[myPlayerId];
        Vector3 myPosition = myPlayer.transform.position;

        var positionMessage = new Dictionary<string, object>
        {
            {"x", myPosition.x},
            {"y", myPosition.y},
            {"z", myPosition.z}
        };

        room.Send("updatePosition", positionMessage);

        nextUpdateTime = Time.time + updateRate;
    }

    // UPDATE OTHER PLAYERS' POSITIONS
    void UpdateRemotePlayers(List<string> serverPlayerIds)
    {
        foreach (string playerId in serverPlayerIds)
        {  
            if (playerId == myPlayerId) continue;
 
            if (!players.ContainsKey(playerId)) continue;

            Player serverData = room.State.players[playerId];
            Vector3 targetPosition = new Vector3(serverData.x, serverData.y, serverData.z);

            GameObject remotePlayer = players[playerId];
            Vector3 currentPosition = remotePlayer.transform.position;

            bool isMoving = IsPlayerMoving(currentPosition, targetPosition);
            bool isJumping = IsPlayerJumping(currentPosition, targetPosition);

            UpdatePlayerAnimation(remotePlayer, isMoving, isJumping);

            MovePlayerSmoothly(remotePlayer, currentPosition, targetPosition, isMoving);
        }
    }

    // CHECK IF PLAYER IS MOVING
    bool IsPlayerMoving(Vector3 current, Vector3 target)
    {

        float horizontalDistance = Vector3.Distance(
            new Vector3(current.x, 0, current.z),
            new Vector3(target.x, 0, target.z)
        );

        return horizontalDistance > 0.02f;  
    }

    bool IsPlayerJumping(Vector3 current, Vector3 target)
    {

        float verticalDistance = Mathf.Abs(target.y - current.y);
        bool isInAir = target.y > 0.6f;  

        return verticalDistance > 0.1f || isInAir;
    }

    // UPDATE PLAYER ANIMATION
    void UpdatePlayerAnimation(GameObject player, bool isMoving, bool isJumping)
    {
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isWalk", isMoving);
            animator.SetBool("isJump", isJumping);
        }
    }

    // MOVE PLAYER SMOOTHLY
    void MovePlayerSmoothly(GameObject player, Vector3 from, Vector3 to, bool isMoving)
    {

        player.transform.position = Vector3.Lerp(from, to, Time.deltaTime * smoothSpeed);

        if (isMoving)
        {
            Vector3 direction = (to - from).normalized;
            direction.y = 0;  

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                player.transform.rotation = Quaternion.Slerp(
                    player.transform.rotation,
                    targetRotation,
                    Time.deltaTime * smoothSpeed
                );
            }
        }
    }

    // CREATE PLAYER CHARACTER
    void CreatePlayerCharacter(string playerId, Player playerData)
    {
        Debug.Log("Spawning player: " + playerId);
        Debug.Log("Player name from server: " + playerData.name);  // ✅ ADD THIS
        Vector3 spawnPosition = CalculateSpawnPosition();

        GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        players[playerId] = newPlayer;

        if (playerId == myPlayerId)
        {
            SetupLocalPlayer(newPlayer, spawnPosition);
        }
        else
        {
            Debug.Log("Setting remote player name to: " + playerData.name);  // ✅ ADD THIS
            SetupRemotePlayer(newPlayer, playerData.name);
        }
    }

    // SETUP MY LOCAL PLAYER
    void SetupLocalPlayer(GameObject player, Vector3 position)
    {
        Debug.Log("This is MY player!");

        player.tag = "Player";

        CameraController camera = Camera.main.GetComponent<CameraController>();
        if (camera != null)
        {
            camera.target = player.transform;
        }

        var message = new Dictionary<string, object>
        {
            {"x", position.x},
            {"y", position.y},
            {"z", position.z}
        };
        room.Send("updatePosition", message);

        string myName = PlayerPrefs.GetString("PlayerName", "Player");
        SetPlayerName(player, myName, true); 
    }

    // ===== HELPER: SET PLAYER NAME =====
    void SetPlayerName(GameObject player, string playerName, bool isLocal)
    {
        // Find the 3D text in the player
        TextMeshPro nameText = player.GetComponentInChildren<TextMeshPro>();

        if (nameText != null)
        {
            nameText.text = playerName;

            // Set rotation based on player type
            if (isLocal)
            {
                // Local player: rotation.y = 0
                nameText.transform.localRotation = Quaternion.Euler(0, 0, 0);
                Debug.Log("Set LOCAL name: " + playerName + " (rotation.y = 0)");
            }
            else
            {
                // Remote player: rotation.y = 180
                nameText.transform.localRotation = Quaternion.Euler(0, 180, 0);
                Debug.Log("Set REMOTE name: " + playerName + " (rotation.y = 180)");
            }
        }
        else
        {
            Debug.LogWarning("No TextMeshPro found on player!");
        }
    }

    //SETUP OTHER PLAYERS
    // ✨ ADD playerName PARAMETER ✨
    void SetupRemotePlayer(GameObject player, string playerName)
    {
        Debug.Log("This is another player: " + playerName);
        player.tag = "Untagged";

        SimpleCharacterController controller = player.GetComponent<SimpleCharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        

        // ✨ USE THE REAL NAME FROM SERVER ✨
        SetPlayerName(player, playerName, false);
    }




    Vector3 CalculateSpawnPosition()
    {
       
        int playerCount = players.Count;
        float angle = playerCount * 45f;  
        float radius = 5f;                

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        return new Vector3(x, 1f, z);  
    }

    // DISCONNECT WHEN QUITTING
    async void OnApplicationQuit()
    {
        if (room != null)
        {
            await room.Leave();
            Debug.Log("Disconnected from room");
        }
    }
}