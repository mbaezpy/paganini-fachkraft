using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class InternalDataModel
{
    /// <summary>
    /// Local data model to handle user settings 
    /// and data of the exploritory route walks
    /// </summary>


    /// USER AND APPLICATION SETTINGS
    public bool isLoginRemembered { get; set; }
    public string lastSavedDate { get; set; }

    public int currentIdOfWay { get; set; }

    public List<DataOfImportedERW> exploritoryRouteWalks { get; set; }

    /// CLASSES FOR DATA OF THE EXPLORITORY ROUTE WALKS

    public class DataOfImportedERW
    {
        private string _Folder;
        public string Folder
        {
            get { return _Folder; }
            set
            {
                IsDirty = true;
                _Folder = value;
            }
        }
        public List<string> Videos { get; set; }
        public List<string> Photos { get; set; }
        public List<Pathpoint> Pathpoints { get; set; }
        public int Id { set; get; }
        public string Start { set; get; }
        public string Destination { set; get; }
        public string StartType { set; get; }
        public string DestinationType { set; get; }
        public string Name { set; get; }
        public string Description { set; get; }
        public int UserId { set; get; }
        public bool IsDirty { set; get; }

        public int RouteId { set; get; }
        public System.DateTime RecordingDate { set; get; }
        public string RecordingName { set; get; }
        public string LocalVideoResolution { set; get; }
        public long StartTimestamp { set; get; }
        public long EndTimestamp { set; get; }
        public int SocialWorkerId { set; get; }

        public bool FromAPI { set; get; }
    }
}

