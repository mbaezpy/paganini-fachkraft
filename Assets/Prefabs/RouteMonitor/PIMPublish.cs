using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;



public class PIMPublish : MonoBehaviour
{

    [Header("UI States")]
    public GameObject DataState;
    public GameObject ErrorState;

    [Header("UI Controls")]
    public ToggleGroup ProcessStepGroup;
    public ButtonPrefab ButtonSave;

    public UnityEvent OnPublishDone;

    private Route CurrentRoute;

    private RouteWalkSharedData WalkSharedData;

    

    void Awake()
    {
        WalkSharedData = RouteWalkSharedData.Instance;        
        WalkSharedData.OnDataUploaded += WalkSharedData_OnDataUploaded;
        WalkSharedData.OnDataUploadError += WalkSharedData_OnDataUploadError;
    }


    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadView()
    {
        LoadView(DataState);
    }


    public void LoadSaveView()
    {
        LoadView(DataState);
    }


    bool IsUploadChangesSelected()
    {

        foreach (Toggle toggle in ProcessStepGroup.ActiveToggles())
        {
            if (toggle.name.Contains("Draft"))
            {
                return false;
            }
            else //if (toggle.name.Contains("Training"))
            {
                return true;
            }

        }

        return false;

    }


    public void DeleteLocalPIMChanges()
    {
        bool[] options = new bool[] { false } ;

        PathpointPIM.DeleteFromRoute(WalkSharedData.CurrentRoute.Id, fromAPI : options);

        OnPublishDone?.Invoke();
    }


    public void ConfirmSelection()
    {
        ButtonSave.RenderBusyState(true);

        var upload = IsUploadChangesSelected();

        if (upload) {
            WalkSharedData.UploadLocalPIM();
        }
        else
        {
            OnPublishDone?.Invoke();
        }


        
    }

    private void WalkSharedData_OnDataUploadError(object sender, string message) {
        LoadView(ErrorState);
    }

    private void WalkSharedData_OnDataUploaded(object sender, EventArgs eventArgs)
    {

        // Flag route as not in draft

        OnPublishDone?.Invoke();
    }

    private void LoadView(GameObject view)
    {
        DataState.SetActive(view == DataState);
        ErrorState.SetActive(view == ErrorState);
    }


    private void OnDestroy()
    {
        WalkSharedData.OnDataUploadError-=WalkSharedData_OnDataUploadError;
        WalkSharedData.OnDataUploaded -= WalkSharedData_OnDataUploaded;
    }
}
