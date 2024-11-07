using System;
using System.Collections.Generic;
using System.Linq;
using PaganiniRestAPI;
using SQLite4Unity3d;
//using SQLiteNetExtensions.Attributes;


public class Route : BaseModel<Route>
{

    [PrimaryKey]
    public int Id { set; get; }
    public string Name { set; get; }
    public System.DateTime Date { set; get; }
    public int Pin { set; get; }
    public RouteStatus Status { set; get; }
    public string LocalVideoFilename { set; get; }
    public string LocalVideoResolution { set; get; }
    public long? StartTimestamp { set; get; }
    public long? EndTimestamp { set; get; }
    public int SocialWorkerId { set; get; }
    public bool? IsDraftUpdated { set; get; }
    public bool? IsPIMUpdated { set; get; }

    public int RouteWalkCount { set; get; }
    public System.DateTime RouteWalkStart { set; get; }
    public System.DateTime RouteWalkEnd { set; get; }
    public System.DateTime FirstRouteWalkDate { set; get; }
    public int? LastRouteWalkId { set; get; }
    public long? LastUpdate { set; get; }

    //[Indexed]
    public int WayId { get; set; }

    [Ignore]
    public List<Pathpoint> Pathpoints { get; set; }

    [Ignore]
    public RouteWalk LastRouteWalk { get; set; }

    public override string ToString()
    {
        return string.Format("[exploratory_route_walk: erw_id={0}, way_id={1}, erw_name={2},  erw_datum={3}, erw_pin={4}, erw_status=(5)]", Id, WayId, Name, Date, Pin, Status);
    }

    public enum RouteStatus
    {
        New,
        DraftPrepared,
        DraftNegotiated,
        Training,
        Completed,
        Discarded
    }

    public static Dictionary<RouteStatus, string> RouteStatusDescriptions = new Dictionary<RouteStatus, string>()
    {
        { RouteStatus.New, "In Bearbeitun" },
        { RouteStatus.DraftPrepared, "In Diskussion" },
        { RouteStatus.DraftNegotiated, "Bei Trainingsanpassung" },
        { RouteStatus.Training, "In Training" },
        { RouteStatus.Completed, "Abgeschlossen" },
        { RouteStatus.Discarded, "Verworfen" },
    };

    public Route() { }
    public Route(RouteAPIResult erw)
    {
        this.Id = erw.erw_id;
        this.WayId = erw.way_id;
        this.Name = erw.erw_name;
        this.Date = (DateTime)DateUtils.ConvertUTCStringToUTCDate(erw.erw_date, "yyyy-MM-dd'T'HH:mm:ss");
        this.Pin = erw.erw_pin;
        this.LocalVideoFilename = erw.erw_video_url;
        if (erw.status != null)
        {
            this.Status = (RouteStatus)erw.status.erw_status_id;
        }
        this.FromAPI = true;

        LocalVideoResolution = erw.erw_video_resolution;
        StartTimestamp = DateUtils.ConvertUTCStringToTsMilliseconds(erw.erw_start_time, "yyyy-MM-dd'T'HH:mm:ss");
        EndTimestamp = DateUtils.ConvertUTCStringToTsMilliseconds(erw.erw_end_time, "yyyy-MM-dd'T'HH:mm:ss");
        SocialWorkerId = erw.erw_socialworker_id??-1;

        RouteWalkCount = erw.routewalk_count;
        if (erw.last_routewalk != null)
        {
            LastRouteWalkId = erw.last_routewalk.rw_id;
            LastRouteWalk = new RouteWalk(erw.last_routewalk);

            RouteWalkStart = LastRouteWalk.StartDateTime;
            RouteWalkEnd = LastRouteWalk.EndDateTime;
        }

        if (erw.first_training_date!=null)
        {
            FirstRouteWalkDate = (DateTime)DateUtils.ConvertUTCStringToUTCDate(erw.first_training_date, "yyyy-MM-dd'T'HH:mm:ss");
        }

    }

