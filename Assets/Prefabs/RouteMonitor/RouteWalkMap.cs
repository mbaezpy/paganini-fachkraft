using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LocationTools;
using NinevaStudios.GoogleMaps;
using PaganiniRestAPI;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RouteWalkMap : MonoBehaviour
{
    public GameObject MapContainer;
    public CanvasScaler ReferenceCanvasScaler;

    [Header("Route elements")]
    public Texture2D DefaultMarkerIcon;
    public Texture2D POILandmarkMarkerIcon;
    public Texture2D POIReassuranceMarkerIcon;
    public Color RouteColor;

    [Header("Route Walk elements")]
    public Texture2D RouteWalkMarkerIcon;
    public Color OffTrackColor;
    public Color OnTrackColor;
    public Color OnPOIColor;

    private static Texture2D ColoredMarkerIcon;
    private static Texture2D ScaledPOILandmarkMarkerIcon;
    private static Texture2D ScaledPOIReassuranceMarkerIcon;

    private Texture2D OffTrackIcon;
    private Texture2D OnTrackIcon;
    private Texture2D OnPOIIcon;

    public GameObject IconContainer; // A UI container to inspect icons


    //PRIVATE
    private GoogleMapsView Map;
    private List<Pathpoint> PathpointList;
    private List<PathpointLog> PathpointLogList;
    private RouteSharedData SharedData;
    private RouteWalkSharedData WalkSharedData;


    // Start is called before the first frame update
    void Awake()
    {
        SharedData = RouteSharedData.Instance;
        WalkSharedData = RouteWalkSharedData.Instance;
    }

    private void Start()
    {
        //OffTrackIcon = ChangeIconColor(RouteWalkMarkerIcon, OffTrackColor);
        //OnTrackIcon = ChangeIconColor(RouteWalkMarkerIcon, OnTrackColor);
        //OnPOIIcon = ChangeIconColor(RouteWalkMarkerIcon, OnPOIColor);

        //DefaultMarkerIcon = ChangeIconColor(DefaultMarkerIcon, RouteColor);

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
    public void LoadMap()
    {
        PathpointList = SharedData.PathpointList;
        PathpointLogList = WalkSharedData.PathpointLogList;

        if (OffTrackIcon == null)
        {
            OffTrackIcon = ChangeIconColor(RouteWalkMarkerIcon, OffTrackColor);
            OnTrackIcon = ChangeIconColor(RouteWalkMarkerIcon, OnTrackColor);
            OnPOIIcon = ChangeIconColor(RouteWalkMarkerIcon, OnPOIColor);

            ColoredMarkerIcon = ChangeIconColor(DefaultMarkerIcon, RouteColor);
            ScaledPOILandmarkMarkerIcon = ResizeTexture(POILandmarkMarkerIcon);
            ScaledPOIReassuranceMarkerIcon = ResizeTexture(POIReassuranceMarkerIcon);
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

    public void DisplayMarkers(List<Pathpoint> pathpoints)
    {
        int i = 0;

        foreach (var pathpoint in pathpoints)
        {
            Debug.Log("DisplayMarkers: " + pathpoint.Id);

            var icon = ColoredMarkerIcon;
            if (pathpoint.POIType == Pathpoint.POIsType.Point)
            {
                icon = ColoredMarkerIcon;
            }
            else if (pathpoint.POIType == Pathpoint.POIsType.Landmark)
            {
                icon = ScaledPOILandmarkMarkerIcon;
            }
            else
            {
                icon = ScaledPOIReassuranceMarkerIcon;
            }

            var mo = new MarkerOptions()
                    .Position(new LatLng(pathpoint.Latitude, pathpoint.Longitude))
                    .Icon(NewCustomDescriptor(icon))
                    .Title($"Marker {i} Lat: {pathpoint.Latitude} Lon: {pathpoint.Longitude}");

            Map.AddMarker(mo);
            i++;
        }
    }

    public void DisplayRouteWalk(List<PathpointLog> pathpointLogs)
    {
        int i = 0;

        //AddIconToUI(OffTrackIcon, "OffTrackIcon");
        //AddIconToUI(OnTrackIcon, "OnTrackIcon");
        //AddIconToUI(OnPOIIcon, "OnPOIIcon");

        foreach (var log in pathpointLogs)
        {
            Texture2D icon = DefaultMarkerIcon;

            if (log.OnOffTrackEvent != null)
            {
                icon = OffTrackIcon;
                Debug.Log($"({i})Routewalk log: offtrack {log.OnOffTrackEvent}");
            }
            else if (log.OnPOIEvent != null)
            {
                icon = OnPOIIcon;
                Debug.Log($"({i})Routewalk log: onPOI {log.OnPOIEvent}");
            }
            else
            {
                icon = OnTrackIcon;
                Debug.Log($"({i})Routewalk log: onTrack {log.OnTrackEvent}");
            }

            var mo = new MarkerOptions()
                    .Position(new LatLng(log.Latitude, log.Longitude))
                    .Icon(NewCustomDescriptor(icon))
                    .Title($"({i}) Pace: {log.WalkingPace} Distance: {log.TotalWalkedDistance} Time: {log.TotalWalkingTime}  Walking?:{log.IsWalking}");

            Map.AddMarker(mo);
            i++;
        }
    }

    public Texture2D GetIconBasedLogStatus(PathpointLog log, bool RenderSpeed = false)
    {
        Texture2D icon = DefaultMarkerIcon;

        return icon;
    }


    int count = 0;
    public void RenderMarker(PathpointTraceMessage traceMessage)
    {
        Texture2D icon = DefaultMarkerIcon;

        var pathpoint = traceMessage.pathpoint;

        var mo = new MarkerOptions()
        .Position(new LatLng(pathpoint.ppoint_lat, pathpoint.ppoint_lon))
        .Icon(NewCustomDescriptor(icon))
        .Title($"Seq: {traceMessage.seq} Lat: {pathpoint.ppoint_lat} Lon: {pathpoint.ppoint_lon}");

        Map.AddMarker(mo);
        count++;
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
        canvasPosition.y += 35;

        return canvasPosition;
    }


    /// <summary>
    /// Event listener when map is ready for operations
    /// </summary>
    private void OnMapReady(GoogleMapsView googleMapsView)
    {
        Debug.Log("The map is ready!");
        Map = googleMapsView;

        DisplayMarkers(PathpointList);
        DisplayRouteWalk(PathpointLogList);
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

    private void AddIconToUI(Texture2D icon, string iconName)
    {
        GameObject iconGO = new GameObject(iconName);
        iconGO.transform.SetParent(IconContainer.transform);

        Image iconImage = iconGO.AddComponent<Image>();
        RectTransform rectTransform = iconGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100); // Set size as needed

        iconImage.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
    }

    static ImageDescriptor NewCustomDescriptor(Texture2D icon)
    {
        return ImageDescriptor.FromTexture2D(icon);
    }

    void OnDestroy(){
        DestroyImmediate(OffTrackIcon);
        DestroyImmediate(OnTrackIcon);
        DestroyImmediate(OnPOIIcon);
        
        DestroyImmediate(ScaledPOILandmarkMarkerIcon);
        DestroyImmediate(ScaledPOIReassuranceMarkerIcon);
        DestroyImmediate(ColoredMarkerIcon);

        if (Map != null)
        {
            Map.Dismiss();
        }
    }


}