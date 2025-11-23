using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Very simple controller to switch between a list of UI GameObjects (buttons or any UI).
/// Attach to an empty GameObject, assign the 5 UI objects in order,
/// and hook your Next/Previous UI Buttons to NextItem() and PrevItem().
/// </summary>
public class SimpleUIButtonSwitcher : MonoBehaviour
{
    [Tooltip("Assign the UI GameObjects (your 5 buttons). Only the current one will be active.")]
    public GameObject[] items;

    [Tooltip("If true, Next from last wraps to first and Prev from first wraps to last.")]
    public bool loop = true;

    [Tooltip("Index to start on (0-based).")]
    public int startIndex = 0;

    int currentIndex = 0;

    void Awake()
    {
        if (items == null || items.Length == 0) return;
        startIndex = Mathf.Clamp(startIndex, 0, items.Length - 1);
        currentIndex = startIndex;
        Refresh();
    }

    // Call from Next button OnClick
    public void NextItem()
    {
        if (items == null || items.Length == 0) return;
        int next = currentIndex + 1;
        if (next >= items.Length)
        {
            if (loop) next = 0;
            else next = items.Length - 1;
        }
        SetIndex(next);
    }

    // Call from Previous button OnClick
    public void PrevItem()
    {
        if (items == null || items.Length == 0) return;
        int prev = currentIndex - 1;
        if (prev < 0)
        {
            if (loop) prev = items.Length - 1;
            else prev = 0;
        }
        SetIndex(prev);
    }

    // jump to specific index
    public void SetIndex(int index)
    {
        if (items == null || items.Length == 0) return;
        index = Mathf.Clamp(index, 0, items.Length - 1);
        if (index == currentIndex) return;
        currentIndex = index;
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].SetActive(i == currentIndex);
        }
    }

    // optional: get current index
    public int GetCurrentIndex() => currentIndex;
}