    public RouteAPI ToAPI()
    {
        RouteAPI erw = new RouteAPI
        {
            way_id = this.WayId,
            erw_name = this.Name,
            erw_date = DateUtils.ConvertUTCDateToUTCString(Date, "yyyy-MM-dd'T'HH:mm:ss"),
            erw_pin = this.Pin,
            erw_video_url = this.LocalVideoFilename,
            status = new RouteStatusAPI { erw_status_id = (int)this.Status }
        };

        erw.erw_video_resolution = LocalVideoResolution;
        erw.erw_start_time = DateUtils.ConvertMillisecondsToUTCString(StartTimestamp, "yyyy-MM-dd'T'HH:mm:ss");
        erw.erw_end_time = DateUtils.ConvertMillisecondsToUTCString(EndTimestamp, "yyyy-MM-dd'T'HH:mm:ss");

        erw.erw_socialworker_id = SocialWorkerId != -1 ? SocialWorkerId : null;

        // Tag whether needs to be updated
        if (!FromAPI)
        {
            erw.IsNew = true;
        }
        else
        {
            erw.erw_id = this.Id;
        }

        return erw;
    }

    public static List<Route> GetRouteListByWay(int wayId)
    {
        List<Route> routes;

        var conn = DBConnector.Instance.GetConnection();

        // Query all Ways and their related Routes using sqlite-net's built-in mapping functionality

        routes = conn.Table<Route>().Where(r => r.WayId == wayId && r.Status != RouteStatus.Discarded).ToList();

        return routes;
    }

    public static void ChangeParent(int oldWayId, int newWayId)
    {
        // Open the SQLliteConnection
        var conn = DBConnector.Instance.GetConnection();

        // Create the SQL command with the update query and parameters
        string cmdText = "UPDATE Route SET WayId = ? WHERE WayId = ?";
        SQLiteCommand cmd = conn.CreateCommand(cmdText, newWayId, oldWayId);

        // Execute the command
        cmd.ExecuteNonQuery();

    }

    ///// <summary>
    ///// Deletes all non-dirty (unmodified) model instances from the database, except those under training.
    ///// </summary>
    //public static void DeleteIfUpdatedDrafts(bool? IsDraftUpdated = null, bool? KeepTraining = null)
    //{
    //    var conn = DBConnector.Instance.GetConnection();


    //    // Create the SQL command with the update query and parameters
    //    string cmdText = "DELETE from Route where IsDirty = 0 "; // we exclude training


    //    if (IsDraftUpdated != null)
    //    {
    //        cmdText = cmdText + " AND IsDraftUpdated = ?";
    //        SQLiteCommand cmd = conn.CreateCommand(cmdText, IsDraftUpdated);
    //        cmd.ExecuteNonQuery();
    //        return;
    //    }
    //    if (KeepTraining != null)
    //    {
    //        cmdText = cmdText + " AND Status <> ?";
    //        SQLiteCommand cmd = conn.CreateCommand(cmdText, KeepTraining);
    //        cmd.ExecuteNonQuery();
    //        return;
    //    }


    //    conn.Execute(cmdText);
    //}

    /// <summary>
    /// Deletes all non-dirty (unmodified) model instances from the database, except those under training.
    /// </summary>
    public static void DeleteIfUpdatedDrafts(bool? IsDraftUpdated = null, bool? KeepTraining = null)
    {
        var conn = DBConnector.Instance.GetConnection();

        // Create the base SQL command
        string cmdText = "DELETE FROM Route WHERE IsDirty = 0";

        // Add conditions based on the provided parameters
        if (IsDraftUpdated != null)
        {
            cmdText += " AND IsDraftUpdated = ?";
        }
        if (KeepTraining != null && (bool)KeepTraining)
        {
            cmdText += " AND Status <> ?";
        }

        // Create the command
        var cmd = conn.CreateCommand(cmdText);

        // Add parameters to the command if they are provided
        if (IsDraftUpdated != null)
        {
            cmd.Bind(IsDraftUpdated);
        }
        if (KeepTraining != null && (bool)KeepTraining)
        {
            cmd.Bind((int)RouteStatus.Training);
        }

        // Execute the command
        cmd.ExecuteNonQuery();
    }


    public static Route GetWithLastRouteWalk(int id)
    {
        Route route = Route.Get(id);
        if (route.LastRouteWalkId != null)
        {
            //route.LastRouteWalk = RouteWalk.Get((int)route.LastRouteWalkId);
        }

        return route;
    }

}