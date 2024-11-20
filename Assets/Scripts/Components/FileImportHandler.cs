using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using SFB = SimpleFileBrowser;
using SimpleFileBrowser;
using Unity.Entities.UniversalDelegates;
using static SynchronizationController;
using System.Runtime.CompilerServices;

public class FileImportHandler : MonoBehaviour
{
    // Change this to the desired directory path on the external volume.

    private string SourceFolderPath;
    private string SourceFolderPathName;

    public SynchronizationController SyncProcess;

    private List<SynchronizationController.DetailedWayExport> RouteList;
    const string TempPathName = "RawImport";
    private string TempPath;

    // Route to import

    SynchronizationController.DetailedWayExport SelectedRoute;


    [Header("UI Configuration")]

    [SerializeField] private GameObject VolumeBrowsePanel;
    [SerializeField] private GameObject VolumeSelectedPanel;
    [SerializeField] private GameObject VolumeCopyingPanel;
    [SerializeField] private GameObject ImportOverviewPanel;
    [SerializeField] private GameObject ImportOverwritePanel;
    [SerializeField] private GameObject ImportProcessingPanel;
    [SerializeField] private GameObject ImportErrorPanel;
    [SerializeField] private GameObject ImportEndPanel;

    [Header("Volume Data UI")]
    [SerializeField] private TMPro.TMP_Text VolumeNameText;
    [SerializeField] private TMPro.TMP_Text ErrorMessageText;
    [SerializeField] private TMPro.TMP_Text LogText;
    [SerializeField] private RouteListPrefab AvailableRoutes;

    [Header("Utility")]
    public PhotoPicker PhotoFilePicker;


    void Start()
    {
        // Start a coroutine to detect volume insertion.

        TempPath = FileManagement.persistentDataPath + "/" + TempPathName;

    }

    public void Initialise()
    {
        // Check if the platform is Android
        if (Application.platform == RuntimePlatform.Android)
        {
            InitialiseAndroid();
        }
        else
        {
            InitialiseEditor();
        }
    }

    public void InitialiseEditor()
    {
        SourceFolderPath = null;
        PhotoFilePicker.OnPhotoSelected.RemoveAllListeners();
        PhotoFilePicker.OnPhotoSelected.AddListener(PathSelectedHandler);
        PhotoFilePicker.PickUpFile(new string[] { NativeFilePicker.ConvertExtensionToFileType("xml") });    
    }    

    public void InitialiseAndroid()
    {
        SourceFolderPath = null;

        StartCoroutine(CheckPathCoroutine());

        try
        {
            SFB.FileBrowser.DirectNativeSAF();
        }
        catch (Exception e)
        {
            Debug.Log(e.StackTrace);
        }

        //StartCoroutine(ShowLoadDialogCoroutine());
    }

    private void PathSelectedHandler(string filePath)
    {
        Debug.Log("PathSelectedHandler() called with path: " + filePath);

        if (!string.IsNullOrEmpty(filePath))
        {
            // Extract the folder path (everything up to the last directory separator)
            SourceFolderPath = Path.GetDirectoryName(filePath);

            // Extract the folder name (the last part of the path)
            SourceFolderPathName = Path.GetFileName(SourceFolderPath);

            Debug.Log($"SourceFolderPathName: {SourceFolderPathName} | SourceFolderPath: {SourceFolderPath}");

            // Update the UI with the selected folder name
            VolumeNameText.text = SourceFolderPathName;

            // Display the screen panel for volume selection
            DisplayScreenPanel(VolumeSelectedPanel);
        }
        else
        {
            Debug.LogWarning("PathSelectedHandler() received a null or empty path.");
        }
    } 

    IEnumerator CheckPathCoroutine()
    {

        Debug.Log("CheckPathCoroutine(): " + (SFB.FileBrowser.GetCurrentPath() == null));
        yield return new WaitForSeconds(1.0f);

        if (SFB.FileBrowser.GetCurrentPath() != null) {
            SourceFolderPathName = SFB.FileBrowser.GetCurrentPathName();
            SourceFolderPath = SFB.FileBrowser.GetCurrentPath();


            Debug.Log($"DestinationFolderName: {SourceFolderPathName} | DestinationFolderPath: {SourceFolderPath} ");

            VolumeNameText.text = SourceFolderPathName;

            DisplayScreenPanel(VolumeSelectedPanel);

        }
        else
        {
            yield return null;
        }

    }


    public void DisplayImportOverview()
    {
        DisplayScreenPanel(VolumeCopyingPanel);
        StartCoroutine(CopyAndShowAvailableRoute());
    }


