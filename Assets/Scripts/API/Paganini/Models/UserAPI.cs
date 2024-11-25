using Newtonsoft.Json;

public interface IUserAPI 
{
    public int user_id { get; set; }
    public string user_mnemonic_token { get; set; }
    public string user_username { get; set; }
    public string user_apitoken { get; set; }
    public bool user_righthanded { get; set; }
    public bool user_canread { get; set; }
    public bool user_activatetts { get; set; }
    public bool user_vibration { get; set; }
    public string user_contact { get; set; }
    public WorkshopAPI user_workshop { get; set; }
}


public class UserAPIBase : BaseAPI
{
    public string user_mnemonic_token { get; set; }
    public string user_username { get; set; }
    public string user_apitoken { get; set; }
    public bool user_righthanded { get; set; }
    public bool user_canread { get; set; }
    public bool user_activatetts { get; set; }
    public bool user_vibration { get; set; }
    public string user_contact { get; set; }
}

[System.Serializable]
public class UserAPI : UserAPIBase, IUserAPI
{
    [JsonIgnore]
    public int user_id { get; set; }

    [JsonIgnore]
    public WorkshopAPI user_workshop { get; set; }
}


[System.Serializable]
public class UserAPIUpdate : UserAPIBase, IUserAPI
{
    [JsonIgnore]
    public int user_id { get; set; }

    [JsonIgnore]
    public WorkshopAPI user_workshop { get; set; }
}


[System.Serializable]
public class UserAPIResult : UserAPIBase, IUserAPI
{
    [JsonProperty]
    public int user_id { get; set; }

    [JsonProperty]
    public WorkshopAPI user_workshop { get; set; }
}

public class UserAPIList
{
    public UserAPIResult[] users;
}
