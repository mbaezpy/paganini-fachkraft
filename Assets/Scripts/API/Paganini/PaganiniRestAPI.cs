using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;


namespace PaganiniRestAPI
{
    public class Path
    {
        //Base URL for the Rest APi
        public const string BaseUrl = "https://infinteg-main.fh-bielefeld.de/paganini/api/";
        //public const string BaseUrl = "http://192.168.178.22:3000/";
       
       
        public const string Authenticate = BaseUrl + "sw/me/authentification";
        
        public const string SwProfile = BaseUrl + "sw/me/profile";

        public const string SwUsersList = BaseUrl + "sw/users";
        public const string SwUsers = SwUsersList + "/{0}";

        public const string SwUserPIN = SwUsers + "/pin";

        public const string SwWaysList =  SwUsers + "/ways";
        public const string SwWays = BaseUrl + "sw/ways/{0}";

        public const string SwRoutesList = SwWays + "/routes";
        public const string SwRoutes = BaseUrl + "sw/routes/{0}";

        public const string SwRoutePhotoList = SwRoutes + "/photos";
        public const string SwPOIList = SwRoutes + "/pois";

        public const string SwPhotoDataList = SwRoutePhotoList + "/data";

        public const string SwPathpointList = SwRoutes + "/pathpoints";
        public const string SwPathpoints = BaseUrl + "sw/pathpoints/{0}";

        public const string SwRouteWalk = BaseUrl + "sw/routewalks/{0}";
        public const string SwRouteWalkList =  SwRoutes + "/routewalks";

        public const string SwRouteWalkPIMList = SwRoutes + "/pims";

        public const string SwRouteWalkEventList = SwRoutes + "/routewalks/events";

        


        // Function to build a query string from a dictionary
        public static string BuildQueryString(Dictionary<string, string> query)
        {
            if (query == null || query.Count == 0)
            {
                return string.Empty; // No query parameters, return an empty string
            }

            var queryString = new StringBuilder("?");
            foreach (var kvp in query)
            {
                // Encode and append each key-value pair to the query string
                queryString.Append(Uri.EscapeDataString(kvp.Key));
                queryString.Append("=");
                queryString.Append(Uri.EscapeDataString(kvp.Value));
                queryString.Append("&");
            }

            return queryString.ToString().TrimEnd('&');
        }

    }


    public class SocialWorker
    {

        public static void Authenticate(string username, string password, UnityAction<AuthTokenAPI> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password }
            };

