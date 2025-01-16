using System.Collections.Generic;
using System.Linq;
using LocationTools;
using NinevaStudios.GoogleMaps;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapManager : MonoBehaviour, IMapSnapshotHandler
{
    public GameObject MapContainer;
    public CanvasScaler ReferenceCanvasScaler;
    public Image MapSnapshot;
    public GameObject LoadingAnimation;

    public MapToolbar Toolbar;

    [Header("Route elements")]
    public Texture2D DefaultMarkerIcon;
    public Texture2D POILandmarkMarkerIcon;
    public Texture2D POIReassuranceMarkerIcon;
    public Color RouteColor;
    public Color SelectedColor;
    public Color DisableColor;
    
    public UnityEvent<Pathpoint> OnPathpointSelected;

    //PRIVATE
    private static Texture2D ColoredMarkerIcon;
    private static Texture2D ScaledPOILandmarkMarkerIcon;
    private static Texture2D ScaledPOIReassuranceMarkerIcon;


    private static Texture2D SelecteddMarkerIcon;
    private static Texture2D SelectedPOILandmarkMarkerIcon;
    private static Texture2D SelectedPOIReassuranceMarkerIcon;

    private static Texture2D DeletedPOILandmarkMarkerIcon;
    private static Texture2D DeletedPOIReassuranceMarkerIcon;    


    private Texture2D currentSnapshot;
    private Sprite currentSprite;

    private GoogleMapsView Map;
    private List<Pathpoint> PathpointList;
    private Dictionary<string, Pathpoint> MarkerPathpoint;
    private Dictionary<string, Marker> PathpointMarker;
    private RouteSharedData SharedData;

    private Marker SelectedMarker;
    private Pathpoint SelectedPathpoint;
    private Marker DestinationMarker;
    private Pathpoint DestinationPathpoint;

    public bool EnableDestinationMarker {get; set;}

    // Start is called before the first frame update
    void Awake()
    {
        SharedData = RouteSharedData.Instance;        
    }

    void Start()
    {
        Toolbar.Hide();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /**********************
     *  Public UI events (and utilities)  *
     **********************/

    /// <summary>
    /// Load the map component
    /// </summary>
    /// <param name="pathpoints">Pathpoints to render in the map</param> 
    public void LoadMap()
    {
        SharedData.CurrentPOI = null;

        SelectedMarker = null;
        DestinationMarker = null;

        PathpointList = SharedData.PathpointList;
        if (ColoredMarkerIcon == null)
        {
            ColoredMarkerIcon = ChangeIconColor(DefaultMarkerIcon, RouteColor);
            ScaledPOILandmarkMarkerIcon = ResizeTexture(POILandmarkMarkerIcon);
            ScaledPOIReassuranceMarkerIcon = ResizeTexture(POIReassuranceMarkerIcon);

            SelecteddMarkerIcon = ChangeIconColor(DefaultMarkerIcon, SelectedColor);
            SelectedPOILandmarkMarkerIcon = ChangeIconColor(ScaledPOILandmarkMarkerIcon, SelectedColor);
            SelectedPOIReassuranceMarkerIcon = ChangeIconColor(ScaledPOIReassuranceMarkerIcon, SelectedColor);   

            DeletedPOILandmarkMarkerIcon = ChangeIconColor(ScaledPOILandmarkMarkerIcon, DisableColor);
            DeletedPOIReassuranceMarkerIcon = ChangeIconColor(ScaledPOIReassuranceMarkerIcon, DisableColor);                        
        }

        if (Map == null)
        {
            LoadMap(19);
        }
    }

    /// <summary>
    /// Disable Map
    /// </summary>
    public void DisableMap()
    {
        if (Map != null)
        {
            //Map.TakeSnapshot(OnSnapshotReady);
            Map.IsVisible = false;
        }
        MapContainer.SetActive(false);
    }    

    /// <summary>
    /// Enable Map component
    /// </summary>
    public void EnableMap()
    {
        if (Map != null)
        {
            Map.IsVisible = true;
        }
        MapContainer.SetActive(true);
    }

    /// <summary>
    /// Remove markers from the map
    /// </summary>
    public void ClearMapMarkers()
    {
        Map.Clear();
    }

    public void ToggleMapAsSnapshot(bool asSnapshot)
    {
        if (Map == null) return;
        
        if (asSnapshot)
        {            
            Map.TakeSnapshot(OnSnapshotReady);
            Map.IsVisible = false;
            MapSnapshot.sprite = currentSprite;
        }
        else
        {
            LoadingAnimation.SetActive(true);
            Map.IsVisible = true;
            MapSnapshot.sprite = null;
        }
            
        
    }


    public void DisplayMarkers(List<Pathpoint> pathpoints)
    {
        int i = 0;
        int poiIndex = 0;
        MarkerPathpoint = new Dictionary<string, Pathpoint>();
        PathpointMarker = new Dictionary<string, Marker>();

        foreach (var pathpoint in pathpoints)
        {
            Debug.Log("DisplayMarkers: " + pathpoint.Id);
            var icon = GetPathpointIcon(pathpoint);

            string title = "GPS Punkt " + i;
            if (pathpoint.POIType == Pathpoint.POIsType.WayStart){
                title = "Startpunkt";
            }
            else if (pathpoint.POIType == Pathpoint.POIsType.WayDestination){
                title = "Zielpunkt";
            }
            else if (pathpoint.POIType != Pathpoint.POIsType.Point){
                poiIndex++;
                title = "Pin " + poiIndex;
            }

            var mo = new MarkerOptions()
                    .Position(new LatLng(pathpoint.Latitude, pathpoint.Longitude))
                    .Icon(NewCustomDescriptor(icon))
                    .Title(title);
                    //.Snippet($"Lat: {pathpoint.Latitude} Lon: {pathpoint.Longitude}");
            
            var marker = Map.AddMarker(mo);     
            MarkerPathpoint.Add(marker.Id, pathpoint);
            PathpointMarker.Add(pathpoint.Id.ToString(), marker);

            i++;
        }
    }

    /// <summary>
    ///  Update the marker icon in the map
    /// </summary>
    /// <param name="pin"></param>
    public void UpdateMarker(Pathpoint pin){        
        var marker = PathpointMarker[pin.Id.ToString()];
        Debug.Log("UpdateMarker: " + pin.Id + " " + pin.POIType + " marker: "+ marker.Id);
        marker.SetIcon(NewCustomDescriptor(GetPathpointIcon(pin)));
    }

    /// <summary>
    /// Swap the markers in the map
    /// </summary>
    /// <param name="pin1">Pin origin</param>
    /// <param name="pin2">Pin destination</param>
    public void SwapMarkers(Pathpoint pin1, Pathpoint pin2){

        Debug.Log("SwapMarkers: " + pin1.Id + " to " + pin2.Id);

        var marker1 = PathpointMarker[pin1.Id.ToString()];
        var marker2 = PathpointMarker[pin2.Id.ToString()];

        // Set proper titles
        marker2.Title = marker1.Title;  // marker turned into a POI gets the title of the POI
        // old marker gets the title of a point, with the proper index
        var poinIndex = PathpointList.FindIndex(p => p.Id == pin2.Id); 
        marker1.Title = "GPS Punkt " + poinIndex;

        PathpointMarker[pin1.Id.ToString()] = marker2;
        PathpointMarker[pin2.Id.ToString()] = marker1;

        MarkerPathpoint[marker1.Id.ToString()] = pin2;
        MarkerPathpoint[marker2.Id.ToString()] = pin1;

        //UpdateMarker(pin1);
        //UpdateMarker(pin2);

        // not clear whether the markers are updated correctly
        // SelectedMarker = marker1;
        // DestinationMarker = marker2;        
        DestinationPathpoint = pin1;
        SelectedPathpoint = pin2;

        marker1.SetIcon(NewCustomDescriptor(GetPathpointIcon(pin2)));
        marker2.SetIcon(NewCustomDescriptor(GetPathpointIcon(pin1)));

        OnMarkerClickHandler(marker2);

    }

    /// <summary>
    /// Refreshes the currently selected marker as focused in the map
    /// </summary>
    public void RefreshSelectedMarkersAsFocused(){
        if (SelectedMarker != null){
            RenderMarkerSelected(SelectedMarker, SelectedPathpoint);
        }
    }

    /// <summary>  
    /// Obtains the icon to display in the map based on the Pathpoint type
    /// </summary>
    /// <param name="pathpoint">Pathpoint to render</param>
    private Texture2D GetPathpointIcon(Pathpoint pathpoint, bool selected = false)
    {
        Texture2D icon = null;
        if (pathpoint.POIType == Pathpoint.POIsType.Point)
        {
            icon = selected? SelecteddMarkerIcon : ColoredMarkerIcon;
        }
        else if (pathpoint.POIType == Pathpoint.POIsType.Landmark)
        {                        
            icon = (pathpoint.CleaningFeedback == Pathpoint.POIFeedback.No || pathpoint.RelevanceFeedback == Pathpoint.POIFeedback.No)? 
                    DeletedPOILandmarkMarkerIcon : ScaledPOILandmarkMarkerIcon;
            icon = selected? SelectedPOILandmarkMarkerIcon : icon;                    
        }
        else
        {            
            icon = (pathpoint.CleaningFeedback == Pathpoint.POIFeedback.No || pathpoint.RelevanceFeedback == Pathpoint.POIFeedback.No)? 
                    DeletedPOIReassuranceMarkerIcon : ScaledPOIReassuranceMarkerIcon;
            icon = selected? SelectedPOIReassuranceMarkerIcon: icon;                    
        }

        // Debug.Log("GetPathpointIcon: " + pathpoint.Id + " selected: " + selected + " icon: " + 
        // icon + " type: " + pathpoint.POIType + " feedback: " + pathpoint.CleaningFeedback + " relevance: " + pathpoint.RelevanceFeedback);
        return icon;
    }

    private void OnSnapshotReady(Texture2D snapshot)
    {
        LoadingAnimation.SetActive(false);

        // Clean up the previous snapshot and sprite, if they exist
        if (!Map.IsVisible) { 
            CleanupSnapshots();            

            // Assign the new snapshot and create a new Sprite
            currentSnapshot = snapshot;
            currentSprite = Sprite.Create(snapshot, new Rect(0, 0, snapshot.width, snapshot.height), new Vector2(0.5f, 0.5f));
            MapSnapshot.sprite = currentSprite;

            Debug.Log("Snapshot taken!");
        }
        else
        {
            DestroyImmediate(snapshot);
        }
    }


    private void LoadMap(int zoom)
    {
        // initialize Map
        var options = new GoogleMapsOptions();        

        if (PathpointList != null && PathpointList.Count > 0)
        {
            // start point
            Pathpoint startPoint = PathpointList[0];

            // setup camera
            var cameraPosition = new CameraPosition(
                new LatLng(startPoint.Latitude, startPoint.Longitude), zoom, 0, 0);
            options = options.Camera(cameraPosition);

        }

        options.MapType(AppState.DefaultMapType);
        GoogleMapsView.CreateAndShow(options, GetScaledComponentSize(), OnMapReady);
    }

    private Rect GetScaledComponentSize()
    {
        Rect originalRect = GetComponentSize();

        if (ReferenceCanvasScaler != null)
        {
            // Get the reference resolution from the CanvasScaler
            Vector2 referenceResolution = ReferenceCanvasScaler.referenceResolution;

            float scaleRatio = GetScaleRatio();

            float scaledWidth = originalRect.width * scaleRatio;
            float scaledHeight = originalRect.height * scaleRatio;

            Rect scaledRect = new Rect(originalRect.x, originalRect.y, scaledWidth, scaledHeight);
            return scaledRect;
        }
        else
        {
            Debug.LogWarning("CanvasScaler not found in the parent hierarchy.");
            return originalRect;
        }
    }

    private float GetScaleRatio()
    {
        Vector2 referenceResolution = ReferenceCanvasScaler.referenceResolution;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float widthRatio = screenWidth / referenceResolution.x;
        float heightRatio = screenHeight / referenceResolution.y;
        return Mathf.Min(widthRatio, heightRatio);
    }

    public Rect GetComponentSize()
    {
        // Get the RectTransform component
        RectTransform rectTransform = MapContainer.GetComponent<RectTransform>();
        // return rectTransform.rect;


        // Calculate the absolute position of the rect within the canvas
        Vector2 absolutePosition = GetCanvasPosition();
        // Calculate the final position and size of the rect

        Rect rect = new Rect(absolutePosition, rectTransform.rect.size);
        return rect;
    }

    public Vector2 GetCanvasPosition()
    {
        // Get the RectTransform of the GameObject
        RectTransform rectTransform = MapContainer.GetComponent<RectTransform>();

        // Get the absolute position of the RectTransform in world space
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // Convert the bottom-left corner to screen coordinates
        Vector2 canvasPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[0]);
        canvasPosition.y += GetScaleRatio() * (Toolbar.GetComponent<RectTransform>().rect.height-5);

        return canvasPosition;
    }


    /// <summary>
    /// Event listener when map is ready for operations
    /// </summary>
    private void OnMapReady(GoogleMapsView googleMapsView)
    {
        Toolbar.ShowToolbar(false);

        Debug.Log("The map is ready!");
        Map = googleMapsView;

        DisplayMarkers(PathpointList);

        Map.SetOnMarkerClickListener(OnMarkerClickHandler, false);
        Map.SetOnMapClickListener(OnMapClickHandler);
    }

    private void OnMarkerClickHandler(Marker marker)
    {            
        Pathpoint pathpoint = MarkerPathpoint[marker.Id];
        Debug.Log($"Marker clicked: {marker.Title}" + " Pathpoint: " + pathpoint.Id);

        OnPathpointSelected?.Invoke(pathpoint);

        // Unselect the marker if it's currently active
        RenderMarkerUnselected(DestinationMarker, DestinationPathpoint);

        if (!EnableDestinationMarker)
        {
           // Unselect the previously selected marker 
           RenderMarkerUnselected(SelectedMarker, SelectedPathpoint);
           SelectedMarker = marker;    

           // Select the clicked marker
           SelectedPathpoint = pathpoint;
           RenderMarkerSelected(SelectedMarker, pathpoint);

           return;
        }

        DestinationMarker = marker;
        DestinationPathpoint = pathpoint;
        RenderMarkerSelected(DestinationMarker, DestinationPathpoint);
    }

    private void OnMapClickHandler(LatLng latLng)
    {
        Debug.Log($"Map clicked: {latLng.Latitude}, {latLng.Longitude}");    

        RenderMarkerUnselected(SelectedMarker, SelectedPathpoint);
        RenderMarkerUnselected(DestinationMarker, DestinationPathpoint);

        SelectedMarker = null;
        DestinationMarker = null;
        SelectedPathpoint = null;
        DestinationPathpoint = null;        

        OnPathpointSelected?.Invoke(null);
    }

    private void RenderMarkerSelected(Marker marker, Pathpoint pathpoint){
        if (marker != null)
        {            
            var icon = GetPathpointIcon(pathpoint, selected : true);
            marker.SetIcon(NewCustomDescriptor(icon));
            marker.SetAnchor(0.5f, 0.5f);
        }
    }

    private void RenderMarkerUnselected(Marker marker, Pathpoint pathpoint){
        if (marker != null)
        {
            var icon = GetPathpointIcon(pathpoint, selected : false);
            marker.SetIcon(NewCustomDescriptor(icon));            
            marker.SetAnchor(0.5f, 1.0f);
        }
    }

    private Texture2D ChangeIconColor(Texture2D icon, Color color)
    {
        // Create a new Texture2D object with the same size as the original texture
        Texture2D newIcon = new Texture2D(icon.width, icon.height);

        // Get the pixel data from the original texture
        Color[] pixels = icon.GetPixels();

        // Modify the color values based on the color multiplier
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].r *= color.r;
            pixels[i].g *= color.g;
            pixels[i].b *= color.b;
            //pixels[i].a *= color.a; // Adjust alpha if necessary
        }

        // Apply the modified pixel data to the new texture
        newIcon.SetPixels(pixels);
        newIcon.Apply();

        return newIcon;
    }

    private Texture2D ResizeTexture(Texture2D source)
    {
        float scaleFactor = GetScaleRatio();
        int newWidth = Mathf.RoundToInt(source.width * scaleFactor);
        int newHeight = Mathf.RoundToInt(source.height * scaleFactor);

        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;

        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Debug.Log($"Source texture: {source.width}x{source.height} scaledDimensions: {newWidth}x{newHeight} result:{result.width}x{result.height}");

        return result;
    }

    static ImageDescriptor NewCustomDescriptor(Texture2D icon)
    {
        return ImageDescriptor.FromTexture2D(icon);
    }

    private void CleanupSnapshots()
    {
        if (currentSnapshot != null)
        {
            Destroy(currentSnapshot);
            currentSnapshot = null;
        }

        if (currentSprite != null)
        {
            Destroy(currentSprite);
            currentSprite = null;
        }
    }

    void OnDestroy()
    {
        DestroyImmediate(ColoredMarkerIcon);
        DestroyImmediate(ScaledPOILandmarkMarkerIcon);
        DestroyImmediate(ScaledPOIReassuranceMarkerIcon);
        DestroyImmediate(SelecteddMarkerIcon);
        DestroyImmediate(SelectedPOILandmarkMarkerIcon);
        DestroyImmediate(SelectedPOIReassuranceMarkerIcon);
        DestroyImmediate(DeletedPOILandmarkMarkerIcon);
        DestroyImmediate(DeletedPOIReassuranceMarkerIcon);

        CleanupSnapshots();

        if (Map != null)
        {
            Map.Dismiss();
        }        
    }


}
