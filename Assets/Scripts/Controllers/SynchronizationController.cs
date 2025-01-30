using Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;
using XCharts.Runtime;
using static FTSCore;
using static InternalDataModel;



public class SynchronizationController : MonoBehaviour
{

    public RouteListPrefab SyncOverviewList;
    //public GameObject TextOverviewPanelAskForConnection;
    public GameObject LoadingIconAskForConnection;

    public FileTransferServer FTS;

    [Header("Import UI Methods")]
    public GameObject UI_Method_View;
    public GameObject UI_Method_Phone;
    public GameObject UI_Method_USBStick;

    [Header("Synchronisation Screens")]
    public GameObject UI_PanelStart;
    public GameObject UI_PanelSearch;
    public GameObject UI_PanelFound;
    public GameObject UI_PanelAskForConnection;
    public GameObject UI_PanelRouteReading;
    public GameObject UI_SyncOverview;
    public GameObject UI_PanelFileTransferrunning;
    public GameObject UI_PanelProcessingData;
    public GameObject UI_PanelEnd;
    public GameObject UI_Overwrite;
    public GameObject UI_PanelError;
    public GameObject UI_PanelDenied;

    [Header("Sync UI Elements")]
    public GameObject UI_PairingViewOverlay;
    public SyncPairViz  PairingViz;

    public TMPro.TMP_Text TextLog;


    [Header("GPS Cleaning Configuration")]
    public double ToleranceSimplify = 0.001;
    public double MaxAccuracyRadio = 15;
    public double DistanceOutlier = 100;
    public int SegmentSplit = 300;
    public double OutlierFactor = 1.5;
    public double MinEvenly = 4;
    public double MaxEvenly = 8;
    public double POIClusterDistance = 10;

    bool wasSendPollRequestCalled = false;

    List<string> logs = new List<string>();


    List<DetailedWayExportFiles> FileListToSyncronize;
    List<DetailedWayExportFiles> FileListProcessed;
    FTSCore.FileRequest fileInfo;

    string currentWayFolderName;

    DataOfImportedERW erw;
    DetailedWayExport selectedDwe;

    // Status of the connection with the remote device
    private bool isCurrentDeviceConnected;
    private int connectionAttemps = 0;
    private DateTime lastHeartbeat = System.DateTime.Now;

    private string currentDeviceName;
    private SyncStatus CurrentStatus;

    // This are the internal protocol states. This is used to verify
    // that incoming (protocol) request arrive at the proper order
    // (i.e., respecting the state machine of the protocol).
    public enum SyncStatus
    {
        LISTEN = 0,
        WAIT_DEVICE_SELECT = 1,
        WAIT_CONNECT_RESPONSE = 2,
        WAIT_ERW_LIST = 3,
        WAIT_ERW_SELECT = 4,
        WAIT_ERW_PREPARE = 5,
        WAIT_ERW_READY = 6,
        WAIT_ERW_MANIFEST = 7,
        DOWNLOAD = 8,
        DOWNLOAD_READY = 9,
        FINISH = 10,
        CANCEL = 11
    }


    private void Awake()
    {
        DBConnector.Instance.Startup();
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentStatus = SyncStatus.LISTEN;

        // Reset overview list
        ResetOverviewLists();

        // Initialise folder
        ResetSynchronisationFolders();

        //// load ways
        //FillOverviewList();

        Log("SynchronizationHandler - Start() called!");

        // TODO: The social worker profile should be
        // available in the environment. This should
        // be the name of the logged in user
        FTS._deviceName = $"{AppState.CurrentSocialWorker.Data.Firstname} {AppState.CurrentSocialWorker.Data.Surname},SWR";

        SyncState.Instance.TabletName = $"{AppState.CurrentSocialWorker.Data.Firstname} {AppState.CurrentSocialWorker.Data.Surname}";
    }


    // Update is called once per frame
    void Update()
    {
        if (UI_PanelSearch.activeInHierarchy && !wasSendPollRequestCalled)
        {
            wasSendPollRequestCalled = true;
            FTS.SendPollRequest();
        }

    }

    /**********************
     *  Public UI events (and utilities)  *
     **********************/

    /// <summary>
    /// Initialises the UI and resets the synchronisation process
    /// </summary>
    public void InitSyncUI()
    {
        // DisplayScreenPanel(UI_PanelStart);


        // LoadingIconAskForConnection.SetActive(true);
        RenderImportMethodPhone();

        FileListToSyncronize = null;
        FileListProcessed = null;

        ResetSynchronisationFolders();

        ResetOrDisposeProcessProtocol();
        CurrentStatus = SyncStatus.LISTEN;

        logs = new List<string>();

        // We prevent the screen from dimming out while the process is active        
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // SyncState.Instance.ClearValues();
        // SyncState.Instance.TabletName = $"{AppState.CurrentSocialWorker.Data.Firstname} {AppState.CurrentSocialWorker.Data.Surname}";

    }

