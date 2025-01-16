using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayerPrefab : MonoBehaviour
{
    public VideoPlayer VideoManager;

    [Header("UI States")]
    public GameObject DataState;
    public GameObject BlankState;

    [Header("Video Controllers")]
    public GameObject PlayControl;
    public GameObject ReplayControl;
    public GameObject FwdControl;
    public GameObject BackControl;
    public GameObject PreviewWrapper;
    public GameObject VideoWrapper;
    public GameObject ControlWrapper;
    public RawImage Preview;

    private double StartTimestamp;
    private double EndTimestamp;
    private bool awaitingPlaybackAction;
    private bool VideoRotated = false;


    // Start is called before the first frame update
    void Start()
    {
      //  VideoManager.prepareCompleted += OnVideoPrepared;
      //  VideoManager.seekCompleted += OnSeekCompleted;
      
    }

    // Update is called once per frame
    void Update()
    {
        // We wait until the replay or skip action is performed by the video player,
        // otherwise, it would bring up the end of video screen again in the next frame
        if (awaitingPlaybackAction && VideoManager.time < EndTimestamp) {
            awaitingPlaybackAction = false;
        }
        else if (awaitingPlaybackAction)
        {
            return;
        }

        if (VideoManager.time >= EndTimestamp && VideoManager.isPlaying)
        {
            EnableReplayControl(true);
            EnableVideoControls();
        }

    }


    public void SetupPlayback(double startTime, double endTime, Texture2D preview = null)
    {
        Debug.Log("SkipToVideoFrame from: " + VideoManager.time + "o Timestamp: " + startTime);
       
        VideoManager.time = startTime;

        StartTimestamp = startTime;
        EndTimestamp = endTime;

        awaitingPlaybackAction = false;
        videoJustLoaded = true;

        // Default controls when we load the video
        EnableReplayControl(false);
        FwdControl.SetActive(false);
        BackControl.SetActive(false);

        if (!VideoManager.isPrepared)
        {
            Debug.Log("Video not prepared yet, waiting for it to be ready");
            VideoManager.prepareCompleted -= OnVideoPrepareCompleted;
            VideoManager.prepareCompleted += OnVideoPrepareCompleted;
            VideoManager.Play();            
        }

        // Remove previous texture
        if (Preview.texture != null)
        {
            Destroy(Preview.texture);
        }
        

        if (preview != null)
        {
            PreviewWrapper.SetActive(true);
            Preview.gameObject.SetActive(true);
            Preview.texture = preview;
        }
        else
        {
            Preview.gameObject.SetActive(false);
        }        
    }

    private bool videoJustLoaded = true;
    public void EnableVideoControls() {

        Debug.Log("EnableVideoControls   time: " + VideoManager.time + "- StartTimestamp" + StartTimestamp + " - EndTimestamp" + EndTimestamp + " - videoJustLoaded: " + videoJustLoaded); 

        VideoManager.Pause();
        ControlWrapper.SetActive(true);
        
        FwdControl.SetActive(VideoManager.time < EndTimestamp && !videoJustLoaded);
        BackControl.SetActive(VideoManager.time > StartTimestamp && !videoJustLoaded);

        EnableReplayControl(VideoManager.time >= EndTimestamp);

        videoJustLoaded = false;
    }

    public void ResumePlayback() {
        VideoManager.Play();
        PreviewWrapper.SetActive(false);
        ControlWrapper.SetActive(false);
    }

    public void Replay()
    {
        // Takes some time (frames) to actually play
        awaitingPlaybackAction = true;
        VideoManager.time = StartTimestamp;        
        ControlWrapper.SetActive(false);

        EnableReplayControl(false);

        VideoManager.Play();

    }

    public void SkipForward() {
        //double playtime = VideoManager.clip.length;
        SkipTimeAndPlay(10);
    }

    public void SkipBackwards() {
        SkipTimeAndPlay(-10);
    }

    private void SkipTimeAndPlay(double skipTime)
    {
        awaitingPlaybackAction = true;
        double targetTime = VideoManager.time + skipTime;
        // Let's check the boundaries of our playback
        if (skipTime>0) {
            skipTime = targetTime > EndTimestamp ?  EndTimestamp - VideoManager.time : skipTime;
        } else {
            skipTime = targetTime < StartTimestamp ? StartTimestamp - VideoManager.time  : skipTime;
        }
        

        VideoManager.time += skipTime;
        VideoManager.Play();
        ControlWrapper.SetActive(false);
    }

    private void OnVideoPrepareCompleted(VideoPlayer player)
    {
        Debug.Log("Video prepared!");

        VideoManager.prepareCompleted -= OnVideoPrepareCompleted;
        VideoManager.time = StartTimestamp;
        
        EnableVideoControls();
    }

    private void EnableReplayControl(bool activate) {
        ReplayControl.SetActive(activate);
        PlayControl.SetActive(!activate);        
    }


    public void LoadVideo(string url, float? aspectRatio = null, bool rotate = false)
    {
        if (File.Exists(url)) {
            BlankState.SetActive(false);
            DataState.SetActive(true);

            VideoRotated = rotate;

            if (rotate)
            {
                var t = VideoManager.gameObject.GetComponent<RectTransform>();
                t.localRotation = Quaternion.Euler(0, 0, 90); // Rotate 90 degrees clockwise
            }

            if (VideoManager != null)
            {
                VideoManager.url = url;
                VideoManager.errorReceived -= OnVideoError;
                VideoManager.errorReceived += OnVideoError;
                if (aspectRatio == null)
                {
                    StartCoroutine(DetectVideoResolution());
                }
                else
                {
                    SetAspectRatioToVideo(VideoManager.gameObject, (float)aspectRatio);
                }
                
            }
        } else {
            DataState.SetActive(false);
            BlankState.SetActive(true);
        }

    }

    private IEnumerator DetectVideoResolution()
    {
        yield return new WaitUntil(() => VideoManager.isPrepared); // Wait until the video is prepared.

        long width = VideoManager.width;   // Width of the video
        long height = VideoManager.height; // Height of the video

        Debug.Log("Video Resolution: " + width + "x" + height);

        float videoAspectRatio = (float) VideoManager.width / VideoManager.height;

        SetAspectRatioToVideo(VideoManager.gameObject, videoAspectRatio);

    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogError("Video error: " + message);
        ToastMessageManager.Instance.Toast.RenderAlertToast("Fehler beim Laden des Videos", message);
    }

    public void SetAspectRatioToVideo(GameObject targetObject, float videoAspectRatio)
    {
        var wrapperTransform = VideoWrapper.GetComponent<RectTransform>();

        var refMeasure = wrapperTransform.sizeDelta.y;

        float height = refMeasure;
        float width = height * videoAspectRatio;


        Vector3 newScale = targetObject.transform.localScale;
        newScale.y = height;
        newScale.x = width;        

        if (VideoRotated)
        {
            newScale.y = width;
            newScale.x = height; 
        }

        targetObject.transform.localScale = newScale;

        wrapperTransform.sizeDelta = new Vector2(width, height);
    }


    public void CleanupView()
    {
        // Unsubscribe from events to prevent memory leaks
        VideoManager.prepareCompleted -= OnVideoPrepareCompleted;

        // Unload the video to release its resources
        VideoManager.url = null;

        // Clear any loaded preview texture
        if (Preview.texture != null)
        {
            DestroyImmediate(Preview.texture, true);
            Preview.texture = null;
        }

        // Deactivate any active game objects
        PreviewWrapper.SetActive(false);
        ControlWrapper.SetActive(false);

        // Reset variables to their initial state
        StartTimestamp = 0;
        EndTimestamp = 0;
        awaitingPlaybackAction = false;
        videoJustLoaded = true;
    }

}
