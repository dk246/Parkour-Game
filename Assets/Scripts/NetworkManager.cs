using UnityEngine;
using Colyseus;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    ColyseusClient client;
    ColyseusRoom<MyRoomState> room;

    public GameObject playerPrefab;
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    string localPlayerId = "";

    async void Start()
    {
        client = new ColyseusClient("ws://localhost:2567");

        try
        {
            room = await client.JoinOrCreate<MyRoomState>("my_room");
            Debug.Log("Joined room successfully!");

            localPlayerId = room.SessionId;
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
            serverPlayerIds.Add(key.ToString()); // Cast to string
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
        }

        // Handle local player movement
        if (players.ContainsKey(localPlayerId))
        {
            float h = Input.GetAxis("Horizontal") * Time.deltaTime * 5f;
            float v = Input.GetAxis("Vertical") * Time.deltaTime * 5f;

            GameObject localPlayer = players[localPlayerId];
            localPlayer.transform.Translate(h, 0, v);

            // Send position to server
            var message = new Dictionary<string, object>
            {
                {"x", localPlayer.transform.position.x},
                {"y", localPlayer.transform.position.y},
                {"z", localPlayer.transform.position.z}
            };

            room.Send("updatePosition", message);
        }

        // Update remote player positions
        foreach (var playerId in serverPlayerIds)
        {
            if (playerId != localPlayerId && players.ContainsKey(playerId))
            {
                Player player = room.State.players[playerId];
                players[playerId].transform.position = new Vector3(player.x, player.y, player.z);
            }
        }
    }

    void OnPlayerAdd(string key, Player player)
    {
        Debug.Log("Player added: " + key);

        GameObject playerObj = Instantiate(playerPrefab, new Vector3(player.x, player.y, player.z), Quaternion.identity);
        players[key] = playerObj;

        // Color local player differently
        if (key == localPlayerId)
        {
            playerObj.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            playerObj.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    async void OnApplicationQuit()
    {
        if (room != null)
        {
            await room.Leave();
        }
    }
}