    /// <summary>
    /// Triggers the synchronization process with the ERW with the given id
    /// </summary>
    /// <param name="erwId">The id of the ERW to synchronize with</param>
    public void SyncWayFromMobilePhone(int erwId)
    {
        string downloadPath = FileManagement.persistentDataPath + "/" + FTS._downloadFolder;

        DisplayScreenPanel(UI_PanelFileTransferrunning);
        PairingViz.RenderTransfer();

        List<DetailedWayExport> listOfWays = DetailedWayExportFiles.ParseDWEFile("waysForExport.xml", downloadPath);

        erw = InternalDataModelController.GetInternalDataModelController().idm.exploritoryRouteWalks.Find(x => x.RouteId.Equals(erwId));

        selectedDwe = listOfWays.Find(x => x.RouteId.Equals(erwId));

        if (erw == null) //NEW AND INSERT
        {
            erw = new DataOfImportedERW();
            RequestSelectedERW();
        }
        else //UPDATE
        {
            currentWayFolderName = erw.Folder;

            DisplayScreenPanel(UI_Overwrite);            
        }

    }

    public bool CheckIfOverwriteRequired(int erwId)
    {
        
        erw = InternalDataModelController.GetInternalDataModelController().idm.exploritoryRouteWalks.Find(x => x.RouteId.Equals(erwId));        

        return erw != null;

    }

    /// <summary>
    /// Synchronise a given ERW from imported folders
    /// </summary>
    /// <param name="erwId">Selected ERW</param>
    /// <param name="sourceFolder">Source folder</param>
    public void SyncFromImportedFolder(int erwId, string importPath, string sourceFolderName)
    {
        List<DetailedWayExport> listOfWays = DetailedWayExportFiles.ParseDWEFile("waysForExport.xml", importPath);
        selectedDwe = listOfWays.Find(x => x.RouteId.Equals(erwId));

        erw = new DataOfImportedERW();

        string sourceFolderPath = importPath + "/" + sourceFolderName;

        // Prepare the files downloaded via the USB
        PrepareFilesToDownload(sourceFolderPath, false);

        // Process the downloaded files
        ProcessDownloadedData(sourceFolderPath);

    }

    /// <summary>
    /// Overwrite the existing ERW, by deleting the local files,
    /// and triggering again the sync process
    /// </summary>
    public void OverwriteRoute()
    {
        // Moved to post-processing, as we want to make sure the process is
        // finished before deleting the previous version
        //Directory.Delete(FileManagement.persistentDataPath + "/" + currentWayFolderName, true);

        RequestSelectedERW();
        DisplayScreenPanel(UI_PanelFileTransferrunning);
        PairingViz.RenderPairingAccepted();
    }

    /// <summary>
    /// Triggers the discovery of remote devices (clients)
    /// </summary>
    public void SearchSmartphone()
    {
        FTS.gameObject.SetActive(true);
        DisplayScreenPanel(UI_PanelSearch);
        PairingViz.RenderSearching();
        
    }

    /// <summary>
    /// Triggers a request for connection to the device selected by
    /// the user.
    /// </summary>
    public void AskForConnection()
    {
        // TODO: we should get the device name as input
        // and set here the currentDeviceName
        // Move Co-routine to the acceptance of the connection request


        // Ask the user selected device for connection
        RequestHandshake();

        // We start a co-routine to check on the connection with
        // the current device
        lastHeartbeat = System.DateTime.Now;
        StartCoroutine(CheckDeviceConnection());

        string[] comp = currentDeviceName.Split(',');
        PairingViz.RenderPairingToPhone(comp[0]);
    }

    /// <summary>
    /// Triggers the cancellation of the synchronisation process
    /// </summary>
    public void CancelSynchronisation()
    {
        RequestCancelProcess();
    }


    /// <summary>
    /// Event that listens to the selection of a route to synchronise
    /// </summary>
    public void OnRouteToSyncSelected(Way w, Route r)
    {
        SyncWayFromMobilePhone(r.Id);
    }


    /// <summary>
    /// Resets the view, when the sync process is midway
    /// </summary>
    public void ResetSyncView()
    {
        if (CurrentStatus == SyncStatus.CANCEL ||
            CurrentStatus == SyncStatus.FINISH) {
            // Do nothing, the process is already over
            
        } else if (CurrentStatus == SyncStatus.LISTEN ||
            CurrentStatus == SyncStatus.WAIT_DEVICE_SELECT ||
            CurrentStatus == SyncStatus.WAIT_CONNECT_RESPONSE) {
            // There is no connection to cancel
            ResetOrDisposeProcessProtocol();
        } else {
            RequestCancelProcess();
            ResetOrDisposeProcessProtocol();
        }

        DisplayScreenPanel(UI_PanelStart);
    }

    /// <summary>
    /// Renders the USB import method view
    /// </summary>
    public void RenderImportMethodUSB()
    {
        RenderMainView(UI_Method_USBStick);
    }

    /// <summary>
    /// Renders the Phone import method view
    /// </summary>
    public void RenderImportMethodPhone() {
        RenderMainView(UI_Method_Phone);
        DisplayScreenPanel(UI_PanelStart);
        PairingViz.RenderStandBy();
    }

    public void RenderImportMethodChoice(){
        RenderMainView(UI_Method_View);
    }

    /// <summary>
    /// Renders the corresponding main view
    /// </summary>
    /// <param name="view"></param>
    public void RenderMainView(GameObject view)
    {
        UI_Method_View.SetActive(UI_Method_View == view);
        UI_Method_Phone.SetActive(UI_Method_Phone == view);
        UI_Method_USBStick.SetActive(UI_Method_USBStick == view);
    }

