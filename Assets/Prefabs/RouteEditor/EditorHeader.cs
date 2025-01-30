using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorHeader : MonoBehaviour
{
    public TMPro.TMP_Text HeaderText;
    public ToggleGroup StatusIconGroup;

    private RouteSharedData SharedData;
    private Route.RouteStatus CurrentStatus;

    // Start is called before the first frame update
    void Awake()
    {
        SharedData = RouteSharedData.Instance;
    }

    private void Start()
    {
        UpdateHeader();
    }

    // Update is called once per frame
    void Update()
    {
        if (SharedData.CurrentRoute != null &&
            SharedData.CurrentRoute.Status != CurrentStatus) {
            UpdateHeader();
            CurrentStatus = SharedData.CurrentRoute.Status;
        }
    }


    void UpdateHeader() {

        Route route = SharedData.CurrentRoute ?? AppState.CurrentRoute;

        HeaderText.text = "";
        if (route == null)
        {
            return;
        }

        HeaderText.text = route.Name;

        if (StatusIconGroup == null) 
            return;
        
        string status = "";
        if(route.Status == Route.RouteStatus.New){
            status = "Cleaning";
        }  
        else if (route.Status == Route.RouteStatus.DraftPrepared)
        {
            status = "Discussion";
        }
        else if (route.Status == Route.RouteStatus.Training)
        {
            status = "Training";
        }
        else 
        {
            status = route.Status.ToString();
        } 


        // activate the icon based on the status
        foreach (Transform child in StatusIconGroup.transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null && toggle.name.Contains(status))
            {
                toggle.isOn = true;
                break;
            }
        }        
    }
}
