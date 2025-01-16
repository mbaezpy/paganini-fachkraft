using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button ButtonOpen;
    public Button ButtonClose;
    public GameObject Menu;

    public GameObject MenuOverlay;

    // SW Profile information
    public RawImage SWPhoto;
    public TMPro.TMP_Text SWName;

    public RawImage UserPhoto;
    public TMPro.TMP_Text UserName;   


    public static class MenuOptions{
        public const string LOGOUT = "OptionLogout";
        public const string SWITCH_USER = "OptionSwitchUser";
        public const string SW_PROFILE = "OptionSWProfile";
    }


    // Start is called before the first frame update
    void Start()
    {
        ButtonOpen.onClick.AddListener(OpenMenu);
        ButtonClose.onClick.AddListener(CloseMenu);

        CloseMenu();

        UpdateSWProfile();

        UpdateUserProfile();

        SetupMenuOptions();

        AppState.CurrentSocialWorker.OnDataChanged.AddListener(UpdateSWProfile);
    }


    // public events
    public void Logout()
    {
        AuthToken.DeleteAll();
        AppState.ResetValues();
        SceneSwitcher.LoadLogin();
    }    

    public void SwitchUser(){
        AppState.CurrentMenuOption = MenuOptions.SWITCH_USER;
        SceneSwitcher.LoadUserManager();        
    }

    public void OpenUserProfile(){
        AppState.CurrentMenuOption = null;
        SceneSwitcher.LoadUserProfile();
    }

    public void OpenSWProfile(){
        AppState.CurrentMenuOption = null;
        SceneSwitcher.LoadSWProfile();
    }

    // private events

    private void UpdateSWProfile()
    {
        if (gameObject.activeInHierarchy) {

            PictureUtils.RenderPicture(SWPhoto, AppState.CurrentSocialWorker.Data.ProfilePic);
            SWName.text = AppState.CurrentSocialWorker.Data.Firstname + " " + AppState.CurrentSocialWorker.Data.Surname;                

            //Debug.Log("SW Profile Updated" + gameObject.transform.parent.parent.name);   
        }                  
    }

    private void UpdateUserProfile(){
        if (AppState.CurrentUser!=null){
            PictureUtils.RenderPicture(UserPhoto, AppState.CurrentUser.ProfilePic);
            UserName.text = AppState.CurrentUser.Mnemonic_token;
        }        
    }

    private void OpenMenu()
    {
        Menu.SetActive(true);
        MenuOverlay.SetActive(true);  
        RenderCanOpen(false);     
    }

    public void CloseMenu()
    {
        Menu.SetActive(false);
        MenuOverlay.SetActive(false);
        RenderCanOpen(true);        
    }  

    public void SetCurrentOption(string option)
    {
        AppState.CurrentMenuOption = option;

        foreach (MainMenuOption menuOption in Menu.GetComponentsInChildren<MainMenuOption>())
        {
            if (menuOption.name == option){
                menuOption.EnableOption(false);
            }
            
        }
    }

    // Attach OptionSelected to all the MainMenuOption buttons in the menu
    private void SetupMenuOptions()
    {
        foreach (MainMenuOption option in Menu.GetComponentsInChildren<MainMenuOption>())
        {
            option.OnOptionSelected.AddListener(OptionSelected);

            if (option.name == AppState.CurrentMenuOption)
            {
                option.EnableOption(false);
            }            
        }
    }

    // Save the name of the option selected
    private void OptionSelected(string option)
    {
        AppState.CurrentMenuOption = option;
    }

    private void OnDestroy()
    {
        ButtonOpen.onClick.RemoveAllListeners();
        ButtonClose.onClick.RemoveAllListeners();

        AppState.CurrentSocialWorker.OnDataChanged.RemoveListener(UpdateSWProfile);  

        if (SWPhoto.texture != null)
        {
            DestroyImmediate(SWPhoto.texture, true);
        }

        if (UserPhoto.texture != null)
        {
            DestroyImmediate(UserPhoto.texture, true);
        }        

        foreach (MainMenuOption option in Menu.GetComponentsInChildren<MainMenuOption>())
        {
            option.OnOptionSelected.RemoveAllListeners();
        }
    }

    private void RenderCanOpen(bool canOpen){
        ButtonOpen.gameObject.SetActive(canOpen);
        ButtonClose.gameObject.SetActive(!canOpen);    
    }

}
