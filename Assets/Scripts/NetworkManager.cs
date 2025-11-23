using UnityEngine;
using Colyseus;
using Colyseus.Schema;
using System.Collections.Generic;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    ColyseusClient client;
    ColyseusRoom<MyRoomState> room;

    public GameObject playerPrefab;
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    string myPlayerId = "";

    [Tooltip("How many skins are available (must match PlayerVisual.skinMaterials length).")]
    public int availableSkins = 5;

    public float smoothSpeed = 15f;
    private float updateRate = 0.05f;
    private float nextUpdateTime = 0f;

    async void Start()
    {
        Application.runInBackground = true;
        client = new ColyseusClient("wss://serverofcolyseus-production-c543.up.railway.app");

        int chosenSkin = PlayerPrefs.GetInt("SkinId", -1);
        if (chosenSkin < 0 || chosenSkin >= availableSkins)
        {
            chosenSkin = Random.Range(0, Mathf.Max(1, availableSkins));
            PlayerPrefs.SetInt("SkinId", chosenSkin);
            PlayerPrefs.Save();
        }

        try
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Player");
            string roomName = PlayerPrefs.GetString("RoomId", "");
            string joinMode = PlayerPrefs.GetString("JoinMode", "quickplay");

            Debug.Log($"=== CONNECTION INFO ===");
            Debug.Log($"Player Name: {myName}");
            Debug.Log($"Room Name: {roomName}");
            Debug.Log($"Join Mode: {joinMode}");
            Debug.Log($"Chosen skin (before join): {chosenSkin}");

            var options = new Dictionary<string, object>
            {
                { "name", myName },
                { "customRoomName", string.IsNullOrEmpty(roomName) ? "default" : roomName },
                { "skinId", chosenSkin }
            };

            room = await client.JoinOrCreate<MyRoomState>("my_room", options);
            myPlayerId = room.SessionId;

            Debug.Log($"✅ SUCCESS!");
            Debug.Log($"   Session ID: {myPlayerId}");
            Debug.Log($"   Room ID: {room.Id}");
            Debug.Log($"======================");

            // Immediately spawn any players that are already present in the room state
            SpawnNewPlayers(GetAllPlayerIds());

            // Keep the existing broadcast listener (still useful), but it is supplemental.
            room.OnMessage<Dictionary<string, object>>("skinChanged", (message) =>
            {
                try
                {
                    string playerId = message["playerId"].ToString();
                    float skinIdFloat = System.Convert.ToSingle(message["skinId"]);
                    int skinId = Mathf.RoundToInt(skinIdFloat);

                    Debug.Log($"📡 Received skinChanged broadcast: Player={playerId}, Skin={skinId}");

                    if (players.ContainsKey(playerId))
                    {
                        GameObject playerObj = players[playerId];
                        PlayerVisual pv = playerObj.GetComponent<PlayerVisual>();

                        if (pv != null)
                        {
                            pv.ApplySkin(skinId);
                        }
                        else
                        {
                            Debug.LogError($"❌ PlayerVisual not found on {playerId}!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Player {playerId} not yet spawned (broadcast).");
                        // SpawnNewPlayers / next Update will create the player and the per-frame reconcile will apply the skin
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error processing skinChanged: {e.Message}");
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ CONNECTION FAILED!");
            Debug.LogError($"   Error: {e.Message}");
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

    List<string> GetAllPlayerIds()
    {
        List<string> ids = new List<string>();
        foreach (var key in room.State.players.Keys)
        {
            ids.Add(key.ToString());
        }
        return ids;
    }

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

    void RemoveDisconnectedPlayers(List<string> serverPlayerIds)
    {
        List<string> playersToRemove = new List<string>();

        foreach (string playerId in new List<string>(players.Keys))
        {
            if (!serverPlayerIds.Contains(playerId))
            {
                playersToRemove.Add(playerId);
            }
        }

        foreach (string playerId in playersToRemove)
        {
            Debug.Log("Player disconnected: " + playerId);
            Destroy(players[playerId]);
            players.Remove(playerId);
        }
    }

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

            // ---- NEW: authoritative skin reconciliation ----
            var pv = remotePlayer.GetComponent<PlayerVisual>();
            if (pv != null)
            {
                int serverSkin = Mathf.Clamp(Mathf.RoundToInt(serverData.skinId), 0, Mathf.Max(0, availableSkins - 1));
                if (pv.currentSkinId != serverSkin)
                {
                    Debug.Log($"🔁 Reconciled skin for {playerId}: server={serverSkin} local={pv.currentSkinId}");
                    pv.ApplySkin(serverSkin);
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ PlayerVisual missing on remote player {playerId}");
            }
        }
    }

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

    void UpdatePlayerAnimation(GameObject player, bool isMoving, bool isJumping)
    {
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isWalk", isMoving);
            animator.SetBool("isJump", isJumping);
        }
    }

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

    void CreatePlayerCharacter(string playerId, Player playerData)
    {
        // Ensure we don't double-spawn if called multiple times
        if (players.ContainsKey(playerId))
            return;

        Debug.Log($">>> Spawning player: {playerId}");
        Debug.Log($"    Name: {playerData.name}");
        Debug.Log($"    Skin from server: {playerData.skinId}");

        Vector3 spawnPosition = CalculateSpawnPosition();

        GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        newPlayer.name = $"Player_{playerId}";
        players[playerId] = newPlayer;

        int skinToApply = Mathf.RoundToInt(playerData.skinId);
        skinToApply = Mathf.Clamp(skinToApply, 0, availableSkins - 1);

        Debug.Log($"🎨 Will apply skin {skinToApply} to {playerId}");
        StartCoroutine(ApplySkinWithDelay(playerId, skinToApply, 0.1f));

        if (playerId == myPlayerId)
        {
            SetupLocalPlayer(newPlayer, spawnPosition);
        }
        else
        {
            SetupRemotePlayer(newPlayer, playerData.name);
        }
    }

    System.Collections.IEnumerator ApplySkinWithDelay(string playerId, int skinId, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (players.ContainsKey(playerId))
        {
            GameObject player = players[playerId];
            PlayerVisual pv = player.GetComponent<PlayerVisual>();

            if (pv != null)
            {
                pv.ApplySkin(skinId);
                Debug.Log($"✅ Applied skin {skinId} to {playerId} (with delay)");
            }
            else
            {
                Debug.LogError($"❌ PlayerVisual component not found on {playerId}!");
            }
        }
    }

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

    void SetPlayerName(GameObject player, string playerName, bool isLocal)
    {
        TextMeshPro nameText = player.GetComponentInChildren<TextMeshPro>();

        if (nameText != null)
        {
            nameText.text = playerName;

            if (isLocal)
            {
                nameText.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                nameText.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
        }
    }

    void SetupRemotePlayer(GameObject player, string playerName)
    {
        Debug.Log("Setting up remote player: " + playerName);
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

    public void ChangeMySkin(int skinId)
    {
        Debug.Log($"📤 ChangeMySkin called: skinId={skinId}");

        if (room == null)
        {
            Debug.LogWarning("Cannot change skin: not connected to room");
            return;
        }

        if (!players.ContainsKey(myPlayerId))
        {
            Debug.LogWarning("ChangeMySkin: my player object not spawned yet.");
            PlayerPrefs.SetInt("SkinId", skinId);
            PlayerPrefs.Save();
            return;
        }

        int clamped = Mathf.Clamp(skinId, 0, Mathf.Max(0, availableSkins - 1));
        PlayerPrefs.SetInt("SkinId", clamped);
        PlayerPrefs.Save();

        GameObject myPlayer = players[myPlayerId];

        PlayerVisual pv = myPlayer.GetComponent<PlayerVisual>();
        if (pv != null)
        {
            pv.ApplySkin(clamped);
            Debug.Log($"✅ Applied skin {clamped} locally");
        }
        else
        {
            Debug.LogError("❌ PlayerVisual component not found on local player!");
        }

        var msg = new Dictionary<string, object> { { "skinId", clamped } };
        room.Send("changeSkin", msg);

        Debug.Log($"📤 Sent changeSkin message to server: {clamped}");
    }

    async void OnApplicationQuit()
    {
        if (room != null)
        {
            await room.Leave();
            Debug.Log("Disconnected from room");
        }
    }
}