using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonPrefab : MonoBehaviour
{
    public Button ButtonObject;
    public GameObject ButtonLabel;
    public GameObject BusyStatus;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    public void RenderBusyState(bool isBusy) {
        BusyStatus.SetActive(isBusy);
        ButtonLabel.SetActive(!isBusy);
        ButtonObject.interactable = !isBusy;
    }

}