    /// <summary>
    /// Populates the list of available ERWs to download, as informed by
    /// the remote device (waysForExport.xml)
    /// </summary>
    private void FillSyncOverviewList()
    {
        string downloadPath = FileManagement.persistentDataPath + "/" + FTS._downloadFolder;

        List<DetailedWayExport> listOfWays = DetailedWayExportFiles.ParseDWEFile("waysForExport.xml", downloadPath);

        // Reset overview list
        ResetOverviewLists();

        foreach (var item in listOfWays)
        {
            SyncOverviewList.AddItem(item);
        }
        SyncOverviewList.FinishLoading();


        CurrentStatus = SyncStatus.WAIT_ERW_SELECT;
    }

    /// <summary>
    /// Resets the UI list of local ERWs, clearing out elements
    /// </summary>
    private void ResetOverviewLists()
    {
        SyncOverviewList.Clearlist();
    }

    private void DisplayScreenPanel(GameObject panel) {
        UI_PanelStart.SetActive(UI_PanelStart == panel);
        UI_PanelSearch.SetActive(UI_PanelSearch == panel);
        UI_PanelFound.SetActive(UI_PanelFound == panel);
        UI_PanelAskForConnection.SetActive(UI_PanelAskForConnection == panel);
        UI_PanelRouteReading.SetActive(UI_PanelRouteReading == panel);
        UI_SyncOverview.SetActive(UI_SyncOverview == panel);
        UI_PanelFileTransferrunning.SetActive(UI_PanelFileTransferrunning == panel);
        UI_PanelEnd.SetActive(UI_PanelEnd == panel);
        UI_Overwrite.SetActive(UI_Overwrite == panel);
        UI_PanelError.SetActive(UI_PanelError == panel);
        UI_PanelDenied.SetActive(UI_PanelDenied == panel);

        if (panel != UI_PanelEnd && panel != UI_SyncOverview){
            UI_PairingViewOverlay.SetActive(true);
        }
        else {
            UI_PairingViewOverlay.SetActive(false);
        }
    }




    /****************************************
     *  Pre- and post- processing functions *
     ****************************************/

    /// <summary>
    /// Prepare the list of files to request from the connected device,
    /// as defined by the previously downloaded ERW manifest.xml file.
    /// Once the list is prepared, it proceeds to request the first file.
    /// </summary>
    /// <param name="sourceFolder">The folder where the ERW files are stored</param>
    /// <param name="mobileSync">Whether the synchronisation is being done via mobile phone</param>
    private void PrepareFilesToDownload(string sourceFolder, bool mobileSync = true)
    {

        FileListToSyncronize = new List<DetailedWayExportFiles>();
        FileListProcessed = new List<DetailedWayExportFiles>();

        List<DetailedWayExport> list = DetailedWayExportFiles.ParseDWEFile($"{selectedDwe.Name}-manifest.xml", sourceFolder);
        DetailedWayExport dwe = list.First();

        // fill ERW
        erw.Description = dwe.Description;
        erw.Destination = dwe.Destination;
        erw.DestinationType = dwe.DestinationType;
        erw.Folder = dwe.Folder;
        erw.Id = dwe.Id;        
        erw.Name = dwe.Name;
        erw.Start = dwe.Start;
        erw.StartType = dwe.StartType;
        erw.UserId = dwe.UserId;
        erw.FromAPI = dwe.FromAPI;
        erw.RouteId = dwe.RouteId;
        erw.RecordingDate = dwe.RecordingDate;
        erw.RecordingName = dwe.RecordingName;
        erw.LocalVideoResolution = dwe.LocalVideoResolution;
        erw.StartTimestamp = dwe.StartTimestamp;
        erw.EndTimestamp = dwe.EndTimestamp;
        erw.SocialWorkerId = dwe.SocialWorkerId ;


        erw.Videos = new List<string>();
        erw.Photos = new List<string>();

        currentWayFolderName = erw.Folder;

        foreach (var file in dwe.Files)
        {
            FileListToSyncronize.Add(file);

            // When we import from files, all files are copied and present and not incrementally
            // processed when we received them from the pohne
            if (!mobileSync) {
                FileListProcessed.Add(file);
            }
        }

        CurrentStatus = SyncStatus.DOWNLOAD;

        // get coordinates
        FileInfo fiPoints = new FileInfo(dwe.Points);

        // we request file if we synchronise via phone, we ignore it via file import
        if (mobileSync)
            RequestFile(fiPoints.Name);

        DetailedWayExportFiles dwePoints = new DetailedWayExportFiles();
        dwePoints.File = fiPoints.Name;
        FileListProcessed.Add(dwePoints);

    }

