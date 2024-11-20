using System;
using UnityEngine;
using UnityEngine.UI;
using static Pathpoint;

public class PinDetailsEdit : MonoBehaviour
{
    [Header("Pin Data")]
    public POITimelineItem POIMetadata;
    //public TMPro.TMP_InputField PinDescription;
    public PhotoElementPrefab POIVideoThumbnail;
    public PhotoElementPrefab POIPhotoThumbnail;
    public DirectionTypeToggle DirectionType;
    public POITypeToggle POIType;
    public POIFeedbackToggle POIFeedback;
    public POIFeedbackToggle POICleaning;
    public POIFeedbackToggle POIReadOnlyFeedback;

    [Header("Edit modes")]
    public GameObject CleaningPanel;
    public GameObject DiscussionPanel;
    public GameObject ReadOnlyPanel;

    [Header("Gallery / Video switch")]
    public GameObject SwitchVideoPanel;
    public GameObject SwitchGalleryPanel;

    [Header("Additional UI Components")]
    public GameObject SwitchToCleaningMode;
    public GameObject SwitchToDiscussionMode;

    private Pathpoint CurrentPathpoint;
    private Way CurrentWay;
    private int CurrentIndex;
    public RouteSharedData.EditorMode EditMode { get; set; }
    bool isTempCleaningEnabled;

