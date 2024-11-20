using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserListPrefab : MonoBehaviour
{

    public GameObject ScrollView;

    public GameObject Content;

    public GameObject ItemPrefab;

    public GameObject BlankState;

    public UserItemEvent OnItemSelected;
    public UserItemEvent OnSettingSelected;

    private Transform optionTransform = null;

    // Start is called before the first frame update
    void Start()
    {
        //Clearlist();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Clearlist()
    {
        Transform content = Content.GetComponent<Transform>();

        // Remove all children
        for (int i = 0; i < content.childCount; i++)
        {
            // Get the child game object
            GameObject child = content.GetChild(i).gameObject;

            // Destroy the child game object
            if (!child.name.Contains("Option"))
            {
                Destroy(child);
            }
            else 
            {
                optionTransform = child.transform;
            }
        }
    }

    public void AddItemSimple(User u)
    {
        var neu = Instantiate(ItemPrefab, Content.transform);

        UserItemPrefab item = neu.GetComponent<UserItemPrefab>();
        item.OnSelected = OnItemSelected;
        item.OnSettingSelected = OnSettingSelected;
        item.FillUser(u);

    }

    public void AddItem(User u)
    {
        // Instantiate the new item
        var neu = Instantiate(ItemPrefab);

        UserPicItem item = neu.GetComponent<UserPicItem>();
        item.OnSelected = OnItemSelected;
        item.FillUser(u);

        // Insert the new item before the "Option"
        if (optionTransform != null)
        {
            neu.transform.SetParent(Content.transform, false);
            neu.transform.SetSiblingIndex(optionTransform.GetSiblingIndex());
        }
        else
        {
            // If "Option" doesn't exist, just add it as the last child
            neu.transform.SetParent(Content.transform, false);
        }
    }

}
