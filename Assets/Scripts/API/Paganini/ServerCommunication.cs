﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// This class is responsible for handling REST API requests to remote server.
/// To extend this class you just need to add new API methods.
/// </summary>
public class ServerCommunication : PersistentLazySingleton<ServerCommunication>
{
    #region [Request Type]
    public enum Requesttype
    {
        GET, POST, DELETE
    }
    public struct Header
    {
        public string name;
        public string value;
    }
    #endregion

    #region [Server Communication]

    /// <summary>
    /// This method is used to begin sending request process.
    /// </summary>
    /// <param name="url">API url.</param>
    /// <param name="callbackOnSuccess">Callback on success.</param>
    /// <param name="callbackOnFail">Callback on fail.</param>
    /// <param name="type">Type of Request to Send</param>
    /// <param name="payload">Payload to send with Post Request</param>
    /// <typeparam name="T">Data Model Type.</typeparam>
    private void SendRequest<T>(string url, UnityAction<T> callbackOnSuccess, UnityAction<string> callbackOnFail, Header[] header, Requesttype type = Requesttype.GET, string payload = "")
    {
        StartCoroutine(RequestCoroutine(url, callbackOnSuccess, callbackOnFail, header, type, payload));
    }

    /// <summary>
    /// Coroutine that handles communication with REST server.
    /// </summary>
    /// <returns>The coroutine.</returns>
    /// <param name="url">API url.</param>
    /// <param name="callbackOnSuccess">Callback on success.</param>
    /// <param name="callbackOnFail">Callback on fail.</param>
    /// <param name="type">Type of Request to Send</param>
    /// <param name="payload">Payload to send with Post Request</param>
    /// <typeparam name="T">Data Model Type.</typeparam>
    private IEnumerator RequestCoroutine<T>(string url, UnityAction<T> callbackOnSuccess, UnityAction<string> callbackOnFail, Header[] header, Requesttype type, string payload)
    {
        UnityWebRequest www;
        switch (type)
        {
            case Requesttype.GET:
                www = UnityWebRequest.Get(url);
                break;
            case Requesttype.POST:
                www = UnityWebRequest.Post(url, payload);
                break;
            case Requesttype.DELETE:
                www = UnityWebRequest.Delete(url);
                break;
            default:
                www = UnityWebRequest.Get(url);
                break;
        }
        //certificat workaround
        www.certificateHandler = new ForceAcceptAll();

        //set http header
        foreach (Header h in header)
        {
            www.SetRequestHeader(h.name, h.value);
        }

        yield return www.SendWebRequest();

        if (www.isNetworkError)
        {
            Debug.LogError(www.error);
            callbackOnFail?.Invoke("NetworkError");
        }
        else if (www.isHttpError && www.responseCode >= 500)
        {
            Debug.LogError(www.error);
            callbackOnFail?.Invoke("ServerError");
        }
        else if (www.isHttpError && www.responseCode == 401)
        {
            Debug.LogError(www.error);
            callbackOnFail?.Invoke("Unauthorised");
        }
        else if (www.isHttpError)
        {
            Debug.LogError(www.error);
            callbackOnFail?.Invoke(www.error);
        }
        else
        {
            ParseResponse(www.downloadHandler.text, callbackOnSuccess, callbackOnFail);
        }
    }
    //workaround bis certifikat trusted
    public class ForceAcceptAll : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
    /// <summary>
    /// This method finishes request process as we have received answer from server.
    /// </summary>
    /// <param name="data">Data received from server in JSON format.</param>
    /// <param name="callbackOnSuccess">Callback on success.</param>
    /// <param name="callbackOnFail">Callback on fail.</param>
    /// <typeparam name="T">Data Model Type.</typeparam>
    private void ParseResponse<T>(string data, UnityAction<T> callbackOnSuccess, UnityAction<string> callbackOnFail)
    {
        var parsedData = JsonUtility.FromJson<T>(data);
        callbackOnSuccess?.Invoke(parsedData);
    }

    #endregion

    #region [API]


    /// <summary>
    /// This method call server API to login via username and password
    /// </summary>
    /// <param name="callbackOnSuccess">Callback on success.</param>
    /// <param name="callbackOnFail">Callback on fail.</param>
    public void GetSocialWorkerAuthentification(UnityAction<AuthTokenAPI> callbackOnSuccess, UnityAction<string> callbackOnFail, string username, string password)
    {
        Header[] header = new Header[2];
        header[0].name = "username";
        header[0].value = username;
        header[1].name = "password";
        header[1].value = password;
        SendRequest(PaganiniRestAPI.Path.Authenticate, callbackOnSuccess, callbackOnFail, header);
    }







    #endregion
}