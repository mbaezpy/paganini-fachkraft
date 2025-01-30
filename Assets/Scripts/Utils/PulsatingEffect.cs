using UnityEngine;
using UnityEngine.UI;
using Unity.VectorGraphics;

public class PulsatingEffect : MonoBehaviour
{
    public float pulseSpeed = 1.0f;
    public float minAlpha = 0.0f;
    public float maxAlpha = 1.0f;

    private Image image;
    private CanvasRenderer canvasRenderer;
    private bool increasing = true;

    private void Start()
    {
        image = GetComponent<Image>();
        canvasRenderer = GetComponent<CanvasRenderer>();

        if (canvasRenderer == null)
        {
            canvasRenderer = gameObject.AddComponent<CanvasRenderer>();
        }
    }

    private void Update()
    {
        if (image != null && canvasRenderer != null && enabled)
        {
            float alpha = Mathf.PingPong(Time.time * pulseSpeed, 1.0f);
            alpha = Mathf.Lerp(minAlpha, maxAlpha, alpha);

            Color color = image.color;
            color.a = alpha;

            image.color = color;
            canvasRenderer.SetAlpha(color.a);
        }
    }

    public void StopPulsating()
    {
        if (image != null && canvasRenderer != null) {
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1.0f);
            canvasRenderer.SetAlpha(1.0f);
        }            
        enabled = false;
        gameObject.SetActive(false);
    }

    public void ResumePusalting(){
        enabled = true;
        gameObject.SetActive(true);
    }
}
