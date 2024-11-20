using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using SFB = SimpleFileBrowser;

public class PhotoPicker : MonoBehaviour
{

    [Header("Events")]
    /// <summary>
    /// Event triggered when a photo is selected.
    /// </summary>
    /// <param name="photoPath">The file path of the selected photo. Null if none selected. </param>
    public UnityEvent<string> OnPhotoSelected; 


    private string SourceFolderPath;
    private string SourceFolderPathName;    

    public void PickUpImage()
    {
        string[] fileTypes = new string[] { "image/*" };
        // Initiates the async file picking process
        PickupFileAsync(fileTypes).ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError("Error in picking file: " + task.Exception);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensures errors are caught on the main thread
    }

    public void PickUpFile(string[] fileTypes)
    {
        // Initiates the async file picking process
        PickupFileAsync(fileTypes).ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError("Error in picking file: " + task.Exception);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensures errors are caught on the main thread
    }


    private async Task PickupFileAsync(string[] fileTypes)
    {
        // Request permission asynchronously
        NativeFilePicker.Permission permission = await NativeFilePicker.RequestPermissionAsync(readPermissionOnly: true);
        Debug.Log("Permission result: " + permission);

        if (permission != NativeFilePicker.Permission.Granted)
        {
            Debug.Log("Permission denied");
            return;
        }

        // File type filter for image files
        
        
        // Use a TaskCompletionSource to await the result from the picker
        var tcs = new TaskCompletionSource<string>();
        
        // Start file picking
        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("Operation cancelled");
                tcs.SetResult(null); // Indicate cancellation
            }
            else
            {
                Debug.Log("Picked file: " + path);
                tcs.SetResult(path); // Set the picked file path as the result
            }
        }, fileTypes);

        // Await the result from TaskCompletionSource
        string selectedPath = await tcs.Task;

        // Trigger event if a file was successfully picked // null if none was picked
        OnPhotoSelected?.Invoke(selectedPath);


    }




}
