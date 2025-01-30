using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;

public class RouteMonitorController : MonoBehaviour
{
    [Header("Main views")]
    public GameObject IntroMainView;
    public GameObject DashboardView;
    public PIMPublish PublishMainView;
    

    [Header("Sub views")]
    public RouteWalkOverview OverviewView;
    public RouteWalkTimeline TimelineView;
    public RouteWalkOnboarding OnboardingView;
    public RouteWalkMap MapView;
    
    public GameObject LoadingView;

    [Header("Components")]
    public ToggleGroup MenuToggle;
    //public TMPro.TMP_Text HeaderText;

    public UnityEvent OnWayDefinitionUploaded;

    private RouteSharedData SharedData;
    private RouteWalkSharedData WalkSharedData;

    void Awake()
    {
        // make sure all views are active, so they can be preloaded
        DashboardView.SetActive(true);
        TimelineView.gameObject.SetActive(true);
        OverviewView.gameObject.SetActive(true);

        // default shown view
        IntroMainView.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        SharedData = RouteSharedData.Instance;
        SharedData.OnDataDownloaded += RouteSharedData_OnDataDownloaded;
        SharedData.OnDataPartiallyDownloaded += RouteSharedData_OnDataPartiallyDownloaded;

        WalkSharedData = RouteWalkSharedData.Instance;
        WalkSharedData.OnDataDownloaded += WalkSharedData_OnDataDownloaded;

        SharedData.DownloadRouteDefinition();
        //HeaderText.text = AppState.CurrentRoute.Name;

        HideAllButThisView(OnboardingView.gameObject);

    }

    /**********************
     *  Public UI events (and utilities)  *
     **********************/

    public void LoadComponents()
    {
        TimelineView.OnViewLoaded.AddListener(OnTimelineViewLoaded);
        OnboardingView.OnUserConfirmed.AddListener(OnUserConfirmedOnboarding);

        TimelineView.LoadView();
        OverviewView.LoadView();
    }

    public void UploadAdaptationChanges()
    {
        WalkSharedData.UploadLocalPIM();
    }

    private void OnTimelineViewLoaded()
    {
        OnboardingView.LoadReadyView();        
    }

    public void OnUserConfirmedOnboarding()
    {        
        SwitchView(true);
    }

    public void RenderMap()
    {
        HideAllButThisView(MapView.gameObject);
        MapView.LoadMap();
    }

    public void RenderTimeline()
    {
        HideAllButThisView(TimelineView.gameObject);
    }

    public void RenderOverview()
    {
        HideAllButThisView(OverviewView.gameObject);
    }

    public void RenderPublishOrClose()
    {
        if (WalkSharedData.ArePIMChangesToUpload())
        {
            HideAllButThisView(PublishMainView.gameObject);
            PublishMainView.LoadView();
        }
        else
        {
            SafelyCloseView();
        }        
    }

    public void SafelyCloseView()
    {
        MapView.DisableMap();
        SceneSwitcher.LoadRouteExplorer();
    }

    public void SwitchView( bool active)
    {
        // avoid triggering it more than once
        if (!active) return;

        foreach (var toggle in MenuToggle.ActiveToggles())
        {
            if (toggle.name.Contains("Overview"))
            {
                RenderOverview();
            }
            else if (toggle.name.Contains("Timeline"))
            {
                RenderTimeline();
            }
            else
            {
                RenderMap();
            }
        }
    }

    //public void FlagChangesToDraft()
    //{
    //    SharedData.CurrentRoute.IsDraftUpdated = true;
    //    SharedData.CurrentRoute.Insert();

    //}

    // private functions


    private void RouteSharedData_OnDataDownloaded(object sender, EventArgs e)
    {        
        OnboardingView.LoadBusyView();

        if (SharedData.CurrentRoute.LastRouteWalkId == null)
        {
            OnboardingView.LoadNoData();
            return;
        }

        OnboardingView.LoadOnboarding();

        // Trigger to  Dowload the RouteWalk
        WalkSharedData.DownloadRouteWalkDefinition();

        // While waiting, cache information about the route definition in memory
        SharedData.LoadRouteFromDatabase();
    }

    private void RouteSharedData_OnDataPartiallyDownloaded(object sender, EventArgs e)
    {        
        //LoadOnboarding();
    }


    private void WalkSharedData_OnDataDownloaded(object sender, EventArgs e)
    {
        // Load route walk information in memory
        WalkSharedData.LoadRouteWalkFromDatabase();        

        LoadComponents();
    }

    private void HideAllButThisView(GameObject view) {

        if (MapView.gameObject != view && MapView.gameObject.activeInHierarchy){
            MapView.DisableMap();
        }
        else if (MapView.gameObject == view)
        {
            MapView.EnableMap();
        }

        OnboardingView.gameObject.SetActive(OnboardingView.gameObject == view);
        OverviewView.gameObject.SetActive(OverviewView.gameObject == view);
        TimelineView.gameObject.SetActive(TimelineView.gameObject == view);
        PublishMainView.gameObject.SetActive(PublishMainView.gameObject == view);
        MapView.gameObject.SetActive(MapView.gameObject == view);
        LoadingView.SetActive(LoadingView == view);
        
    }

    private void OnDestroy()
    {
        SharedData.OnDataDownloaded -= RouteSharedData_OnDataDownloaded;
        SharedData.OnDataPartiallyDownloaded -= RouteSharedData_OnDataPartiallyDownloaded;

        WalkSharedData.OnDataDownloaded -= WalkSharedData_OnDataDownloaded;

        TimelineView.OnViewLoaded.RemoveListener(OnTimelineViewLoaded);
        OnboardingView.OnUserConfirmed.RemoveListener(OnUserConfirmedOnboarding);

    }



}