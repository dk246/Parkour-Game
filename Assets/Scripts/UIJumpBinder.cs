using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIJumpBinder : MonoBehaviour
{
    public Button jumpButton;          // assign in Inspector
    public string playerTag = "Player";
    public float retryInterval = 0.5f; // how often to try finding the player

    private Coroutine finderCoroutine;

    void Start()
    {
        if (jumpButton == null)
        {
            Debug.LogError("UIJumpBinder: jumpButton not assigned");
            enabled = false;
            return;
        }
        BindToExistingOrWait();
    }

    void BindToExistingOrWait()
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            Attach(player);
        else
        {
            // start retrying until found
            if (finderCoroutine == null)
                finderCoroutine = StartCoroutine(FindPlayerRoutine());
        }
    }

    IEnumerator FindPlayerRoutine()
    {
        while (true)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                Attach(player);
                finderCoroutine = null;
                yield break;
            }
            yield return new WaitForSeconds(retryInterval);
        }
    }

    void Attach(GameObject player)
    {
        var controller = player.GetComponent<SimpleCharacterController>();
        if (controller == null)
        {
            Debug.LogWarning("UIJumpBinder: found player but no SimpleCharacterController attached");
            return;
        }

        // remove previous listeners so you won't double-call
        jumpButton.onClick.RemoveAllListeners();
        jumpButton.onClick.AddListener(controller.OnJumpButton);
        Debug.Log("UIJumpBinder: jump button bound to player instance " + player.name);
    }

    void OnDestroy()
    {
        if (jumpButton != null)
            jumpButton.onClick.RemoveAllListeners();
    }
}