using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RouteOnboarding : MonoBehaviour
{
    [Header("UI States")]
    public GameObject LoadingState;
    public ButtonPrefab OkButton;

    [Header("Onboarding Screens")]    
    public GameObject OnboardDiscussion;
    public GameObject OnboardCleaning;
    public GameObject OnboardTraining;
    

    private RouteSharedData SharedData;

   



    // Start is called before the first frame update
    void Awake()
    {
        SharedData = RouteSharedData.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadBusyView() {        
        gameObject.SetActive(true);
        LoadingState.SetActive(false);
        OkButton.RenderBusyState(true);
        PopulateRouteOnboarding();
        
    }

    public void LoadReadyView()
    {
        OkButton.RenderBusyState(false);
    }

    /// <summary>
    /// Populates the Route Onboarding view based on the status of the Route
    /// </summary>
    public void PopulateRouteOnboarding()
    {
        //SharedData current route

        OnboardCleaning.SetActive(SharedData.CurrentRoute.Status == Route.RouteStatus.New);
        OnboardDiscussion.SetActive(SharedData.CurrentRoute.Status == Route.RouteStatus.DraftPrepared);
        OnboardTraining.SetActive(SharedData.CurrentRoute.Status == Route.RouteStatus.Training);
    }
}