    /// <summary>
    /// Processes the data that was downloaded by creating the destination folders, reassembling file chunks, 
    /// copying files to the appropriate folder and updating the ERW's videos, photos and Pathpoints lists.
    /// As a result, files are organised and stored in the appropriate location, and the internal data model
    /// is updated.
    /// </summary>
    private void ProcessDownloadedData(string sourceFolderPath)
    {
        // If the base folder exists, we overwrite it (as the user consented to it)
        if (Directory.Exists(FileManagement.persistentDataPath + "/" + currentWayFolderName))
            Directory.Delete(FileManagement.persistentDataPath + "/" + currentWayFolderName, true);


        // create base folder
        Directory.CreateDirectory(FileManagement.persistentDataPath + "/" + currentWayFolderName);

        // check for video folder
        if (!Directory.Exists(FileManagement.persistentDataPath + "/" + currentWayFolderName + "/Videos"))
            Directory.CreateDirectory(FileManagement.persistentDataPath + "/" + currentWayFolderName + "/Videos");

        // check for photo folder
        if (!Directory.Exists(FileManagement.persistentDataPath + "/" + currentWayFolderName + "/Fotos"))
            Directory.CreateDirectory(FileManagement.persistentDataPath + "/" + currentWayFolderName + "/Fotos");

        // Reassemble file chunks
        DetailedWayExportFiles.ReassembleFilesInFolder(sourceFolderPath);

        foreach (DetailedWayExportFiles file in FileListProcessed)
        {
            FileInfo fi = new FileInfo(file.File);
            string fileName = fi.Name;

            Log("ProcessDownloadedData - fileName: " + fileName);

            // get the original name, the chunk belongs to
            if (fileName.EndsWith(".chunk"))
            {
                fileName = Path.GetFileNameWithoutExtension(fileName);
                string[] fileNameComponents = fileName.Split('.');
                fileName = String.Join(".", fileNameComponents.Take(fileNameComponents.Length - 1));
            }

            if (fileName.EndsWith(".mp4"))
            {
                // Check whether we already processed the video
                if (erw.Videos.Contains(fileName)) continue;

                erw.Videos.Add(fileName);
                File.Copy(sourceFolderPath + "/" + fileName,
                    FileManagement.persistentDataPath + "/" + currentWayFolderName + "/Videos/" + fileName);

            }
            else if (fileName.EndsWith(".jpg"))
            {
                erw.Photos.Add(fileName);
                File.Copy(sourceFolderPath + "/" + fileName,
                    FileManagement.persistentDataPath + "/" + currentWayFolderName + "/Fotos/" + fileName);

            }
            else if (fileName.EndsWith("-coordinates.xml"))
            {
                ProcessPathpoints(fileName, sourceFolderPath);
            }
        }

        // Update the internal data model. If the erw doesn't exist, we add it to the
        // internal data model. If it does, we just need to update it
        if (InternalDataModelController.GetInternalDataModelController()
            .idm.exploritoryRouteWalks.Find(x => x.RouteId.Equals(erw.RouteId)) == null)
        {
            InternalDataModelController.GetInternalDataModelController().idm.exploritoryRouteWalks.Add(erw);
        }

        InternalDataModelController.GetInternalDataModelController().CheckDirtyFlagsAndSave();

        SaveToLocalStorage(erw);

    }

    /// <summary>
    /// Processes a file containing Pathpoints by deserializing it and storing it in the current ERW
    /// </summary>
    /// <param name="fileName">The name of the file containing the Pathpoints</param>
    private void ProcessPathpoints(string fileName, string sourcePath)
    {
        try
        {
            List<Pathpoint> ppoints = new List<Pathpoint>();

            if (File.Exists(sourcePath+ "/" + fileName))
            {
                using (var xmlReader = new XmlTextReader(sourcePath + "/" + fileName))
                {
                    var xmlSerializer = new XmlSerializer(typeof(List<Pathpoint>));
                    ppoints = (List<Pathpoint>)xmlSerializer.Deserialize(xmlReader);
                }

                erw.Pathpoints = ppoints;

                Log("ProcessPathpoints - deserialize erw.Pathpoints: " + erw.Pathpoints[0].RouteId);

            } else {
                Log("ProcessPathpoints - Coordinates file does not exist!");
            }
        }
        catch (IOException ex)
        {
            // todo: call error handler here
            LogError("ProcessPathpoints - deserialize coordinates:" + fileName + "Error message: " + ex.Message);
        }
    }

    /// <summary>
    /// Save processed files to the local database
    /// </summary>
    /// <param name="erw">Imported route</param>
    private void SaveToLocalStorage(DataOfImportedERW erw)
    {
        Way w = new Way();
        w.Id = erw.Id;
        w.Name = erw.Name;
        //w.UserId = erw.UserId;
        w.Start = erw.Start;
        w.StartType = erw.StartType;
        w.Description = erw.Description;
        w.Destination = erw.Destination;
        w.DestinationType = erw.DestinationType;
        w.FromAPI = erw.FromAPI;
        w.UserId = erw.UserId;
        w.IsDirty = true;
        w.Insert();

        // replace with timestamp
        //int routeId = w.Id * 10000 + System.DateTime.UtcNow.Millisecond;

        Route r = new Route();
        //r.Id = -routeId;
        r.Id = erw.RouteId;
        r.Name = erw.RecordingName;
        r.Date = erw.RecordingDate;
        r.StartTimestamp = erw.StartTimestamp;
        r.EndTimestamp = erw.EndTimestamp;
        r.SocialWorkerId = erw.SocialWorkerId;
        r.LocalVideoResolution = erw.LocalVideoResolution;

        //TODO: Warning if there is no video

        r.Status = (Int32)Route.RouteStatus.New;
        r.WayId = w.Id;
        r.LocalVideoFilename = currentWayFolderName + "/Videos/" + erw.Videos[0];
        r.InsertDirty();

        List<Pathpoint> ppList = CleanRoutePathpoints(erw.Pathpoints);

        var minPP = Pathpoint.GetWithMinId(x => x.Id);
        int ppId = minPP == null? -1 : minPP.Id > 0 ? -1 : minPP.Id - 1;

        foreach (var pp in ppList)
        {
            //pp.Id = -routeId++;
            pp.Id = ppId--;
            pp.RouteId = r.Id;
            pp.InsertDirty();
            

            if (pp.POIType!= Pathpoint.POIsType.Point && pp.PhotoFilenames != null)
            {

                Debug.Log("Pathpoint type:" + pp.POIType + "Photo file: " + pp.PhotoFilename + " Cluster files: " + pp.PhotoFilenames);

                int photoId = -pp.Id * 100 + System.DateTime.UtcNow.Millisecond;
                foreach (var photofile in pp.PhotoFilenames)
                {

                    string filename = FileManagement.persistentDataPath + "/" + currentWayFolderName + "/Fotos/" + photofile.Filename;

                    PathpointPhoto ppf = new PathpointPhoto();
                    ppf.Id = - photoId++;
                    ppf.PathpointId = pp.Id;
                    ppf.PhotoId = ppf.Id;
                    ppf.Timestamp = photofile.Timestamp;
                    ppf.InsertDirty();

                    PhotoData pd = new PhotoData();
                    pd.Id = ppf.Id;
                    pd.Photo = File.ReadAllBytes(filename);
                    pd.InsertDirty();
                }

            }

        }
    }

