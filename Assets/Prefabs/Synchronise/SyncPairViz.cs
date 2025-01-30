using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SyncPairViz : MonoBehaviour
{
    [Header("Data")]
    public TMPro.TMP_Text PhoneName;
    public TMPro.TMP_Text TabletName;
    public GameObject ConnectionLink;


    [Header("Pairing viz")]
    public bool PairingToPhone = true;
    public GameObject PairingCue;
    public Image PairedIconBackground;
    public Image PairedNameBackground;
    public PulsatingEffect PulsatingIcon;
    public Color ConnectedBackgroundColor;
    
    private Color originalNameBackgroundColor;
    private Color originalIconBackgroundColor;
    


    void Start()
    {
        InitValues();          
    }

    private void InitValues(){
        if (PairingToPhone){
            TabletName.text = AppState.CurrentSocialWorker.Data.Firstname;
        }
        else 
        {
            PhoneName.text = AppState.CurrentUser.Mnemonic_token;
        }
        
        originalNameBackgroundColor = PairedNameBackground.color;
        originalIconBackgroundColor = PairedIconBackground.color;   
    }

    public void RenderStandBy(){
        InitValues();

        ConnectionLink.SetActive(false);
        PulsatingIcon.StopPulsating();
        PairedNameBackground.gameObject.SetActive(false);
    }

    public void RenderSearching(){
        ConnectionLink.SetActive(true);
        PulsatingIcon.ResumePusalting();
        PairedNameBackground.gameObject.SetActive(false);
    }

    public void RenderFoundPhone(string phoneName){
        ConnectionLink.SetActive(false);
        PairingCue.SetActive(false);
        PhoneName.gameObject.SetActive(true);
        PhoneName.text = phoneName;

        PairedNameBackground.gameObject.SetActive(true);
        PairedNameBackground.color = ConnectedBackgroundColor;
        PairedIconBackground.color = originalIconBackgroundColor;
        
        PulsatingIcon?.StopPulsating();
    }

    public void RenderConnecting(){
        PairingCue.SetActive(true);
        TabletName.gameObject.SetActive(false);
    }


    public void RenderTransfer(){
        ConnectionLink.SetActive(true);
        PulsatingIcon?.StopPulsating(); 
    }

    public void RenderPairingToTablet(string pairedToTablet)
    {
        PairingCue.SetActive(false);
        TabletName.gameObject.SetActive(true);
        TabletName.text = pairedToTablet;
        PairedNameBackground.color = ConnectedBackgroundColor;
        PairedIconBackground.color = originalIconBackgroundColor;
        
        PulsatingIcon?.StopPulsating();
    }

    public void RenderPairingToPhone(string pairedToPhone){
        PairingCue.SetActive(false);
        PhoneName.gameObject.SetActive(true);
        PhoneName.text = pairedToPhone;
        PairedNameBackground.color = ConnectedBackgroundColor;
        PairedIconBackground.color = originalIconBackgroundColor;
        
        PulsatingIcon?.ResumePusalting();        
    }

    public void RenderPairingAccepted(){
        PairedNameBackground.color = originalNameBackgroundColor;
        PairedIconBackground.color = ConnectedBackgroundColor;
    }

    public void RenderError(){
        PulsatingIcon?.StopPulsating(); 
        PairingCue.SetActive(false);
    }
    
}

