using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VideoGallery : MonoBehaviour
{
    public VideoPlayerPrefab VideoPlayer;

    // Time of playback before POI
    public double BeforePOIVideoPlayback = 10;

    private Pathpoint POIStart;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadVideo(Pathpoint start )
    {
        (float? aspectRatio, bool rotate) = ProcessResolution(AppState.CurrentRoute.LocalVideoResolution);

        POIStart = start;
        VideoPlayer.LoadVideo(FileManagement.persistentDataPath + "/" + AppState.CurrentRoute.LocalVideoFilename, aspectRatio, rotate);
    }

    private (float?, bool) ProcessResolution(string resolutionString)
    {
        float? aspectRatio = null;
        bool rotate = false;
        if (AppState.CurrentRoute.LocalVideoResolution != null)
        {
            string[] resList = resolutionString.Split(":");

            aspectRatio = getAspectRadio(resList[0]);

            if (resList.Length > 1)
            {
                var requestedAspectRatio = getAspectRadio(resList[1]);

                if (aspectRatio != requestedAspectRatio)
                {
                    rotate = true;
                    //aspectRatio = requestedAspectRatio;
                }
            }
        }

        return (aspectRatio, rotate);
    }

    private float getAspectRadio(string resolutionString)
    {        
        string[] res = resolutionString.Split("x");
        if (res.Length == 2)
        {
            float width = float.Parse(res[0]);
            float height = float.Parse(res[1]);

            return width / height;
        }
        return -1;
    }


    /// <summary>
    /// Moves the VideoPlayback to the pathpoint timestamp, so as to
    /// see the video around the current pathpoint
    /// </summary>
    /// <param name = "pathpoint" > Pathpoint to synchronize with</param>
    public void LimitPlaybackTimeframe(Pathpoint currentPOI, Pathpoint nextPOI)
    {        
        double startTime = (currentPOI.Timestamp - POIStart.Timestamp) / 1000;
        double endTime = (nextPOI.Timestamp - POIStart.Timestamp) / 1000;
        double duration = endTime - startTime;

        if (currentPOI.TimeInVideo != null)
        {
            startTime = (double)currentPOI.TimeInVideo;
            endTime = startTime + duration;
        }

        if(nextPOI.TimeInVideo != null)
        {
            endTime = (double)nextPOI.TimeInVideo;
        }

        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(currentPOI.Photos[0].Data.Photo);
        VideoPlayer.SetupPlayback(startTime - BeforePOIVideoPlayback, endTime, texture);
    }

    public void CleanupView()
    {        
        // Unload the video player to release resources
        VideoPlayer.CleanupView();

        Resources.UnloadUnusedAssets();

        // Optionally, you can destroy the GameObject itself if it's no longer needed
        //Destroy(gameObject);
    }
}
