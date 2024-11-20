using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UserPicEdit : MonoBehaviour
{

    [Header("Profile picture")]
    public RawImage UserPhoto;
    public GameObject NoPhoto;
    public GameObject DataPhoto;


    [Header("Utility")]
    public PhotoPicker PhotoFilePicker;
    
    public UnityEvent<byte[]> OnProfilePicChanged;

    // Start is called before the first frame update
    void Start()
    {     
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // public events

    /// <summary>
    /// Change the profile picture
    /// </summary>
    public void ChangeProfilePicture(){
        // Change the profile picture
        PhotoFilePicker.OnPhotoSelected.RemoveAllListeners();
        PhotoFilePicker.OnPhotoSelected.AddListener(PhotoSelectedHandler);
        PhotoFilePicker.PickUpImage();            
    }

    /// <summary>
    /// Render the profile picture
    /// </summary>
    /// <param name="picture">byte encoded picture</param>
    /// <param name="renderPicture">Whether to render picture (true) or only a placeholder (false)</param>
    public void RenderProfilePic(byte[] picture, bool renderPicture = true){
        if (picture == null){
            NoPhoto.SetActive(true);
            UserPhoto.gameObject.SetActive(false);
            DataPhoto.SetActive(false);
        } else {
            NoPhoto.SetActive(false);
            UserPhoto.gameObject.SetActive(true);
            DataPhoto.SetActive(true);

            if (renderPicture) PictureUtils.RenderPicture(UserPhoto, picture);
        }
        
    }

    // private events

    /// <summary>
    /// Handle the photo selected event
    /// </summary>
    /// <param name="path">Path to the selected photo</param>
    private void PhotoSelectedHandler(string path){
        if (path != null) {
            var picBytes = PictureUtils.LoadImageFile(path);
            
            OnProfilePicChanged?.Invoke(picBytes);

            RenderProfilePic(picBytes);
        }

    }

    private void OnDestroy(){
        PhotoFilePicker.OnPhotoSelected?.RemoveListener(PhotoSelectedHandler);

        if (UserPhoto.texture != null){
            DestroyImmediate(UserPhoto.texture, true);
        }
    }

}
