using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapToolbar : MonoBehaviour
{
    [Header("Views")]
    public GameObject ToolbarView;
    public GameObject TipView;
    public MapManager MapView;    

    [Header("Toolbar")]
    public GameObject POIOptions;
    public GameObject GPSOptions;
    public GameObject POIMoveOptions;
    public GameObject UndoOptions;

    [Header("UI Components")]
    public BubbleIcon POIIcon;
    public GameObject GPSIcon;
    public Button DeleteButton;
    public Button UndoButton;
    public Button MoveConfirmButton;

    public UnityEvent<Pathpoint,Int32> OnPOIOpened;

    private RouteSharedData SharedData;

    private Pathpoint CurrentPathpoint;
    private Pathpoint TargetPathpoint;

    private bool POIMoving = false;

    void Awake()
    {
        SharedData = RouteSharedData.Instance;
    }

    void Start()
    {
        ShowToolbar(false);

        POIMoving = false;
    }

    public void ShowToolbar(bool show)
    {
        ToolbarView.SetActive(show);
        TipView.SetActive(!show);
    }

    public void Hide(){
        ToolbarView.SetActive(false);
        TipView.SetActive(false);        
    }

    /// <summary>
    /// PinSelectedHandler is called when a pin is selected in the map
    /// </summary>
    /// <param name="pin"> The selected pin </param>
    public void PinSelectedHandler(Pathpoint pin){

        // POIMoving
        if (POIMoving){
            PrepareMoving(pin);
            return;
        }

        CurrentPathpoint = pin;

        if (pin == null){
            ShowToolbar(false);
            return;
        }

        MapView.EnableDestinationMarker = false;

        int index = SharedData.POIList.FindIndex(x => x.Id == pin.Id);
        RenderTitle(pin, index);        

        // Enable edit options only for cleaning and discussion
        if (SharedData.CurrentEditorMode == RouteSharedData.EditorMode.Cleaning || 
            SharedData.CurrentEditorMode == RouteSharedData.EditorMode.Discussion){
            // Disable the delete button for GPS points coming from the API
            bool enableDelete = ! (pin.POIType == Pathpoint.POIsType.Point && pin.FromAPI);
            ShowDeleteOrUndoOptions(showDelete: true, isInterctable: enableDelete);

            if (pin.POIType == Pathpoint.POIsType.Point){
                ShowGPSOptions();                        
            } 
            else if (pin.POIType == Pathpoint.POIsType.Landmark || pin.POIType == Pathpoint.POIsType.Reassurance){
                ShowPOIOptions(pin);
            }
            else if (pin.POIType == Pathpoint.POIsType.WayStart || pin.POIType == Pathpoint.POIsType.WayDestination){
                ShowNoOptions();
            }             
        }
    }

    /// <summary>
    /// Show the options for a POI
    /// </summary>
    public void ShowPOIMoveOptions()
    {
        ShowOptionView(POIMoveOptions);
        POIMoving = true;
        MapView.EnableDestinationMarker = true;
    }    

    /// <summary>
    /// Turn a GPS point into a POI
    /// </summary>
    public void TurnGPSIntoPOI()
    {
        // Next POI?
        int poiIndex = GetNextPOIIndex(CurrentPathpoint);        
        
        CurrentPathpoint.POIType = Pathpoint.POIsType.Reassurance;
        CurrentPathpoint.InsertDirty();
        
        // Insert the new POI, before that one
        SharedData.POIList.Insert(poiIndex-1, CurrentPathpoint);

        MapView.UpdateMarker(CurrentPathpoint);
        
        PinSelectedHandler(CurrentPathpoint);
    }

    public void RemovePOI()
    {        
        // If it's a draft, we can remove it
        if (!CurrentPathpoint.FromAPI){
            RemovePOIFromMemory(CurrentPathpoint);
            Pathpoint.Delete(CurrentPathpoint.Id);
            return;            
        }

        // The pathpoint is a reassurance or decision point
        if(CurrentPathpoint.POIType != Pathpoint.POIsType.Point){           

            // If it doen't have photos, we can remove it and turn it into a GPS point
            if (CurrentPathpoint.Photos == null || CurrentPathpoint.Photos.Count == 0){
                RemovePOIFromMemory(CurrentPathpoint, removeAlsoGPS: false);
                CurrentPathpoint.POIType = Pathpoint.POIsType.Point;
                CurrentPathpoint.InsertDirty();

                Debug.Log("POI removed and turned into GPS point");
            }
            else { 
                // if it has photos, we lazy delete it, giving the user the option to recover it     
                if (SharedData.CurrentEditorMode == RouteSharedData.EditorMode.Cleaning){
                    CurrentPathpoint.CleaningFeedback = Pathpoint.POIFeedback.No;                    
                }
                else if (SharedData.CurrentEditorMode == RouteSharedData.EditorMode.Discussion){
                    CurrentPathpoint.RelevanceFeedback = Pathpoint.POIFeedback.No;                                        
                }
                
                CurrentPathpoint.InsertDirty();

                Debug.Log("POI lazy deleted with feedback");
            }
        }

        // We are not deleting the GPS points, once they are in the API

        MapView.UpdateMarker(CurrentPathpoint);
        MapView.RefreshSelectedMarkersAsFocused();
        PinSelectedHandler(CurrentPathpoint);
    }

    public void OpenPOI(){
        OnPOIOpened?.Invoke(CurrentPathpoint, SharedData.POIList.FindIndex(x => x.Id == CurrentPathpoint.Id));
    }

    public void UndoDeletePOI(){
        // if it has photos, we lazy delete it, giving the user the option to recover it     
        if (SharedData.CurrentEditorMode == RouteSharedData.EditorMode.Cleaning){
            CurrentPathpoint.CleaningFeedback = Pathpoint.POIFeedback.None;                    
        }
        else if (SharedData.CurrentEditorMode == RouteSharedData.EditorMode.Discussion){
            CurrentPathpoint.RelevanceFeedback = Pathpoint.POIFeedback.None;                                        
        }
        
        CurrentPathpoint.InsertDirty();
        Debug.Log("POI lazy deleted with feedback");        

        MapView.UpdateMarker(CurrentPathpoint);
        MapView.RefreshSelectedMarkersAsFocused();
        PinSelectedHandler(CurrentPathpoint);        
    }

    public void MovePOI(){
        if (TargetPathpoint == null){
            return;
        }

        Debug.Log("Moving POI: "+ CurrentPathpoint.Id + " to "+ TargetPathpoint.Id);

        // Swap the pathpoints
        CurrentPathpoint.Latitude = TargetPathpoint.Latitude;
        CurrentPathpoint.Longitude = TargetPathpoint.Longitude;
        CurrentPathpoint.Timestamp = TargetPathpoint.Timestamp;

        var ogPathpoint = Pathpoint.Get(CurrentPathpoint.Id);
        TargetPathpoint.Latitude = ogPathpoint.Latitude;
        TargetPathpoint.Longitude = ogPathpoint.Longitude;
        TargetPathpoint.Timestamp = ogPathpoint.Timestamp;

        CurrentPathpoint.InsertDirty(); 
        TargetPathpoint.InsertDirty(); 

        // Swap positions between Current and Target pathpoints
        int ogIndex = SharedData.PathpointList.FindIndex(x => x.Id == CurrentPathpoint.Id);
        int newIndex = SharedData.PathpointList.FindIndex(x => x.Id == TargetPathpoint.Id);

        SharedData.PathpointList[ogIndex] = TargetPathpoint;
        SharedData.PathpointList[newIndex] = CurrentPathpoint;                

        // Update the marker
        POIMoving = false;
        MapView.EnableDestinationMarker = false;        
        MapView.SwapMarkers(CurrentPathpoint, TargetPathpoint);        

        // Update the target
        TargetPathpoint = null;

        // refresh the currently selected marker?
    }

    public void CancelMovePOI(){
        POIMoving = false;
        MapView.EnableDestinationMarker = false;
        PinSelectedHandler(CurrentPathpoint);
    }    

    private void PrepareMoving(Pathpoint pin){

        // Cancel if the user tap outside the map
        if (pin == null){
            CancelMovePOI();
            return;
        }

        Debug.Log("PrepareMoving " + pin.POIType + " - CurrentPathpoint "+ CurrentPathpoint.Id + " - TargetPathpoint: "+ pin.Id);

        if (pin.POIType == Pathpoint.POIsType.Point){

            // check that is moving only between two POIs, and not jumping over one

            TargetPathpoint = pin;
            MoveConfirmButton.interactable = true;                 
        } 

    }

    // Private functions

    private void RemovePOIFromMemory(Pathpoint pathpoint, bool removeAlsoGPS = true){
            Debug.Log("Removing POI: "+ pathpoint.Id + " - "+ pathpoint.POIType + " - removeAlsoGPS: "+ removeAlsoGPS);
            // remove gps track
            if (removeAlsoGPS){
                int pinIndex = SharedData.PathpointList.FindIndex(x => x.Id == pathpoint.Id);
                SharedData.PathpointList.RemoveAt(pinIndex);
            }
            
            // remove POI
            int poiIndex = SharedData.POIList.FindIndex(x => x.Id == pathpoint.Id);
            Debug.Log("Remove POI Index: "+ poiIndex + " - POIList.Count: " + SharedData.POIList.Count);
            if (poiIndex != -1){
                SharedData.POIList.RemoveAt(poiIndex);
            }
            Debug.Log("After removing POIList.Count: " + SharedData.POIList.Count);
    }

    private int GetNextPOIIndex(Pathpoint pin){
        int pinIndex = SharedData.PathpointList.FindIndex(x => x.Id == pin.Id);
        
        // find the next POI
        Pathpoint poi = null;
        for (int i = pinIndex; i < SharedData.PathpointList.Count; i++)
        {
            if (SharedData.PathpointList[i].POIType != Pathpoint.POIsType.Point){
                poi = SharedData.PathpointList[i];
                return SharedData.POIList.FindIndex(x => x.Id == poi.Id);
            }
        }
        return -1;
    }

    private void ShowNoOptions()
    {
        ShowOptionView(null);
    }

    private void ShowPOIOptions(Pathpoint pin)
    {
        if (pin.CleaningFeedback == Pathpoint.POIFeedback.No || pin.RelevanceFeedback == Pathpoint.POIFeedback.No){
            ShowDeleteOrUndoOptions(showDelete: false);
            ShowUndoOptions();
            return;
        }
        ShowOptionView(POIOptions);
    }

    private void ShowGPSOptions()
    {
        ShowOptionView(GPSOptions);
    }

    private void ShowUndoOptions(){
        ShowOptionView(UndoOptions);
    }

    private void ShowDeleteOrUndoOptions(bool showDelete = true, bool isInterctable = true)
    {
        DeleteButton.gameObject.SetActive(showDelete);
        DeleteButton.interactable = isInterctable;

        UndoButton.gameObject.SetActive(!showDelete);
        UndoButton.interactable = isInterctable;        
    }

    private void RenderTitle(Pathpoint pin, int index){

        GPSIcon.SetActive(pin.POIType == Pathpoint.POIsType.Point);
        POIIcon.gameObject.SetActive(pin.POIType != Pathpoint.POIsType.Point);        

        if (pin.POIType != Pathpoint.POIsType.Point){
            POIIcon.FillPathpoint(pin, AppState.CurrentWay, index);
        } 
    }

    private void ShowOptionView(GameObject view)
    {
        ShowToolbar(true);
        POIOptions.SetActive(view == POIOptions);
        GPSOptions.SetActive(view == GPSOptions);
        POIMoveOptions.SetActive(view == POIMoveOptions);
        UndoOptions.SetActive(view == UndoOptions);
    }    
}