    // Start is called before the first frame update
    void Start()
    {
        isTempCleaningEnabled = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PopulateMetadata(Pathpoint point, Way way, int index, bool preserveTempCleaning = false)
    {
        CurrentPathpoint = point;
        CurrentWay = way;
        CurrentIndex = index;

        // Changes allowed?
        DisableChangesOnPOI(point.CleaningFeedback == Pathpoint.POIFeedback.No);

        // POI information
        FillPOIEditMode(point, way, index);

        // Information Editing
        if (EditMode == RouteSharedData.EditorMode.Cleaning)
        {
            isTempCleaningEnabled = false;
            PopulateCleaning(point);
        }
        else if (EditMode == RouteSharedData.EditorMode.Discussion)
        {
            isTempCleaningEnabled = isTempCleaningEnabled && preserveTempCleaning;
            PopulateDiscussion(point);            
        }
        else
        {
            PopulateReadOnly(point);
        }

        // Render Navigation Thumbnnail
        if (point.Photos != null && point.Photos.Count > 0) {
            // render picture
            var previewPhoto = PathpointPhoto.GetDefaultPhoto(point.Photos);
            RenderThumbnail(previewPhoto);
        }

    }

    public void EnableSwitchToGallery(bool activate) {
        SwitchVideoPanel.SetActive(!activate);
        SwitchGalleryPanel.SetActive(activate);
    }

    public void DisableChangesOnPOI(bool disable)
    {
        InactiveOverlay.SharedData.Instance.CurrentlyDisabled = disable;
    }

    public void SwitchToCleaning()
    {
        PopulateCleaning(CurrentPathpoint);
        SwitchToDiscussionMode.SetActive(true);
        isTempCleaningEnabled = true;
    }

    public void SwitchToDiscussion()
    {
        PopulateDiscussionInfo(CurrentPathpoint);
        SwitchToCleaningMode.SetActive(true);
        isTempCleaningEnabled = false;
    }


    // Private functions


    private void PopulateCleaning(Pathpoint point) {

        DiscussionPanel.SetActive(false);
        ReadOnlyPanel.SetActive(false);
        SwitchToCleaningMode.SetActive(false);

        // If start / destination, user cannot edit POI Information (type, direction..)
        if (point.POIType == Pathpoint.POIsType.WayStart ||
            point.POIType == Pathpoint.POIsType.WayDestination)
        {
            CleaningPanel.SetActive(false);
        }
        else
        {
            CleaningPanel.SetActive(true);
            POIType.SetSelectedPOIType(point.POIType);
            POICleaning.FillDiscussionFeedback(point);
        }

        // Direction Type
        if (point.POIType == Pathpoint.POIsType.Landmark)
        {
            DirectionType.gameObject.SetActive(true);
            DirectionType.SetSelectedDirectionType(point.Instruction.ToString());
        }
        else
        {
            DirectionType.gameObject.SetActive(false);
        }
    }

    private void PopulateDiscussion(Pathpoint poi)
    {
        bool isNormalPOI = poi.POIType != Pathpoint.POIsType.WayStart &&
                           poi.POIType != Pathpoint.POIsType.WayDestination;

        if (isTempCleaningEnabled)
        {
            PopulateCleaning(poi);
            SwitchToDiscussionMode.SetActive(isNormalPOI);
        }
        else
        {
            PopulateDiscussionInfo(poi);
            SwitchToCleaningMode.SetActive(isNormalPOI);
        }
    }

    private void PopulateDiscussionInfo(Pathpoint poi)
    {
        CleaningPanel.SetActive(false);        
        ReadOnlyPanel.SetActive(false);
        SwitchToDiscussionMode.SetActive(false);

        if (poi.POIType == Pathpoint.POIsType.WayStart ||
            poi.POIType == Pathpoint.POIsType.WayDestination)
        {
            DiscussionPanel.SetActive(false);
        }
        else
        {
            DiscussionPanel.SetActive(true);
            SwitchToCleaningMode.SetActive(true);
            POIFeedback.FillDiscussionFeedback(poi);
        }        
    }

    private void PopulateReadOnly(Pathpoint poi)
    {
        CleaningPanel.SetActive(false);
        DiscussionPanel.SetActive(false);        

        if (poi.POIType == Pathpoint.POIsType.WayStart ||
            poi.POIType == Pathpoint.POIsType.WayDestination)
        {            
            ReadOnlyPanel.SetActive(false);
        }
        else
        {
            ReadOnlyPanel.SetActive(true);
            POIReadOnlyFeedback.FillDiscussionFeedback(poi);
        }        
    }


    private void FillPOIEditMode(Pathpoint point, Way way, int index)
    {
        if (point.POIType == Pathpoint.POIsType.WayStart)
        {
            POIMetadata.FillPathpointStart(point, way);
        }
        else if (point.POIType == Pathpoint.POIsType.WayDestination)
        {
            POIMetadata.FillPathpointDestination(point, way);
        }
        else
        {
            POIMetadata.FillPathpoint(point, index);
        }
    }

    private void RenderThumbnail(PathpointPhoto pathpointPhoto)
    {
        // Destroy the old texture before creating a new one
        //if (POIVideoThumbnail.texture != null)
        //{
        //    Destroy(POIVideoThumbnail.texture);
        //}

        //Texture2D texture = new Texture2D(2, 2);
        //texture.LoadImage(imageBytes);

        //POIVideoThumbnail.texture = texture;
        //POIVideoThumbnail.gameObject.SetActive(true);

        //if (POIPhotoThumbnail.texture != null)
        //{
        //    Destroy(POIPhotoThumbnail.texture);
        //}

        //POIPhotoThumbnail.texture = texture;
        //POIPhotoThumbnail.gameObject.SetActive(true);


        POIPhotoThumbnail.FillPhoto(pathpointPhoto, false,0, displayFeedback: false);
        POIPhotoThumbnail.gameObject.SetActive(true);
        POIVideoThumbnail.FillPhoto(pathpointPhoto, false, 0, displayFeedback: false);
        POIVideoThumbnail.gameObject.SetActive(true);

    }

    public void OnPOITypeValueChanged(Pathpoint.POIsType pOIType)
    {
        CurrentPathpoint.POIType = pOIType;
        CurrentPathpoint.InsertDirty();

        // Decision point? (landmark?)
        DirectionType.gameObject.SetActive(CurrentPathpoint.POIType == Pathpoint.POIsType.Landmark);

        Debug.Log($"OnPOITypeValueChanged: {pOIType}");

        PopulateMetadata(CurrentPathpoint, CurrentWay, CurrentIndex, true);
    }
    public void OnDirectionTypeValueChanged(string direction)
    {
        direction = direction == "" || direction == null ? "None" : direction;
        var instruction = Enum.Parse<NavDirection>(direction);

        CurrentPathpoint.Instruction = instruction;
        CurrentPathpoint.InsertDirty();

        Debug.Log($"OnDirectionTypeValueChanged: {direction}");

        FillPOIEditMode(CurrentPathpoint, CurrentWay, CurrentIndex);
    }


    public void RefreshPOIMeta()
    {
        FillPOIEditMode(CurrentPathpoint, CurrentWay, CurrentIndex);
    }


    private void DestroyTexture(Texture texture)
    {
        if (texture != null)
        {
            // Use DestroyImmediate to properly destroy the texture at runtime
            DestroyImmediate(texture, true);
        }
    }

    public void CleanupView()
    {
        // Clear references to objects
        CurrentPathpoint = null;
        CurrentWay = null;

        // Destroy textures to release memory
        POIVideoThumbnail.CleanupView();
        POIPhotoThumbnail.CleanupView();

        POIMetadata.CleanupView();

        // Remove event listeners or cleanup any other resources as needed
        // For example, if you have registered event listeners, unregister them here.

        // Deactivate or hide any game objects or UI elements that are no longer needed
        CleaningPanel.SetActive(false);
        DiscussionPanel.SetActive(false);
        ReadOnlyPanel.SetActive(false);
        SwitchVideoPanel.SetActive(false);
        SwitchGalleryPanel.SetActive(false);

        // Unload unused assets to free up memory
        Resources.UnloadUnusedAssets();
    }

}
