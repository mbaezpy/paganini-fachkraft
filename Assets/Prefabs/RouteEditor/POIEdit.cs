using UnityEngine;

/// <summary>
/// POIEdit is a component that manages the UI elements for editing a POI
/// </summary>
public class POIEdit : MonoBehaviour, IServeContextualHelp
{
    [Header("UI Elements")]
    public PinDetailsEdit PinEdit;
    public PhotoGallery Gallery;
    public VideoGallery Video;
    public PhotoSlideShow SlideShow;
    public PinMetaEdit PinMeta;


    private RouteSharedData SharedData;   


    // Start is called before the first frame update
    void Awake()
    {
        SharedData = RouteSharedData.Instance;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// LoadView is called to load the POIEdit view
    /// </summary>
    /// <param name="index"> The index of the POI in the timeline </param>
    public void LoadView(int index)
    {
        gameObject.SetActive(true);        

        //PinEdit
        LoadPinEdit(index);

        // Gallery
        LoadGallery();
    }


    /// <summary>
    /// LoadGallery is called to load the Photo gallery ovierview
    /// </summary>
    public void LoadGallery()
    {
        ShowView(Gallery.gameObject);
        Gallery.EditMode = SharedData.CurrentEditorMode;
        Gallery.Clearlist();
        Gallery.LoadPhotos(SharedData.CurrentPOI.Photos, SharedData.CurrentPOI);
        PinEdit.EnableSwitchToGallery(false);
    }

    /// <summary>
    /// LoadSlideShow is called to load the Photo slideshow fullscreen
    /// </summary>
    /// <param name="photo"></param>
    /// <param name="startIndex"></param>
    public void LoadSlideShow(PathpointPhoto photo, int startIndex)
    {
        ShowView(SlideShow.gameObject);
        SlideShow.EditMode = SharedData.CurrentEditorMode;
        SlideShow.LoadSlideShow(SharedData.CurrentPOI.Photos, startIndex);
        PinEdit.EnableSwitchToGallery(false);
    }

    /// <summary>
    /// LoadPinMeta is called to load the metadata of the POI
    /// </summary>
    public void LoadPinMeta()
    {
        //HideAllButThisView(PinMeta.gameObject);

        PinMeta.gameObject.SetActive(true);
        PinMeta.PopulateMetadata(SharedData.CurrentPOI, SharedData.CurrentWay);
    }

    /// <summary>
    /// LoadVideo is called to load the video of the POI
    /// </summary>
    public void LoadVideo()
    {
        ShowView(Video.gameObject);


        Video.LoadVideo(SharedData.POIList[0]);

        Pathpoint pointNext = SharedData.CurrentPOI;
        if (SharedData.CurrentPOI.POIType != Pathpoint.POIsType.WayDestination &&
            SharedData.CurrentPOIIndex + 1 < SharedData.POIList.Count) {
            pointNext = SharedData.POIList[SharedData.CurrentPOIIndex + 1];
        }

        Video.LimitPlaybackTimeframe(SharedData.CurrentPOI, pointNext);
        PinEdit.EnableSwitchToGallery(true);
    }

    /// <summary>
    /// GetContextualHelpKey is called to get the key for the contextual help (IServeContextualHelp)
    /// </summary>
    public string GetContextualHelpKey(){
        return SharedData?.CurrentEditorMode.ToString();
    }

    private void LoadPinEdit(int index) {
        PinEdit.EditMode = SharedData.CurrentEditorMode;
        //PinEdit.DisableChangesOnPOI(false);
        PinEdit.PopulateMetadata(SharedData.CurrentPOI, SharedData.CurrentWay, index);
    }

    /// <summary>
    /// Displays a view and hides all other views
    /// </summary>
    /// <param name="view"></param>
    private void ShowView(GameObject view)
    {
        SlideShow.gameObject.SetActive(SlideShow.gameObject == view);
        if (SlideShow.gameObject != view)
        {
            SlideShow.CleanupView();
        }

        Gallery.gameObject.SetActive(Gallery.gameObject == view);
        if (Gallery.gameObject != view)
        {
            Gallery.CleanupView();
        }

        Video.gameObject.SetActive(Video.gameObject == view);
        if (Video.gameObject != view)
        {
            Video.CleanupView();
        }

        PinMeta.gameObject.SetActive(PinMeta.gameObject == view);

    }

    /// <summary>
    /// CleanupView is called to hide all views
    /// </summary>
    public void CleanupView()
    {
        PinEdit.CleanupView();
        SlideShow.CleanupView();
        Gallery.CleanupView();
        Video.CleanupView();
    }

}
