using System;
using System.Collections.Generic;
using UnityEngine;

public class MainContextualHelp : MonoBehaviour
{
    [System.Serializable]
    public class ContextualHelpItem
    {
        public GameObject View; // The view that triggers the help
        public GameObject HelpPanel; // The corresponding help panel
        public string HelpKey; // The key that matches the key returned by the view (or any other condition)
    }

    [Header("Help Mappings")]
    public List<ContextualHelpItem> HelpMappings; // List of view-to-help mappings

    private Dictionary<GameObject, ContextualHelpItem> _helpDictionary;

    void Start()
    {
        // Initialize the dictionary for quick lookup
        _helpDictionary = new Dictionary<GameObject, ContextualHelpItem>();
        foreach (var item in HelpMappings)
        {
            _helpDictionary[item.View] = item;
        }
    }

    public void CloseHelp()
    {
        gameObject.SetActive(false);

        // Handle special behavior for any view implementing IMapSnapshotHandler (if applicable)
        foreach (var item in HelpMappings)
        {
            if (item.View.activeSelf && item.View.TryGetComponent<IMapSnapshotHandler>(out var mapHandler))
            {
                mapHandler.ToggleMapAsSnapshot(false);
            }
        }
    }

    /// <summary>
    /// Renders the contextual help based on the active view
    /// </summary>
    public void RenderContextualHelp()
    {
        bool showPanel = false;

        foreach (var item in HelpMappings)
        {
            if (!showPanel && item.View.activeInHierarchy)
            {
                showPanel = true;
                // Check if the view implements IServeContextualHelp
                if (item.View.TryGetComponent<IServeContextualHelp>(out var contextualHelpProvider))
                {
                    string helpKey = contextualHelpProvider.GetContextualHelpKey();

                    // If the key from the view matches the HelpKey in the help item, show the panel
                    showPanel = helpKey == item.HelpKey;
                }
                
                if (showPanel)
                {
                    item.HelpPanel.SetActive(true);
                    // Handle special behavior for views implementing IMapSnapshotHandler (if applicable)
                    if (item.View.TryGetComponent<IMapSnapshotHandler>(out var mapHandler))
                    {
                        mapHandler.ToggleMapAsSnapshot(true);
                    }                   
                }             
            }
            else
            {
                item.HelpPanel.SetActive(false); // Hide if it doesn't implement the interface
            }
        }

        gameObject.SetActive(true);
    }
}
