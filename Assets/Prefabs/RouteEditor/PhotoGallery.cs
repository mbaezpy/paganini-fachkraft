using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


[System.Serializable]
public class GalleryEvent : UnityEvent<PathpointPhoto, int>
{
}

public class PhotoGallery : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject Content;
    public GameObject ItemPrefab;
    public GameObject BlankState;
    public TMPro.TMP_Text TitleText;
    public Button AddPhotoButton;


    [Header("Utility")]
    public PhotoPicker PhotoFilePicker;

    private List<PathpointPhoto> CurrentPhotos;
    private Pathpoint CurrentPathpoint;
    private RouteSharedData.EditorMode _editMode;

    [Header("Events")]
    public GalleryEvent OnPhotoOpened;
    public UnityEvent OnPhotoCurated;


    public RouteSharedData.EditorMode EditMode
    {
        get { return _editMode; }
        set
        {
            _editMode = value;

            // Update the TitleText based on the EditMode
            switch (value)
            {
                case RouteSharedData.EditorMode.Cleaning:
                    TitleText.text = "Überprüfen Sie Fotos, die an diesem Ort aufgenommen wurden. Im Cleaning-modus können Sie minderwertige Bilder abwählen. Tippen Sie für Vollbildansicht.";
                    break;
                case RouteSharedData.EditorMode.Discussion:
                    TitleText.text = "Im Diskussionsmodus Feedback sammeln, um auszuwählen, welche Bilder zu behalten sind. Tippen Sie auf ein Bild für Vollbild-Feedback.";
                    break;
                case RouteSharedData.EditorMode.ReadOnly:
                    TitleText.text = "Im Trainingsmodus Bilder nur im Lese-Modus durchsehen. Keine Bearbeitung möglich.";
                    break;
                default:
                    TitleText.text = "";
                    break;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {        
        AddPhotoButton.gameObject.SetActive(EditMode == RouteSharedData.EditorMode.Cleaning || EditMode == RouteSharedData.EditorMode.Discussion);
    }

    // Update is called once per frame
    void Update()
    {

    }


    /* UI and events */

    public void Clearlist()
    {
        Debug.Log("Clearing list!!!!!!!!!!!!!!!");
        if (!Content) return;

        Transform content = Content.GetComponent<Transform>();

        // Remove all children
        for (int i = 0; i < content.childCount; i++)
        {
            // Get the child game object
            GameObject child = content.GetChild(i).gameObject;

            // Destroy the child game object
            if (child.name != "AddPhoto") 
            {
                DestroyItem(child);
            }
            
        }
    }

    public void LoadPhotos(List<PathpointPhoto> photos, Pathpoint pathpoint)
    {
        CurrentPhotos = photos;
        CurrentPathpoint = pathpoint;

        if (!Content) return;

        int index = 0;
        foreach (var photo in photos)
        {
            if (EditMode == RouteSharedData.EditorMode.Cleaning ||
                photo.CleaningFeedback != PathpointPhoto.PhotoFeedback.Delete)
            {
                if (photo.Data != null)
                {
                    AddItem(photo, index++);
                }
                
            }
            
            Debug.Log($"Id: {photo.Id} Timestamp:{photo.Timestamp}");
        }

        RefreshAddPhotoCard();

    }

    public void AddNewPhoto(){
        // Change the profile picture
        PhotoFilePicker.OnPhotoSelected.RemoveAllListeners();
        PhotoFilePicker.OnPhotoSelected.AddListener(NewPhotoSelectedHandler);
        PhotoFilePicker.PickUpImage();                    
    }

    public void AddItem(PathpointPhoto p, int index)
    {
        var neu = Instantiate(ItemPrefab, Content.transform);

        var item = neu.GetComponent<PhotoElementPrefab>();
        item.OnPhotoOpened.AddListener(OnPhotoOpenedHandler);
        item.OnSelectedChanged.AddListener(OnPhotoSelectedHandler);
        item.FillPhoto(p, EditMode == RouteSharedData.EditorMode.Cleaning, index);

        Debug.Log($"Index: {index} Photo: {p.Id}");

    }

    private void DestroyItem(GameObject itemObject)
    {
        // Remove event listeners before destroying the item
        var item = itemObject.GetComponent<PhotoElementPrefab>();
        item.OnPhotoOpened.RemoveListener(OnPhotoOpenedHandler);
        item.OnSelectedChanged.RemoveListener(OnPhotoSelectedHandler);

        // Destroy the item game object
        Destroy(itemObject);
    }

    private void OnPhotoOpenedHandler(PhotoElementPrefab prefab) {
        // 'Deleted' / 'Unselected' pictures cannot be opened
        if (prefab.CurrentPathpointPhoto.CleaningFeedback != PathpointPhoto.PhotoFeedback.Delete)
        {
            OnPhotoOpened?.Invoke(prefab.CurrentPathpointPhoto, prefab.CurrentIndex);
        }        
    }

    private void OnPhotoSelectedHandler(PhotoElementPrefab prefab)
    {
        var photo = prefab.CurrentPathpointPhoto;
        if (prefab.IsSelected) {
            photo.CleaningFeedback = PathpointPhoto.PhotoFeedback.Keep;
        } else {
            photo.CleaningFeedback = PathpointPhoto.PhotoFeedback.Delete;
        }
        photo.InsertDirty();

        OnPhotoCurated?.Invoke();
    }

    /// <summary>
    /// Handle the photo selected event
    /// </summary>
    /// <param name="path">Path to the selected photo</param>
    private void NewPhotoSelectedHandler(string path){
        if (path != null) {
            var picBytes = PictureUtils.LoadImageFile(path);
             
            // get ids
            var lastPP = PathpointPhoto.GetWithMinId( p => p.Id);
            int ppId = lastPP != null ? lastPP.Id - 1 : -1;   

            var lastPD = PhotoData.GetWithMinId( p => p.Id);
            int pdId = lastPD != null ? lastPD.Id - 1 : -1;            

            // photo data
            var photoData = new PhotoData();
            photoData.Id = pdId; 
            photoData.Photo = picBytes;            
            photoData.InsertDirty();                             

            // photo metadata
            PathpointPhoto newPhoto = new PathpointPhoto();
            newPhoto.Id = ppId;
            newPhoto.PathpointId = CurrentPathpoint.Id;
            newPhoto.Timestamp = DateUtils.UTCMilliseconds();
            newPhoto.PhotoId = photoData.Id;
            newPhoto.Data = photoData;

            // If the photo is added  in Discussion mode, the cleaning feedback should be 'Keep'
            // so that it is not curated as 'deleted' by default
            if (EditMode == RouteSharedData.EditorMode.Discussion)
            {
                newPhoto.CleaningFeedback = PathpointPhoto.PhotoFeedback.Keep;
            }

            newPhoto.InsertDirty();

            CurrentPhotos.Add(newPhoto);            
            AddItem(newPhoto, CurrentPhotos.Count - 1);

            // push the blank state to the end of the list, again
            RefreshAddPhotoCard();
            
        }

    }   

    private void RefreshAddPhotoCard(){
        // Show blank state card suggesting users to add a new photo (when there are less than 3 photos, otherwise the button is also there)
        bool showBlankState = CurrentPhotos.Count < 3 && EditMode == RouteSharedData.EditorMode.Cleaning || EditMode == RouteSharedData.EditorMode.Discussion;
        if (BlankState != null)
        {
            BlankState.SetActive(showBlankState);

            // Reparent blank state to the end of the list
            if (showBlankState)
            {
                BlankState.transform.SetAsLastSibling();
            }
        }        
    } 

    public void CleanupView()
    {
        // Clear the list of photos and destroy instantiated items
        Clearlist();

        // Unload unused assets (textures, etc.)
        //Resources.UnloadUnusedAssets();

        // Reset the current photos
        CurrentPhotos = null;
        CurrentPathpoint = null;

        // Optionally, you can also destroy the GameObject itself if it's no longer needed
        //Destroy(gameObject);
    }

}