    public List<Pathpoint> CleanRoutePathpoints(List<Pathpoint> points)
    {
        if (points.Count < 3)
            return points;

        var pipeline = new LocationTools.GPSCleaningPipeline
        {
            ToleranceSimplify = ToleranceSimplify,
            MaxAccuracyRadio = MaxAccuracyRadio,
            DistanceOutlier = DistanceOutlier,
            SegmentSplit = SegmentSplit,
            OutlierFactor = OutlierFactor,
            MinEvenly = MinEvenly,
            MaxEvenly = MaxEvenly,
            POIClusterDistance = POIClusterDistance
        };
        return pipeline.CleanRoute(points);
    }



    /// <summary>
    /// Resets the synchronisation folders, clearing up contents and initialising
    /// the dummy files used as part of the transfer protocol
    /// </summary>
    private void ResetSynchronisationFolders()
    {
        // Check existence of shared folder
        if (!Directory.Exists(FileManagement.persistentDataPath + "/" + FTS._sharedFolder))
        {
            Directory.CreateDirectory(FileManagement.persistentDataPath + "/" + FTS._sharedFolder);
        }

        // Delete all files in sharing folder
        try
        {
            foreach (var file in Directory.GetFiles(FileManagement.persistentDataPath + "/" + FTS._sharedFolder))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            ErrorHandlerSingleton.GetErrorHandler().AddNewError("SynchronizationHandler:Start(): Error in 'Delete all files in sharing folder'", ex.Message, false);
        }

        File.Create(FileManagement.persistentDataPath + "/" + FTS._sharedFolder + "/CONNECTIONALLOWED").Close();
        File.Create(FileManagement.persistentDataPath + "/" + FTS._sharedFolder + "/CONNECTIONDENIED").Close();
        File.Create(FileManagement.persistentDataPath + "/" + FTS._sharedFolder + "/REQUEST-ERW-READY").Close();
        File.Create(FileManagement.persistentDataPath + "/" + FTS._sharedFolder + "/CANCELSYNC").Close();
    }

    /// <summary>
    /// Resets the underlying FTS connections, related variables, and stops
    /// connection polling co-routines.
    /// </summary>
    private void ResetOrDisposeProcessProtocol()
    {
        Log("Performing: ResetOrDisposeProcessProtocol");
        StopAllCoroutines();
        FTS.ResetDeviceList();
        //FTS.Disconnect();
        isCurrentDeviceConnected = false;
        currentDeviceName = null;
        wasSendPollRequestCalled = false;

    }


    /**************************
     *  FTS event listeners   *
     **************************/

    /// <summary>
    /// FTS event listener for file downloading (On File Download ()). This is
    /// the callback that informs us that a requested file has
    /// been completely downloaded, or a protocol message has been served.
    /// </summary>
    /// <param name="file">FileRequest representing the downloaded file</param>
    public void OnFileDownload(FileRequest file)
    {
        Log("CheckForMetadataFile: " + file._sourceName + "with status: " + file.GetStatus().ToString());

        if (file._sourceName.Equals("HANDSHAKE"))
        {
            DisplayScreenPanel(UI_PanelAskForConnection);
        }
        else if (file._sourceName.Equals("waysForExport.xml"))
        {
            fileInfo = file;

            DisplayScreenPanel(UI_SyncOverview);

            FillSyncOverviewList();

        }
        else if (file._sourceName.Equals("ENDOFSYNC"))
        {
            DisplayScreenPanel(UI_PanelProcessingData);
            CurrentStatus = SyncStatus.FINISH;

            try
            {
                ProcessDownloadedData(FileManagement.persistentDataPath + "/" + FTS._downloadFolder);
                DisplayScreenPanel(UI_PanelEnd);
            }
            catch(Exception e)
            {
                DisplayScreenPanel(UI_PanelError);
                PairingViz.RenderError();
                LogError("Error processing downloaded data: " + e.Message);
            }
                       
            ResetOrDisposeProcessProtocol();
        }
        else if (file._sourceName.Equals("CANCELSYNC")) {
            CurrentStatus = SyncStatus.CANCEL;
            // We do not really need to do anything
        }
        else if (file._sourceName.StartsWith("REQUEST-ERW-"))
        {
            //RequestAllFiles();
            // We don't do anything. We wait for the client to
            // prepare the ERW data, and inform us via a
            // a request (REQUEST-ERW-READY)
            CurrentStatus = SyncStatus.WAIT_ERW_READY;
        }
        else if (file._sourceName.EndsWith("-manifest.xml"))
        {
            PrepareFilesToDownload(FileManagement.persistentDataPath + "/" + FTS._downloadFolder);
        }
        else if (file._sourceName.EndsWith(".mp4") ||
            file._sourceName.EndsWith(".chunk") ||
            file._sourceName.EndsWith(".jpg") ||
            file._sourceName.EndsWith("-coordinates.xml"))
        {
            RequestNextFile();
        }


    }

