using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPrefab : MonoBehaviour
{
    public Button ButtonObject;
    public GameObject ButtonLabel;
    public GameObject BusyStatus;

    [Header(@"Pressed Effect")]
    public bool PressedEffect = true;
    public bool BusyOnClicked = false;
    public GameObject PressedObject;
    public GameObject NormalObject;
    public GameObject ToggleObject = null;

    [Header(@"Text Pressed Effect")] 
    public bool FontPressedEffect = false;   
    public TMPro.TMP_Text FontPressedObject;
    public Image IconFillColorPressed;
    public Color FontPressedColor = Color.white;

    [Header(@"Vibration Effect")]    
    public bool VibrateOnPressed = false;

    [Header(@"Disable Effect")]    
    public Color BackgroundDisableColor = Color.gray;
    public Color OutlineDisableColor = Color.gray;
    public Color FontDisableColor = Color.white;

    [Header(@"Events")]
    [Tooltip("Event to be invoked when the button is clicked, avoiding misfiring in scroll events")]
    public UnityEvent OnProperClick; // Event to be invoked when the button is clicked

    private Vector3 originalAnchoredPosition;
    private bool positionStored = false; // Prevent multiple overwrites    
    private Color? originalFontColor = null;
    private Color? originalBackroundColor = null;
    private Color? originalOutlineColor = null;
    private Color? originaIconColor = null;
    private bool? isInteractable = null;
    private bool isBusy = false;

    // scroll detection variables
    private bool isDragging = false; // Track if the user is dragging
    private Vector2 pointerDownPosition; // Track the initial pointer down position
    private const float dragThreshold = 10f; // Distance in pixels to consider as drag

    

    void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        RegisterButtonPressedEvent();
    }

    // Update is called once per frame
    void Update()
    {
        if (isBusy) return;

        if (ButtonObject.interactable != isInteractable){
            isInteractable = ButtonObject.interactable;
            RenderDisable(!(bool)isInteractable);
        }
    }

    public void RenderBusyState(bool isBusy) {
        this.isBusy = isBusy;
        if (isBusy){
            OnButtonPressed(forceIfDisabled: true);
        }
        else{
            OnButtonReleased(forceIfDisabled: true);
        }

        if (BusyStatus !=null) BusyStatus.SetActive(isBusy);
        if (ButtonLabel !=null) ButtonLabel.SetActive(!isBusy);
        
        ButtonObject.interactable = !isBusy;                
    }

 private void RegisterButtonPressedEvent()
    {
        if (ButtonObject == null) return;

        EventTrigger trigger = ButtonObject.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = ButtonObject.gameObject.AddComponent<EventTrigger>();
        }

        // Create entry for pointer down event
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((eventData) => OnPointerDown((PointerEventData)eventData));
        trigger.triggers.Add(pointerDownEntry);

        // Create entry for pointer up event
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((eventData) => OnPointerUp((PointerEventData)eventData));
        trigger.triggers.Add(pointerUpEntry);

        // Create entry for drag event
        EventTrigger.Entry dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener((eventData) => OnDrag((PointerEventData)eventData));
        trigger.triggers.Add(dragEntry);

        // OnClick
        ButtonObject.onClick.AddListener(OnButtonClicked);
    }

    private void OnPointerDown(PointerEventData eventData)
    {
        isDragging = false;
        pointerDownPosition = eventData.position;
        OnButtonPressed(); // Provide visual feedback for button press                
    }

    public void OnDrag(PointerEventData eventData)
    {        
        // Calculate the distance between the pointer down position and the current position
        float distance = Vector2.Distance(pointerDownPosition, eventData.position);
        if (distance > dragThreshold)
        {
            isDragging = true; // User is dragging, not tapping
        }

    }


    private void OnPointerUp(PointerEventData eventData)
    {
        OnButtonReleased();          
    }


    public void OnButtonPressed(bool forceIfDisabled = false, bool forceFontEffect = false, Color? fontPressedColor = null)
    {
        if (!forceIfDisabled && !ButtonObject.interactable) return;
        if (!positionStored)
        {
            RectTransform rectTransform = ButtonLabel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                originalAnchoredPosition = rectTransform.anchoredPosition; // Store the anchored position
                positionStored = true;
                rectTransform.anchoredPosition = PressedObject.GetComponent<RectTransform>().anchoredPosition;
                RenderPressed(true);
            }
            else
            {
                Debug.LogWarning("ButtonLabel does not have a RectTransform.");
            }
        }

        if (FontPressedEffect || forceFontEffect)
        {
            if (FontPressedObject != null)
            {
                originalFontColor = originalFontColor ?? FontPressedObject.color;
                FontPressedObject.color = fontPressedColor ?? FontPressedColor;
            }

            if (IconFillColorPressed != null)
            {
                originaIconColor = originaIconColor ?? IconFillColorPressed.color;
                IconFillColorPressed.color = fontPressedColor ?? FontPressedColor;
            }

        }        
    }

    public void OnButtonReleased(bool forceIfDisabled = false, bool forceFontEffect = false)
    {
        if (!forceIfDisabled && !ButtonObject.interactable) return;

        if (positionStored)
        {
            RectTransform rectTransform = ButtonLabel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition; // Restore the original anchored position
                positionStored = false; // Reset the flag
                RenderPressed(false);
            }
        }

        if ((FontPressedEffect || forceFontEffect) && originalFontColor != null)
        {
            if (FontPressedObject != null)
            {                
                FontPressedObject.color = (Color)originalFontColor;
            }

            if (IconFillColorPressed != null)
            {
                IconFillColorPressed.color = (Color)originaIconColor;
            }
        }
    }    

    public void OnButtonClicked()
    {
        Debug.Log("OnButtonClicked");
        if (isDragging) {
            Debug.Log("OnButtonClicked - Dragging detected, ignoring click event");
            return;
        }

        if (BusyOnClicked){
            RenderBusyState(true);
        }

        if (VibrateOnPressed){
            //HapticUtils.VibrateForClick();
        }

        OnProperClick?.Invoke();
        
    }

    private void RenderPressed(bool pressed){
        PressedObject.SetActive(pressed);
        NormalObject.SetActive(!pressed);       
        if (ToggleObject != null) {
            ToggleObject.SetActive(pressed);
        } 
        
    }

    private void RenderDisable(bool disabled){
        if (disabled){
            OnButtonPressed(forceIfDisabled: true, forceFontEffect: true, fontPressedColor: FontDisableColor);
        }
        else{
            OnButtonReleased(forceIfDisabled: true, forceFontEffect: true);
        }
        
        RenderImageDisabled(PressedObject.GetComponent<Image>(), disabled);
    }


    private void RenderImageDisabled(Image image, bool disabled){
        if (image != null)
        {
            originalBackroundColor = originalBackroundColor ?? image.color;
            image.color = disabled ? BackgroundDisableColor : (Color)originalBackroundColor;

            // Outline
            if (image.GetComponent<Outline>() != null)
            {
                originalOutlineColor = originalOutlineColor ?? image.GetComponent<Outline>().effectColor;
                image.GetComponent<Outline>().effectColor = disabled ? OutlineDisableColor : (Color)originalOutlineColor;
            }

        }
    }

    void OnDestroy()
    {
        if (ButtonObject == null) return;

        EventTrigger trigger = ButtonObject.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers.Clear();
        }

        ButtonObject?.onClick.RemoveListener(OnButtonClicked);
    }

}
