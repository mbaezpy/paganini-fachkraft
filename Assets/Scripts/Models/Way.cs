using System.Collections.Generic;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;

public class Way : BaseModel
{

    [PrimaryKey]
    public int Id { set; get; }
    public string Start { set; get; }
    public string Destination { set; get; }
    public string StartType { set; get; }
    public string DestinationType { set; get; }
    public string Name { set; get; }
    public string Description { set; get; }
    public int Status { set; get; }

    [OneToMany(CascadeOperations = CascadeOperation.All)]
    public List<Route> Routes { get; set; }

    public enum WayStatus
    {
        Local = 0,
        FromAPI = 1
    }


    public override string ToString()
    {
        return string.Format("[Weg: way_id={0}, way_start={1}, way_destination={2}, "
            + "way_start_type {3}, way_destination_type={4}, way_name={5},  way_description={6}, status={7}]",
            Id, Start, Destination, StartType, DestinationType, Name, Description, Status);
    }

    public Way() { }
    public Way(WayAPI way)
    {
        this.Id = way.way_id;
        this.Start = way.way_start.adr_name;
        this.Destination = way.way_destination.adr_name;
        this.StartType = way.way_start.adr_icon;
        this.DestinationType = way.way_destination.adr_icon;
        this.Name = way.way_name;
        this.Description = way.way_description;
        this.Status = (int)WayStatus.FromAPI;
        this.FromAPI = true;
    }

    public WayAPI ToAPI()
    {
        AddressAPI addrStart = new AddressAPI
        {
            adr_name = this.Start,
            adr_icon = this.StartType
        };

        AddressAPI addrDestination = new AddressAPI
        {
            adr_name = this.Destination,
            adr_icon = this.DestinationType
        };

        WayAPI way = new WayAPI
        {
            way_id = this.Id,
            way_start = addrStart,
            way_destination = addrDestination,
            way_name = this.Name,
            way_description = this.Description
        };
        return way;
    }

    public static List<Way> GetAllWaysAndRoutes()
    {
        List<Way> ways;

        var conn = DBConnector.Instance.GetConnection();

        // Query all Ways and their related Routes using sqlite-net's built-in mapping functionality
         ways = conn.GetAllWithChildren<Way>(recursive: true);


        return ways;
    }
}