    /// <summary>
    /// FTS event listener for file uploading (On File Upload ()). This is
    /// a callback that informs us of incoming requests from the connected device.
    /// </summary>
    /// <param name="fileUpload">FileUpload representing incoming request. </param>
    public void OnFileUpload(FileUpload fileUpload)
    {
        LogProcess("OnFileUpload:fileUpload: " + fileUpload.GetName());

        // The phone app accepted the connection
        // so we can proceed downloading the list of ways-ERW
        if (fileUpload.GetName().Equals("CONNECTIONALLOWED"))
        {

            // Check that the peer is sending the message according to
            // the state machine of the protocol
            if (CurrentStatus != SyncStatus.WAIT_CONNECT_RESPONSE)
            {
                LogError($"CONNECTIONALLOWED: Process status is {CurrentStatus.ToString()} when WAIT_CONNECT_RESPONSE was expected ");
            }

            RequestFile("waysForExport.xml");

            //TextOverviewPanelAskForConnection.GetComponent<TMP_Text>().text = "Die Geräte sind verbunden.";
            //LoadingIconAskForConnection.SetActive(false);
            DisplayScreenPanel(UI_PanelRouteReading);
            PairingViz.RenderPairingAccepted();

            CurrentStatus = SyncStatus.WAIT_ERW_LIST;
        }
        else if (fileUpload.GetName().Equals("CONNECTIONDENIED"))
        {

            // Check that the peer is sending the message according to
            // the state machine of the protocol
            if (CurrentStatus != SyncStatus.WAIT_CONNECT_RESPONSE)
            {
                LogError($"CONNECTIONALLOWED: Process status is {CurrentStatus.ToString()} when WAIT_CONNECT_RESPONSE was expected ");
            }
            DisplayScreenPanel(UI_PanelDenied);
            PairingViz.RenderError();

            CurrentStatus = SyncStatus.CANCEL; // DENIED
            ResetOrDisposeProcessProtocol();

        }
        else if (fileUpload.GetName().Equals("CANCELSYNC"))
        {
            // Check that the peer is sending the message according to
            // the state machine of the protocol

            if ((int)CurrentStatus < (int)SyncStatus.WAIT_ERW_LIST || (int)CurrentStatus > (int)SyncStatus.DOWNLOAD)
            {
                LogError($"CONNECTIONALLOWED: Process status is {CurrentStatus.ToString()} when status WAIT_ERW_LIST to DOWNLOAD was expected ");
            }
            DisplayScreenPanel(UI_PanelDenied);
            PairingViz.RenderError();

            CurrentStatus = SyncStatus.CANCEL; // DENIED
            ResetOrDisposeProcessProtocol();

        }
        // The ERW manifest we requested is ready to be downloaded
        // This includes the files to be downloaded
        else if (fileUpload.GetName().Equals("REQUEST-ERW-READY"))
        {

            // Check that the peer is sending the message according to
            // the state machine of the protocol
            if (CurrentStatus != SyncStatus.WAIT_ERW_READY)
            {
                LogError($"REQUEST-ERW-READY: Process status is {CurrentStatus.ToString()} when WAIT_ERW_READY was expected ");
            }

            string msg = $"{selectedDwe.Name}-manifest.xml";
            RequestFile(msg);

            CurrentStatus = SyncStatus.WAIT_ERW_MANIFEST;
        }
    }

    /// <summary>
    ///  FTS event listener for updating the device list (On Device List Update ()).
    /// </summary>
    /// <param name="devices">A list of FTSCore.RemoteDevice representing the
    /// currently connected devices. </param>
    public void OnUpdateDevicesList(List<FTSCore.RemoteDevice> devices)
    {
        List<string> list = GetPhoneNamesList();

        isCurrentDeviceConnected = true;
        connectionAttemps = 0;

        // If we are not actively listening
        if (CurrentStatus == SyncStatus.LISTEN ||
            CurrentStatus == SyncStatus.WAIT_DEVICE_SELECT)
        {

            if (list.Count > 0)
            {
                string[] comp = list[0].Split(',');
                //SmartphoneFoundPrefab.GetComponentInChildren<TMP_Text>().text = comp[0];
                //SmartphoneAskForConnectionText.GetComponent<TMP_Text>().text = comp[0];

                currentDeviceName = list[0];
                PairingViz.RenderFoundPhone(comp[0]);
                //SyncState.Instance.PhoneName = comp[0];

                DisplayScreenPanel(UI_PanelFound);
               
                
            }
            else
            {
                wasSendPollRequestCalled = false;
            }

            CurrentStatus = SyncStatus.WAIT_DEVICE_SELECT;
        }
        else
        {
            //Log($"OnDeviceListUpdate: Process status is {CurrentStatus.ToString()} when LISTEN or WAIT_DEVICE_SELECT was expected ");            
        }

        TimeSpan diff = (System.DateTime.Now - lastHeartbeat);

        LogProcess($"LastHeartbeat: {diff.TotalSeconds} Device list: " + string.Join("--", list));

        TextLog.text += $"\n[{System.DateTime.Now.ToString("HH:mm:ss")}] Status: {CurrentStatus.ToString()} LastHeartbeat: {diff.TotalSeconds} Device list: " + string.Join("--", list);

    }

