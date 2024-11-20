using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RouteTimeline: MonoBehaviour
{

    [Header("UI Elements")]
    public TMPro.TMP_Text TitleText;
    public POITimeline POITimelineView;
    public GameObject NoDataView;
    public GameObject SaveButton;

    private RouteSharedData SharedData;



    // Start is called before the first frame update
    void Awake()
    {
        SharedData = RouteSharedData.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadView()
    {
        gameObject.SetActive(true);
        SaveButton.SetActive(true);

        TitleText.text = SharedData.CurrentRoute.Name;
        if (SharedData.CurrentPOI == null)
        {
            LoadPathpointList();
        }
        else
        {
            UpdateTimelineChanges();
        }
        

        SharedData.CurrentPOI = null;
        SharedData.CurrentPOIIndex = -1;
    }

    private void UpdateTimelineChanges()
    {
        POITimelineView.RenderPOIChanges(SharedData.CurrentPOIIndex);
    }

    // <summary>
    // Populates the Route Onboarding view based on the status of the Route
    // </summary>
    private void LoadPathpointList()
    {
        POITimelineView.Clearlist();
        int index = 0;
        foreach (var item in SharedData.POIList)
        {
            try {
                // We load photos                
                item.Photos = PathpointPhoto.GetPathpointPhotoListByPOI(item.Id);
                //item.Photos
            }
            catch (Exception e)
            {
                Debug.Log($"Index: {index} Id: {item.Id} POIType: {item.POIType} --> Error loading photos");
                index++;
                continue;
            }

            // Add item to list
            if (index == 0) {
                //TODO: Remove hack
                if (item.POIType != Pathpoint.POIsType.WayStart) { 
                    item.POIType = Pathpoint.POIsType.WayStart;
                    item.InsertDirty();
                }
                POITimelineView.AddStart(item, SharedData.CurrentWay);
                
            } else if (index == SharedData.POIList.Count -1) {
                //TODO: Remove hack
                if (item.POIType != Pathpoint.POIsType.WayDestination)
                {
                    item.POIType = Pathpoint.POIsType.WayDestination;
                    item.InsertDirty();
                }
                POITimelineView.AddDestination(item, SharedData.CurrentWay);
                
            } else {                
                POITimelineView.AddPOI(item);
            }

            Debug.Log($"Index: {index} Id: {item.Id} POIType: {item.POIType}");

            index++;

            

        }

        // No data to render
        if (index == 0) {
            LoadNoData();
        }

        Debug.Log("Number of POIs: "+ SharedData.POIList.Count);
    }

    private void LoadNoData() {
        POITimelineView.gameObject.SetActive(false);
        NoDataView.SetActive(true);
        //SaveButton.SetActive(false);
    }

    public void CleanupView()
    {
        POITimelineView.CleanupView();
    }

}