            RESTAPI.Instance.Get<AuthTokenAPI>(Path.Authenticate, successCallback, errorCallback, headers);
        }

        public static void GetProfile(UnityAction<SocialWorkerAPIResult> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            string url = Path.SwProfile;

            RESTAPI.Instance.Get<SocialWorkerAPIResult>(url, successCallback, errorCallback, headers);
        }

        public static void Update(SocialWorkerAPIUpdate user, UnityAction<SocialWorkerAPIResult> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            string url = Path.SwProfile;  
            RESTAPI.Instance.PutMultipart<SocialWorkerAPIResult>(url, user, user.files, successCallback, errorCallback, headers);

        }
        
    }

    public class User
    {

        public static void GetByID(Int32 id, UnityAction<UserAPIResult> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }                
            };

            string url = string.Format(Path.SwUsers, id);

            RESTAPI.Instance.Get<UserAPIResult>(url, successCallback, errorCallback, headers);
        }

        public static void GetAll(UnityAction<UserAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            RESTAPI.Instance.Get<UserAPIList>(Path.SwUsersList, successCallback, errorCallback, headers);
        }

        public static void CreateOrUpdate(UserAPI user, UnityAction<UserAPIResult> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            if (user.IsNew)
            {
                RESTAPI.Instance.Post<UserAPIResult>(Path.SwUsersList, user, successCallback, errorCallback, headers);
            }
            else
            {
                string url = string.Format(Path.SwUsers, user.user_id);     
                RESTAPI.Instance.Put<UserAPIResult>(url, user, successCallback, errorCallback, headers);
            }
        }

        public static void GeneratePIN(Int32 id, UnityAction<UserPinAPI> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }                
            };

            string url = string.Format(Path.SwUserPIN, id);
            
            // empty body
            var body = new BaseAPI();
            RESTAPI.Instance.Post<UserPinAPI>(url, body, successCallback, errorCallback, headers);
        }        

    }

    public class Way
    {

        public static void GetAll(Int32 userId, UnityAction<WayAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            string url = string.Format(Path.SwWaysList, userId);

            RESTAPI.Instance.Get<WayAPIList>(url, successCallback, errorCallback, headers);
        }


        public static void CreateOrUpdate(Int32 userId, WayAPI way, UnityAction<WayAPIResult> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };            

            if (way.IsNew)
            {
                string url = string.Format(Path.SwWaysList, userId);
                RESTAPI.Instance.Post<WayAPIResult>(url, way, successCallback, errorCallback, headers);
            }
            else
            {
                string url = string.Format(Path.SwWays, way.way_id);                
                RESTAPI.Instance.Put<WayAPIResult>(url, (WayAPIUpdate)way, successCallback, errorCallback, headers);
            }
        }

    }

    public class Route
    {


        public static void CreateOrUpdate(Int32 wayId, RouteAPI route, UnityAction<RouteAPIResult> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            if (route.IsNew)
            {
                string url = string.Format(Path.SwRoutesList, wayId);
                RESTAPI.Instance.Post<RouteAPIResult>(url, route, successCallback, errorCallback, headers);
            }
            else
            {
                string url = string.Format(Path.SwRoutes, route.erw_id);
                RESTAPI.Instance.Put<RouteAPIResult>(url, route, successCallback, errorCallback, headers);
            }
        }

    }

    public class RouteWalk
    {

        public static void Get(Int32 routeWalkId, UnityAction<RouteWalkAPIResult> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };


            string url = string.Format(Path.SwRouteWalk, routeWalkId);
            RESTAPI.Instance.Get<RouteWalkAPIResult>(url, successCallback, errorCallback, headers);
        }

        public static void GetAll(Int32 routeId, UnityAction<RouteWalkAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };


            string url = string.Format(Path.SwRouteWalkList, routeId);
            RESTAPI.Instance.Get<RouteWalkAPIList>(url, successCallback, errorCallback, headers);
        }

    }

    public class RouteWalkEvent
    {

        public static void GetAll(Int32 routeId, Dictionary<string, string> query, UnityAction<RouteWalkEventAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            var queryString = Path.BuildQueryString(query);

            string url = string.Format(Path.SwRouteWalkEventList, routeId) + queryString;


            RESTAPI.Instance.Get<RouteWalkEventAPIList>(url, successCallback, errorCallback, headers);
        }

    }

    public class PathpointPIM
    {

        public static void BatchCreate(Int32 routeWalkId, PathpointPIMAPIBatch batch, UnityAction<PathpointPIMAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };


            string url = string.Format(Path.SwRouteWalkPIMList, routeWalkId);
            RESTAPI.Instance.Post<PathpointPIMAPIList>(url, batch, successCallback, errorCallback, headers);
        }
    }


    public class Pathpoint
    {
        public static void GetAll(Int32 routeId, UnityAction<PathpointAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            string url = string.Format(Path.SwPathpointList, routeId);

            RESTAPI.Instance.Get<PathpointAPIList>(url, successCallback, errorCallback, headers);
        }

        public static void BatchCreate(Int32 routeId, PathpointAPIBatch batch, UnityAction<PathpointAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };


            string url = string.Format(Path.SwPathpointList, routeId);
            RESTAPI.Instance.Post<PathpointAPIList>(url, batch, successCallback, errorCallback, headers);
        }

        public static void BatchUpdate(Int32 routeId, PathpointAPIBatch batch, UnityAction<PathpointAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };


            string url = string.Format(Path.SwPathpointList, routeId);
            RESTAPI.Instance.Put<PathpointAPIList>(url, batch, successCallback, errorCallback, headers);
        }

    }

    public class PathpointPhoto
    {
        public static void BatchCreate(Int32 routeId, PathpointPhotoAPIBatch batch, UnityAction<PathpointPhotoAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            // batch will need to have the list of byte[] files, with reference to the pathpoints (photo with the filename?)

            string url = string.Format(Path.SwRoutePhotoList, routeId);
            RESTAPI.Instance.PostMultipart<PathpointPhotoAPIList>(url, batch, batch.files, successCallback, errorCallback, headers);

        }

        public static void BatchUpdate(Int32 routeId, PathpointPhotoAPIBatch batch, UnityAction<PathpointPhotoAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            // batch will need to have the list of byte[] files, with reference to the pathpoints (photo with the filename?)

            string url = string.Format(Path.SwRoutePhotoList, routeId);
            RESTAPI.Instance.Put<PathpointPhotoAPIList>(url, batch, successCallback, errorCallback, headers);

        }

    }


    public class PathpointPOI
    {
        public static void GetAll(Int32 routeId, UnityAction<PathpointPOIAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            string url = string.Format(Path.SwPOIList, routeId);
            RESTAPI.Instance.Get<PathpointPOIAPIList>(url, successCallback, errorCallback, headers);
        }

        public static void BatchCreate(Int32 routeId, PathpointPOIAPIBatch batch, UnityAction<PathpointPOIAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            // batch will need to have the list of byte[] files, with reference to the pathpoints (photo with the filename?)

            string url = string.Format(Path.SwPOIList, routeId);
            RESTAPI.Instance.PostMultipart<PathpointPOIAPIList>(url, batch, batch.files, successCallback, errorCallback, headers);

        }
    }


    public class PhotoData
    {
        public static void GetAll(Int32 routeId, Dictionary<string, string> query, UnityAction<PhotoDataAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            var queryString = Path.BuildQueryString(query);

            string url = string.Format(Path.SwPhotoDataList, routeId) + queryString;

            RESTAPI.Instance.Get<PhotoDataAPIList>(url, successCallback, errorCallback, headers);
        }


        public static void BatchUpdate(Int32 routeId, PhotoDataAPIBatch batch, UnityAction<PhotoDataAPIList> successCallback, UnityAction<string> errorCallback)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "apitoken", AppState.APIToken }
            };

            // batch will need to have the list of byte[] files, with reference to the pathpoints (photo with the filename?)

            string url = string.Format(Path.SwPhotoDataList, routeId);
            RESTAPI.Instance.PutMultipart<PhotoDataAPIList>(url, batch, batch.files, successCallback, errorCallback, headers);

        }

    }


}