    /// <summary>
    /// FTS event listener for file timeout (On File Timeout ())
    /// </summary>
    /// <param name="file">FileRequest representing the timed out request</param>
    public void OnFileTimeout(FileRequest file)
    {
        TextLog.text = $"[{System.DateTime.Now.ToString("HH:mm:ss")}] OnFileTimeout: Timeout for " + file._sourceName + " with status: " + file.GetStatus().ToString();
        Log(TextLog.text);

        List<string> list = FTS.GetDeviceNamesList();
        Log($"[{CurrentStatus.ToString()}] Device list: " + string.Join("--", list));

        //UI_PanelError.SetActive(true);
        //ResetOrDisposeProcessProtocol();

    }

    /// <summary>
    /// FTS event listener for errors (On Error ())
    /// </summary>
    /// <param name="code">Error number</param>
    /// <param name="msg">The error message</param>
    public void OnError(int code, string msg)
    {
        Log("OnError: with code " + code + " and message: " + msg);
    }


    /**********************************
     *  Protocol (outgoing) Requests  *
     **********************************/

    /// <summary>
    /// Sends a target device a request for connection (a handshake)
    /// </summary>
    private void RequestHandshake()
    {
        LogProcess("AskForConnection");
        RequestFile("HANDSHAKE");

        CurrentStatus = SyncStatus.WAIT_CONNECT_RESPONSE;
    }

    /// <summary>
    /// Request the connected device the ERW selected by the user
    /// from the list of available routes.
    /// </summary>
    private void RequestSelectedERW()
    {
        string msg = $"REQUEST-ERW-{selectedDwe.RouteId}";
        LogProcess($"RequestSelectedERW: {msg}");

        RequestFile(msg);

        CurrentStatus = SyncStatus.WAIT_ERW_PREPARE;
    }

    /// <summary>
    /// Informs the connected device that the process has been cancelled
    /// so that it can terminate it with the proper status.
    /// </summary>
    private void RequestCancelProcess()
    {
        LogProcess("CancelSynchronisation: Requesting ENDOFSYNC");
        RequestFile("CANCELSYNC");

        CurrentStatus = SyncStatus.CANCEL;
    }

    /// <summary>
    /// Informs the connected device that all files have been downloaded
    /// successfully, and the transfer process is completed.
    /// </summary>
    private void RequestEndOfSync()
    {
        LogProcess("RequestNextFile: Nothing left; Requesting ENDOFSYNC");
        RequestFile("ENDOFSYNC");

        CurrentStatus = SyncStatus.DOWNLOAD_READY;
    }

    /// <summary>
    /// Request the connected device a specific file to download
    /// </summary>
    private void RequestNextFile()
    {
        if (FileListToSyncronize.Count > 0)
        {
            DetailedWayExportFiles firstItem = FileListToSyncronize[0];
            FileInfo fi = new FileInfo(firstItem.File);

            LogProcess($"RequestNextFile: {fi.Name}");
            RequestFile(fi.Name);
            FileListProcessed.Add(firstItem);
            FileListToSyncronize.RemoveAt(0);

        }
        else
        {
            RequestEndOfSync();
        }

    }

    /// <summary>
    /// Requests the specified file from the currently connected device.
    /// </summary>
    /// <param name="fileName">Name of the file to request.</param>
    private void RequestFile(string fileName)
    {
        int device = GetCurrentDeviceIndex();
        FTS.RequestFile(device, fileName);
    }

    /// <summary>
    ///  Obtains the current connected device index, from the dynamic list of
    ///  FTS connected devices.
    /// </summary>
    /// <returns>The index of the current device in the list of FTS connected devices</returns>
    private int GetCurrentDeviceIndex()
    {
        var list = FTS.GetDeviceNamesList();
        return list.IndexOf(currentDeviceName);
    }

    /// <summary>
    ///  Obtains the list of connected phones, from the dynamic list of
    ///  FTS connected devices.
    /// </summary>
    /// <returns>List of connected phone apps</returns>
    private List<string> GetPhoneNamesList()
    {
        var list = FTS.GetDeviceNamesList();
        var phones = new List<string>();

        foreach (var device in list)
        {
            // Log("Device: "+ device);
            string[] comp = device.Split(','); // name,type
            if (comp.Length == 2 && comp[1] == "USR")
            {
                phones.Add(device);
            }

        }
        return phones;
    }


    /// <summary>
    ///  Checks whether the remote device is reponsive (connected). This is based
    ///  on delayed polling requests, as FTS does not have a primitive for
    ///  checking the status of a remote device.
    /// </summary>
    private IEnumerator CheckDeviceConnection()
    {
        while (isCurrentDeviceConnected || connectionAttemps < 3)
        {
            int device = GetCurrentDeviceIndex();
            string deviceIp = FTS.GetDevice(device).ip;


            // We switch off the connected flag
            isCurrentDeviceConnected = false;
            connectionAttemps++;

            // We poll the connected device
            // If alive, it should set the deviceConnected to = true

            FTS.SendPollRequest(deviceIp);
            lastHeartbeat = System.DateTime.Now;


            TextLog.text = $"Pooling request sent to device {currentDeviceName} (attempt {connectionAttemps}). ";
            LogProcess(TextLog.text);

            yield return new WaitForSeconds(10f);
        }

        if (CurrentStatus != SyncStatus.FINISH &&
            CurrentStatus != SyncStatus.CANCEL)
        {
            LogProcess($"Device {currentDeviceName} is disconnected.");

            DisplayScreenPanel(UI_PanelError);
            PairingViz.RenderError();

            ResetOrDisposeProcessProtocol();
        }

    }



