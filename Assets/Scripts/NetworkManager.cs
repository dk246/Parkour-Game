using UnityEngine;
using Colyseus;
using System.Collections.Generic;
using TMPro.Examples;

public class NetworkManager : MonoBehaviour
{
    ColyseusClient client;
    ColyseusRoom<MyRoomState> room;

    public GameObject playerPrefab;

    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();

    string localPlayerId = "";
    private float sendRate = 0.05f;
    private float nextSendTime = 0f;
    public float interpolationSpeed = 15f;

    async void Start()
    {
        Application.runInBackground = true;

        client = new ColyseusClient("ws://localhost:2567");

        try
        {
            // Check if we're coming from menu with room info
            string roomId = PlayerPrefs.GetString("RoomId", "");

            if (!string.IsNullOrEmpty(roomId))
            {
                // Try to join the specific room by ID
                try
                {
                    room = await client.JoinById<MyRoomState>(roomId);
                    Debug.Log("Joined room: " + roomId);
                }
                catch
                {
                    // If join by ID fails, just join or create new room
                    room = await client.JoinOrCreate<MyRoomState>("my_room");
                    Debug.Log("Room not found, joined/created new room");
                }
            }
            else
            {
                // No room info, just join or create
                room = await client.JoinOrCreate<MyRoomState>("my_room");
                Debug.Log("Joined/Created room from game scene");
            }

            localPlayerId = room.SessionId;
            Debug.Log("Connected! Session ID: " + localPlayerId);
            Debug.Log("Room ID: " + room.Id);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error connecting: " + e.Message);
        }
    }

    void Update()
    {
        if (room == null || room.State == null) return;

        // Get all player IDs from the server state
        List<string> serverPlayerIds = new List<string>();
        foreach (var key in room.State.players.Keys)
        {
            serverPlayerIds.Add(key.ToString());
        }

        // Add new players
        foreach (var playerId in serverPlayerIds)
        {
            if (!players.ContainsKey(playerId))
            {
                Player player = room.State.players[playerId];
                OnPlayerAdd(playerId, player);
            }
        }

        // Remove disconnected players
        List<string> toRemove = new List<string>();
        foreach (var playerId in players.Keys)
        {
            if (!serverPlayerIds.Contains(playerId))
            {
                toRemove.Add(playerId);
            }
        }

        foreach (var playerId in toRemove)
        {
            Debug.Log("Player removed: " + playerId);
            Destroy(players[playerId]);
            players.Remove(playerId);
            targetPositions.Remove(playerId);
        }

        // Send local player position to server at regular intervals
        if (players.ContainsKey(localPlayerId) && Time.time >= nextSendTime)
        {
            GameObject localPlayer = players[localPlayerId];

            var message = new Dictionary<string, object>
            {
                {"x", localPlayer.transform.position.x},
                {"y", localPlayer.transform.position.y},
                {"z", localPlayer.transform.position.z}
            };

            room.Send("updatePosition", message);
            nextSendTime = Time.time + sendRate;
        }

        // Update remote player positions and animations
        foreach (var playerId in serverPlayerIds)
        {
            if (playerId != localPlayerId && players.ContainsKey(playerId))
            {
                Player player = room.State.players[playerId];
                Vector3 serverPosition = new Vector3(player.x, player.y, player.z);

                GameObject remotePlayer = players[playerId];
                Vector3 currentPosition = remotePlayer.transform.position;

                // Calculate movement for animation
                float horizontalDistance = Vector3.Distance(
                    new Vector3(currentPosition.x, 0, currentPosition.z),
                    new Vector3(serverPosition.x, 0, serverPosition.z)
                );

                bool isMoving = horizontalDistance > 0.02f;
                bool isJumping = Mathf.Abs(serverPosition.y - currentPosition.y) > 0.1f;

                // Update animator
                Animator animator = remotePlayer.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool("isWalk", isMoving);
                    animator.SetBool("isJump", isJumping || serverPosition.y > 0.6f);
                }

                // Smoothly move to target position
                remotePlayer.transform.position = Vector3.Lerp(
                    currentPosition,
                    serverPosition,
                    Time.deltaTime * interpolationSpeed
                );

                // Rotate towards movement direction
                if (isMoving)
                {
                    Vector3 direction = (serverPosition - currentPosition).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        remotePlayer.transform.rotation = Quaternion.Slerp(
                            remotePlayer.transform.rotation,
                            targetRotation,
                            Time.deltaTime * interpolationSpeed
                        );
                    }
                }
            }
        }
    }

    void OnPlayerAdd(string key, Player player)
    {
        Debug.Log("Player added: " + key);

        // Calculate spawn position based on player count
        Vector3 spawnPosition = GetSpawnPosition();

        // Override the player position from server with our spawn position
        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        players[key] = playerObj;

        if (key == localPlayerId)
        {
            // Local player - keep all components enabled
            Debug.Log("Local player spawned at: " + spawnPosition);

            // TAG THE LOCAL PLAYER
            playerObj.tag = "Player";

            // Tell camera to follow this player
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null)
            {
                cam.target = playerObj.transform;
            }

            // Send initial spawn position to server
            var message = new Dictionary<string, object>
        {
            {"x", spawnPosition.x},
            {"y", spawnPosition.y},
            {"z", spawnPosition.z}
        };
            room.Send("updatePosition", message);

            // Color local player green
            Renderer[] renderers = playerObj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material.color = Color.green;
            }
        }
        else
        {
            // Remote player - disable movement script but keep animator and rigidbody
            Debug.Log("Remote player spawned at: " + spawnPosition);

            // REMOVE TAG FROM REMOTE PLAYERS
            playerObj.tag = "Untagged";

            // Disable the SimpleCharacterController
            SimpleCharacterController controller = playerObj.GetComponent<SimpleCharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Keep Animator enabled
            Animator animator = playerObj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
            }

            // Make rigidbody kinematic for remote players (no physics)
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Color remote player red
            Renderer[] renderers = playerObj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material.color = Color.red;
            }
        }
    }

    // Method to calculate spawn positions
    Vector3 GetSpawnPosition()
    {
        // Option 1: Circle formation
        int playerCount = players.Count;
        float angle = playerCount * 45f; // 45 degrees apart
        float radius = 5f;

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        return new Vector3(x, 1f, z); // Y=1 to spawn above ground

        // Option 2: Grid formation (uncomment to use)
        // int playerCount = players.Count;
        // int gridSize = 3; // 3x3 grid
        // float spacing = 3f;
        // 
        // int row = playerCount / gridSize;
        // int col = playerCount % gridSize;
        // 
        // float x = (col - gridSize / 2) * spacing;
        // float z = (row - gridSize / 2) * spacing;
        // 
        // return new Vector3(x, 1f, z);

        // Option 3: Random position (uncomment to use)
        // float randomX = Random.Range(-10f, 10f);
        // float randomZ = Random.Range(-10f, 10f);
        // return new Vector3(randomX, 1f, randomZ);
    }

    public bool IsLocalPlayer(GameObject playerObj)
    {
        foreach (var kvp in players)
        {
            if (kvp.Value == playerObj && kvp.Key == localPlayerId)
            {
                return true;
            }
        }
        return false;
    }

    async void OnApplicationQuit()
    {
        if (room != null)
        {
            await room.Leave();
        }
    }
}