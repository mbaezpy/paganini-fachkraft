using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppState 
{

    public static string APIToken = null;
    public static SocialWorker CurrenSocialWorker = null;

    public static int ScreenSleepTimeout = Screen.sleepTimeout;

    public static User CurrentUser = null;
    public static Route CurrentRoute = null;
    public static Way CurrentWay = null;

}
