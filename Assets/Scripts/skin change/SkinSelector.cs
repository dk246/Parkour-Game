using UnityEngine;

// UI helper: attach to a UI object. Each button should call SelectSkin(int id) with the proper id (0..N-1).
public class SkinSelector : MonoBehaviour
{
    public GameObject panel; // skin selection panel
    public NetworkManager networkManager; // assign in inspector

    public void TogglePanel()
    {
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
    }

    // Called by UI button OnClick (pass skinId)
    public void SelectSkin(int skinId)
    {
        if (networkManager == null)
        {
            Debug.LogWarning("SkinSelector: networkManager not assigned.");
            return;
        }

        // Persist choice locally
        PlayerPrefs.SetInt("SkinId", skinId);
        PlayerPrefs.Save();

        networkManager.ChangeMySkin(skinId);

        if (panel != null) panel.SetActive(false);
    }
}