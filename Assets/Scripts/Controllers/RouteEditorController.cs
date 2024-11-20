using UnityEngine;
using UnityEngine.Events;
using System;

public class RouteEditorController : MonoBehaviour
{
    [Header("Components")]
    public RouteOnboarding RouteOnboardingView;
    public RouteTimeline RouteTimelineView;
    public POIEdit POIEditView;
    public RouteStepChange StatusChange;
    public RouteInfoEdit RouteInfoEditView;


    //public VideoPlayerPrefab VideoManager;
    //public PinListPrefab PinList;
    public MapManager MapView;


    public UnityEvent OnWayDefinitionUploaded;

    private RouteSharedData SharedData;

    // Start is called before the first frame update
    void Start()
    {
        SharedData = RouteSharedData.Instance;
        SharedData.OnDataDownloaded += RouteSharedData_OnDataDownloaded;
        SharedData.OnDataPartiallyDownloaded += RouteSharedData_OnDataPartiallyDownloaded;
        SharedData.OnDataUploaded += SharedData_OnDataUploaded;

        SharedData.DownloadRouteDefinition();        
    }

    /**********************
     *  Public UI events (and utilities)  *
     **********************/

    public void LoadInfo() {

    }

    public void LoadMap()
    {
        HideAllButThisView(MapView.gameObject);
        MapView.LoadMap();
    }

    public void LoadChangeStatusAndSave()
    {
        HideAllButThisView(StatusChange.gameObject);
        StatusChange.LoadRouteStepChange(SharedData.CurrentRoute);
    }

    public void LoadEditRouteInfo()
    {
        HideAllButThisView(RouteInfoEditView.gameObject);
        RouteInfoEditView.LoadRouteInfo(SharedData.CurrentWay, SharedData.CurrentRoute);
    }

    public void LoadPOIEditor(Pathpoint poi, int index)
    {
        SharedData.CurrentPOI = poi;
        SharedData.CurrentPOIIndex = index;

        HideAllButThisView(POIEditView.gameObject);
        POIEditView.LoadView(index);        
    }

    public void LoadTimeline() {
        HideAllButThisView(RouteTimelineView.gameObject);
        RouteTimelineView.LoadView();        
    }

    public void BackToTimeline()
    {
        HideAllButThisView(RouteTimelineView.gameObject);
    }

    public void LoadOnboarding() {
        HideAllButThisView(RouteOnboardingView.gameObject);
        RouteOnboardingView.LoadBusyView();        
    }


    public void FlagChangesToDraft()
    {
        SharedData.CurrentRoute.IsDraftUpdated = true;
        SharedData.CurrentRoute.Insert();

    }

    // private functions

    /// <summary>
    ///  SetupEditorMode is called to set the editor mode based on the status of the route
    /// </summary>
    private void SetupEditorMode() {
        if (SharedData.CurrentRoute.Status == Route.RouteStatus.New)
        {
            SharedData.CurrentEditorMode = RouteSharedData.EditorMode.Cleaning;
        }
        else if (SharedData.CurrentRoute.Status == Route.RouteStatus.DraftPrepared)
        {
            SharedData.CurrentEditorMode = RouteSharedData.EditorMode.Discussion;
        }
        else
        {
            SharedData.CurrentEditorMode = RouteSharedData.EditorMode.ReadOnly;
        }
    }

    private void RouteSharedData_OnDataDownloaded(object sender, EventArgs e)
    {
        RouteOnboardingView.LoadReadyView();
    }

    private void RouteSharedData_OnDataPartiallyDownloaded(object sender, EventArgs e)
    {
        SharedData.LoadRouteFromDatabase();
        SetupEditorMode();        
        LoadOnboarding();
    }

    private void SharedData_OnDataUploaded(object sender, EventArgs e)
    {
        OnWayDefinitionUploaded?.Invoke();
    }

    private void HideAllButThisView(GameObject view) {

        RouteOnboardingView.gameObject.SetActive(RouteOnboardingView.gameObject == view);
        RouteTimelineView.gameObject.SetActive(RouteTimelineView.gameObject == view);

        if (MapView.gameObject != view && MapView.gameObject.activeInHierarchy)
        {
            MapView.DisableMap();
        }
        else if (MapView.gameObject == view)
        {
            MapView.EnableMap();
        }

        if (RouteTimelineView.gameObject != view)
        {
            //RouteTimelineView.CleanupView();
        }

        POIEditView.gameObject.SetActive(POIEditView.gameObject == view);
        if (POIEditView.gameObject != view)
        {
            POIEditView.CleanupView();
        }

        MapView.gameObject.SetActive(MapView.gameObject == view);
        StatusChange.gameObject.SetActive(StatusChange.gameObject == view);

        RouteInfoEditView.gameObject.SetActive(RouteInfoEditView.gameObject == view);
    }

    private void OnDestroy()
    {
        SharedData.OnDataDownloaded -= RouteSharedData_OnDataDownloaded;
        SharedData.OnDataUploaded -= SharedData_OnDataUploaded;
        SharedData.OnDataPartiallyDownloaded -= RouteSharedData_OnDataPartiallyDownloaded;
    }



}