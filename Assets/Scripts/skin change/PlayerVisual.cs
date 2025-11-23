using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Tooltip("Assign the Renderers (6 meshes) that must all use the same material for a skin.")]
    public Renderer[] meshRenderers;

    [Tooltip("Assign one Material per skin (e.g. 5 materials).")]
    public Material[] skinMaterials;

    [HideInInspector]
    public int currentSkinId = -1;

    void Start()
    {
        Debug.Log($"PlayerVisual initialized on {gameObject.name}");
        Debug.Log($"  Mesh Renderers: {meshRenderers?.Length ?? 0}");
        Debug.Log($"  Skin Materials: {skinMaterials?.Length ?? 0}");

        if (meshRenderers == null || meshRenderers.Length == 0)
        {
            Debug.LogError($"❌ PlayerVisual on {gameObject.name}: No mesh renderers assigned!");
        }

        if (skinMaterials == null || skinMaterials.Length == 0)
        {
            Debug.LogError($"❌ PlayerVisual on {gameObject.name}: No skin materials assigned!");
        }
    }

    public void ApplySkin(int skinId)
    {
        Debug.Log($"🎨 ApplySkin called on {gameObject.name}: skinId={skinId}, currentSkinId={currentSkinId}");

        if (meshRenderers == null || skinMaterials == null)
        {
            Debug.LogError($"❌ PlayerVisual arrays are null on {gameObject.name}");
            return;
        }

        if (skinId < 0 || skinId >= skinMaterials.Length)
        {
            Debug.LogError($"❌ Invalid skinId {skinId} (must be 0-{skinMaterials.Length - 1})");
            return;
        }

        Material mat = skinMaterials[skinId];

        if (mat == null)
        {
            Debug.LogError($"❌ Material at index {skinId} is null!");
            return;
        }

        int appliedCount = 0;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] == null)
            {
                Debug.LogWarning($"⚠️ Mesh renderer at index {i} is null");
                continue;
            }

            meshRenderers[i].material = mat;
            appliedCount++;
        }

        currentSkinId = skinId;
        Debug.Log($"✅ Applied skin {skinId} ({mat.name}) to {appliedCount}/{meshRenderers.Length} renderers on {gameObject.name}");
    }
}