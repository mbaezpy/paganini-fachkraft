using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Globalization;


[System.Serializable]
public class RouteItemEvent : UnityEvent<Way,Route>
{
}

public class RouteItemPrefab : MonoBehaviour
{

    [Header("Cell Settings")]
    public RouteItemCellPrefab TitleCell;
    public RouteItemCellPrefab StartCell;
    public RouteItemCellPrefab DestinatitonCell;
    public RouteItemCellPrefab RecordingDateCell;
    public RouteItemCellPrefab DateStartCell;
    public RouteItemCellPrefab DateLastWalkCell;
    public RouteItemCellPrefab NumWalksCell;
    public RouteItemCellPrefab ProgressCell;
    public RouteItemCellPrefab EmptyStatsCell;

    [Header("Row Settings")]
    public GameObject NewFlag;
    public GameObject DraftFlag;
    public Image RawPanel;
    public Button RouteButton;
    public Button RouteEditButton;
    public Color NotTrainingColor;

    [Header("Events")]
    public RouteItemEvent OnSelected;
    public RouteItemEvent OnRouteEdit;

    private Way WayItem;
    private Route RouteItem;


    // Start is called before the first frame update
    void Start()
    {
        RouteButton.onClick.AddListener(itemSelected);
        RouteEditButton.onClick.AddListener(itemEdit);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    ///  Fill recording and training data
    /// </summary>
    /// <param name="way"></param>
    /// <param name="route"></param>
    public void FillWayRoute(Way way, Route route)
    {
        // LandmarkIcon.LandmarkType startIcon = Enum.Parse<LandmarkIcon.LandmarkType>(way.StartType);
        // LandmarkIcon.LandmarkType destIcon = Enum.Parse<LandmarkIcon.LandmarkType>(way.DestinationType);
        // TitleCell.FillCell(route.Name, route.Status);
        // StartCell.FillCell(way.Start, startIcon);
        // DestinatitonCell.FillCell(way.Destination, destIcon);    

        // Fill basic recording data
        FillRecordingData(way, route);

        // Enable relevant cells based on route status
        RecordingDateCell.gameObject.SetActive(route.Status != Route.RouteStatus.Training);
        EmptyStatsCell.gameObject.SetActive(route.Status != Route.RouteStatus.Training);

        DateStartCell.gameObject.SetActive(route.Status == Route.RouteStatus.Training);
        DateLastWalkCell.gameObject.SetActive(route.Status == Route.RouteStatus.Training);
        NumWalksCell.gameObject.SetActive(route.Status == Route.RouteStatus.Training);
        ProgressCell.gameObject.SetActive(route.Status == Route.RouteStatus.Training);

        if (route.Status == Route.RouteStatus.Training)
        {
            
            if (route.RouteWalkCount > 0){
                NumWalksCell.FillCell(route.RouteWalkCount.ToString());
                ProgressCell.SetProgress(route.LastRouteWalk.WalkCompletionPercentage);

                DateStartCell.FillCell(DateUtils.ConvertUTCToLocalString(route.FirstRouteWalkDate, "dd MMMM yyyy", CultureInfo.CurrentCulture));
                DateLastWalkCell.FillCell(DateUtils.ConvertUTCToLocalString(route.LastRouteWalk.StartDateTime, "dd MMMM yyyy", CultureInfo.CurrentCulture));                

                //completion of the last walk
                DateLastWalkCell.SetCompletedStatus(route.LastRouteWalk.WalkCompletion == RouteWalk.RouteWalkCompletion.Completed);
            }
            else 
            {
                DateStartCell.FillCell("-");
                DateLastWalkCell.FillCell("-");
                DateLastWalkCell.SetCompletedStatus(false, renderEmpty: true);

                NumWalksCell.FillCell("0");
                ProgressCell.SetProgress(0);                
            }
        }
        else
        {
            //RecordingDateCell.FillCell(DateUtils.ConvertUTCToLocalString(route.Date, "HH:mm 'Uhr' - dd.MM.yyyy", CultureInfo.CurrentCulture));
            RawPanel.color = NotTrainingColor;
        }


        // NewFlag.SetActive(!route.FromAPI);
        // DraftFlag.SetActive(route.IsDraftUpdated == true);

        // WayItem = way;
        // RouteItem = route;
    }


    /// <summary>
    /// Fill the recording data (ERW), based on the way and route
    /// </summary>
    /// <param name="way"></param>
    /// <param name="route"></param>
    public void FillRecordingData(Way way, Route route)
    {
        LandmarkIcon.LandmarkType startIcon = Enum.Parse<LandmarkIcon.LandmarkType>(way.StartType);
        LandmarkIcon.LandmarkType destIcon = Enum.Parse<LandmarkIcon.LandmarkType>(way.DestinationType);
        TitleCell.FillCell(route.Name, route.Status);
        StartCell.FillCell(way.Start, startIcon);
        DestinatitonCell.FillCell(way.Destination, destIcon); 
        RecordingDateCell.FillCell(DateUtils.ConvertUTCToLocalString(route.Date, "HH:mm 'Uhr' - dd.MM.yyyy", CultureInfo.CurrentCulture));   

        NewFlag.SetActive(!route.FromAPI);
        DraftFlag.SetActive(route.IsDraftUpdated == true);

        WayItem = way;
        RouteItem = route;
    }

    private void itemSelected()
    {
        if (OnSelected != null)
        {
            OnSelected.Invoke(WayItem, RouteItem);
        }

        Debug.Log("Item RouteItem " + RouteItem.Id);
    }

    private void itemEdit()
    {
        if (OnRouteEdit != null)
        {
            OnRouteEdit.Invoke(WayItem, RouteItem);
        }

        Debug.Log("Edit Item RouteItem " + RouteItem.Id);
    }    

    void OnDestroy()
    {
        RouteButton.onClick.RemoveAllListeners();
        RouteEditButton.onClick.RemoveAllListeners();
    }
}