    /* Log functions */

    private void Log(string text)
    {
        Debug.Log(text);
        logs.Add(text);
    }

    private void LogError(string text)
    {
        Debug.Log(text);
        logs.Add(text);
    }

    private void LogProcess(string text)
    {
        string entry = $"[{System.DateTime.Now.ToString("HH:mm:ss")}] Status: {CurrentStatus.ToString()} Remote Device: {currentDeviceName} == {text}";
        Debug.Log(entry);
    }

    public class DetailedWayExport
    {
        public string Folder { get; set; }
        public List<DetailedWayExportFiles> Files { get; set; }
        public string Points { get; set; }
        public int Id { set; get; }
        public string Start { set; get; }
        public string Destination { set; get; }
        public string StartType { set; get; }
        public string DestinationType { set; get; }
        public string Name { set; get; }
        public string Description { set; get; }
        public int UserId { set; get; }

        public int RouteId { set; get; }
        public System.DateTime RecordingDate { set; get; }
        public string RecordingName { set; get; }
        public string LocalVideoResolution { set; get; }
        public long StartTimestamp { set; get; }
        public long EndTimestamp { set; get; }
        public int SocialWorkerId { set; get; }

        public bool FromAPI { set; get; }
    }

    public class DetailedWayExportFiles
    {
        public string File { get; set; }
        public byte[] Checksum { get; set; }


        /// <summary>
        /// Reassembles the chunks of the files in a specific folder into a single file
        /// </summary>
        /// <param name="folder">The folder where the chunks are stored</param>
        public static void ReassembleFilesInFolder(string folder)
        {

            HashSet<string> processedFiles = new HashSet<string>();

            int ChunkSize = 50 * 1024 * 1024;
            // Get a list of all chunk files in the folder
            string[] chunkFiles = Directory.GetFiles(folder, "*.chunk");

            foreach (string chunkFile in chunkFiles)
            {
                // Get the original file name and chunk count from the chunk file name
                string fileName = Path.GetFileNameWithoutExtension(chunkFile);
                string[] fileNameComponents = fileName.Split('.');
                string originalFileName = String.Join(".", fileNameComponents.Take(fileNameComponents.Length - 1));
                Debug.Log("filenameComponents:" + fileNameComponents.Last());
                int chunkCount = int.Parse(fileNameComponents.Last().Split('-').Last());

                if (!processedFiles.Contains(originalFileName))
                {
                    // Reassemble the file
                    ReassembleFile(folder, originalFileName, chunkCount, ChunkSize);

                    // Add the file name to the processed files set
                    processedFiles.Add(originalFileName);
                }

            }
        }

        /// <summary>
        /// Reassembles the chunks of a file into a single file
        /// </summary>
        /// <param name="folder">The folder where the chunks are stored</param>
        /// <param name="file">The name of the file to reassemble</param>
        /// <param name="chunkCount">The number of chunks the file is divided into</param>
        /// <param name="chunkSize">The size of each chunk</param>
        public static void ReassembleFile(string folder, string file, int chunkCount, double ChunkSize)
        {
            Debug.Log("filename to ressemble: " + file);

            // Create a buffer for reading chunks
            byte[] buffer = new byte[(int)ChunkSize];

            // Construct the full path of the output file
            string outputFile = Path.Combine(folder, file);

            // Create a new FileStream for the output file
            using (FileStream outputStream = new FileStream(outputFile, FileMode.Create))
            {
                for (int i = 0; i < chunkCount; i++)
                {
                    // Construct the chunk file name
                    string chunkFileName = Path.Combine(folder, $"{new FileInfo(file).Name}.part{i + 1}-{chunkCount}.chunk");

                    // Open the chunk file for reading
                    using (FileStream inputStream = new FileStream(chunkFileName, FileMode.Open))
                    {
                        // Read the chunk into the buffer
                        int chunkSize = inputStream.Read(buffer, 0, buffer.Length);

                        // Write the chunk to the output file
                        outputStream.Write(buffer, 0, chunkSize);
                    }
                }
            }

            // Delete the chunk files
            for (int i = 0; i < chunkCount; i++)
            {
                string chunkFileName = Path.Combine(folder, $"{new FileInfo(file).Name}.part{i + 1}-{chunkCount}.chunk");
                System.IO.File.Delete(chunkFileName);
            }
        }



        public static List<DetailedWayExport> ParseDWEFile(string fileName, string path)
        {
            List<DetailedWayExport> listOfWays = new List<DetailedWayExport>();

            if (System.IO.File.Exists((path+ "/" + fileName)))
            {
                Debug.Log(path + "/" + fileName + " exist!");

                using (var xmlReader = new XmlTextReader(path + "/" + fileName))
                {
                    var xmlSerializer = new XmlSerializer(typeof(List<DetailedWayExport>));
                    listOfWays = (List<DetailedWayExport>)xmlSerializer.Deserialize(xmlReader);
                }
            }
            else
            {
                Debug.LogWarning(path + "/" + fileName + " does not exist!");
                ErrorHandlerSingleton.GetErrorHandler().AddNewError("ParseDWEFile(): ", path + "/" + fileName + " does not exist!");
            }

            return listOfWays;
        }



    }
}
