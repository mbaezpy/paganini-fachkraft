using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using Unity.VectorGraphics;

public class ClickEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image imageObject;  // The Image GameObject to apply the effect.
    public TMPro.TMP_Text textObject;   // The Text GameObject to apply the effect.
    public GameObject Wrapper;
    public SVGImage ClickBackground;

    private float scaleReduction = 0.95f;  // Scale reduction when pressed (0.9 means 90% of the original size).
    public Color pressedColor = Color.black;      // Color to change to when pressed.

    private Vector3 originalScale;   // Original scale of the GameObject.
    private Color? originalImageColor; // Original color of the Image.
    private Color? originalTextColor;  // Original color of the Text.

    

    private void Start()
    {
        // Store the original scale and colors.
        originalScale = Wrapper.transform.localScale;
        originalImageColor = imageObject?.color;
        originalTextColor = textObject?.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Apply the click effect when the GameObject is pressed.
        Wrapper.transform.localScale = originalScale * scaleReduction;
        if (imageObject!= null) imageObject.color = pressedColor;
        if (textObject != null) textObject.color = pressedColor;

        if (ClickBackground!=null) ClickBackground.enabled = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset to the original scale and colors when the click is released.
        Wrapper.transform.localScale = originalScale;
        if (imageObject != null) imageObject.color = (Color)originalImageColor;
        if (textObject != null)  textObject.color = (Color)originalTextColor;

        if (ClickBackground != null) ClickBackground.enabled = false;
    }
}
