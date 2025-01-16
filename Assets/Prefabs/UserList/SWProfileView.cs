using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SWProfileView : MonoBehaviour
{

    [Header("Profile picture")]
    public UserPicEdit UserPhoto;

    [Header("Meta data")]
    public TMPro.TMP_Text Fullname;
    public TMPro.TMP_Text ErrorText;
    public TMPro.TMP_InputField UsernameInput;
    public TMPro.TMP_InputField FirstnameInput;
    public TMPro.TMP_InputField LastnameInput;
    //public TMPro.TMP_InputField ContactTelInput;
    public ButtonPrefab UpdateButton;

    private SocialWorker UpdatedUser;
    
    void Awake()
    {
        UserPhoto.OnProfilePicChanged.AddListener(PhotoChangedHandler);
    }

    // Start is called before the first frame update
    void Start()
    {
        ErrorText.gameObject.SetActive(false);        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Display the user data
    /// </summary>
    public void DisplaySWData(){
        
        UsernameInput.text = AppState.CurrentSocialWorker.Data.Username;
        FirstnameInput.text = AppState.CurrentSocialWorker.Data.Firstname;
        LastnameInput.text = AppState.CurrentSocialWorker.Data.Surname;

        Fullname.text = AppState.CurrentSocialWorker.Data.Firstname + " " + 
                        AppState.CurrentSocialWorker.Data.Surname;

        UpdatedUser = (SocialWorker)AppState.CurrentSocialWorker.Data.Clone();

        UserPhoto.RenderProfilePic(AppState.CurrentSocialWorker.Data.ProfilePic);

    }


    // public events

    /// <summary>
    /// Save the profile
    /// </summary>
    public void SaveProfile(){
        UpdatedUser.Firstname = FirstnameInput.text;
        UpdatedUser.Surname = LastnameInput.text;
        UpdatedUser.Username = UsernameInput.text;        

        UpdateButton.RenderBusyState(true);

        SocialWorkerAPIUpdate user = UpdatedUser.ToAPIUpdate();

        PaganiniRestAPI.SocialWorker.Update(user, SaveProfileSucceed, SaveProfileFailed);
    }

    /// <summary>
    /// Cancel the profile
    /// </summary>
    public void CancelProfile(){
        // Cancel the profile
        DisplaySWData();
        ErrorText.gameObject.SetActive(false);
    }

    // private events

    /// <summary>
    /// Handle the photo selected event
    /// </summary>
    /// <param name="socialWorker">The user resource returned by the api</param>
    private void SaveProfileSucceed(SocialWorkerAPIResult socialWorker){

        AppState.CurrentSocialWorker.Data = UpdatedUser;
        AppState.CurrentSocialWorker.NotifyChanges();
        
        DisplaySWData();

        ErrorText.gameObject.SetActive(false);
        UpdateButton.RenderBusyState(false);
    }

    /// <summary>
    /// Handle the photo selected event
    /// </summary>
    /// <param name="error">The error message returned by the api</param>
    private void SaveProfileFailed(string error){
        Debug.Log("Failed to save profile: " + error);
        ErrorText.gameObject.SetActive(true);
        UpdateButton.RenderBusyState(false);
        
        ToastMessageManager.Instance.Toast.RenderAlertToast("Fehler beim Speichern Ihres Profils", 
        "Überprüfen Sie die Internetverbindung und wenden Sie sich an den technischen Support, falls das Problem weiterhin besteht.");            
    }

    /// <summary>
    /// Handle the photo selected event
    /// </summary>
    /// <param name="picTexture">The selected photo texture</param>
    private void PhotoChangedHandler(byte[] picBytes){
        if (picBytes != null) {
            UpdatedUser.ProfilePic = picBytes;
        }
    }

    private void OnDestroy(){
        UserPhoto.OnProfilePicChanged.RemoveListener(PhotoChangedHandler);
    }

}