    IEnumerator CopyAndShowAvailableRoute() {

        SelectedRoute = null;
        string overviewFilename = "waysForExport.xml";

        // create temp directory
        if (Directory.Exists(TempPath))
            Directory.Delete(TempPath, true);
        Directory.CreateDirectory(TempPath);
        try
        {
            //TODO: Does it throw an Exception if the folder does not exist?
            if (Application.platform == RuntimePlatform.Android)
            {
                SFB.FileBrowserHelpers.CopyDirectory(SourceFolderPath, TempPath);
            }
            else 
            {
                CopyDirectory(SourceFolderPath, TempPath);
            }
            
            

            LogText.text += "SourceFolderPath: " + SourceFolderPath + "\n";

            //TODO: Throw exceptions when files / folders are not found
            RouteList = SynchronizationController.DetailedWayExportFiles.ParseDWEFile(overviewFilename, TempPath);

            DisplayScreenPanel(ImportOverviewPanel);

            LogText.text += "Count: " + RouteList.Count + "\n";

            foreach (var item in RouteList)
            {
                Debug.Log($"item:{item.Name}");
                LogText.text = LogText.text + $"item:{item.Name}";

                AvailableRoutes.AddItem(item);
            }
            AvailableRoutes.FinishLoading();

        }
        catch (Exception e)
        {
            ErrorMessageText.text = e.Message;

            LogText.text += "\n" + e.StackTrace;

            DisplayScreenPanel(ImportErrorPanel);

            Debug.LogError(e.StackTrace);
        }

        yield return null;

    }

    void CopyDirectory(string sourceDir, string destDir)
    {
        // Ensure the destination directory exists
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Copy all files in the source directory to the destination directory
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true); // true to overwrite existing files
        }

        // Recursively copy subdirectories
        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string destDirName = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destDirName);
        }
    }    


    public void OnRouteSelected(Way w, Route r) {

        // Do we need to prompt an overwrite dialog?
        bool alreadyExists = SyncProcess.CheckIfOverwriteRequired(r.Id);

        foreach (var item in RouteList)
        {
            if (item.Id == r.Id) {
                SelectedRoute = item;
            }
        }

        if (alreadyExists)
        {
            DisplayOverwrite();
            return;
        }

        if (SelectedRoute != null) {
            StartFilesImport(r.Id, SelectedRoute.RecordingName);
        }
        else
        {
            Debug.Log($"Import folder '{SelectedRoute.RecordingName}' not found.");
        }

        
    }

    /// <summary>
    /// Resets the view, when the sync process is midway
    /// </summary>
    public void ResetSyncView()
    {
        DisplayScreenPanel(VolumeBrowsePanel);
    }


    private void DisplayScreenPanel(GameObject panel) {
        VolumeBrowsePanel.SetActive(VolumeBrowsePanel == panel);
        VolumeSelectedPanel.SetActive(VolumeSelectedPanel == panel);
        VolumeCopyingPanel.SetActive(VolumeCopyingPanel == panel);
        ImportOverviewPanel.SetActive(ImportOverviewPanel == panel);
        ImportOverwritePanel.SetActive(ImportOverwritePanel == panel);
        ImportProcessingPanel.SetActive(ImportProcessingPanel == panel);
        ImportErrorPanel.SetActive(ImportErrorPanel == panel);
        ImportEndPanel.SetActive(ImportEndPanel == panel);
    }


    private void DisplayOverwrite() {
        DisplayScreenPanel(ImportOverwritePanel);
    }


    private void SuccessullyFinished() {
        DisplayScreenPanel(ImportEndPanel);
    }


    public void ConfirmOverwrite() {
        StartFilesImport(SelectedRoute.Id, SelectedRoute.RecordingName);
    }

    public void CancelOverwrite()
    {
        ErrorMessageText.text = "Import cancelled.";

        DisplayScreenPanel(ImportErrorPanel);
    }


    public void StartFilesImport(int id, string folder)
    {
        Debug.Log($"Selected Id: '{id}' Folder: {folder}.");

        DisplayScreenPanel(ImportProcessingPanel);

        try {
            SyncProcess.SyncFromImportedFolder(id, TempPath, folder);
            SuccessullyFinished();
        }
        catch (Exception e) {
            ErrorMessageText.text = "Import Error: " + e.Message;

            DisplayScreenPanel(ImportErrorPanel);

            Debug.LogError(e.StackTrace);

        }
        
    }

}
