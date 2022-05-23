﻿using NinevaStudios.GoogleMaps;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeMarkerObject : MonoBehaviour
{
    public Vector2 LatLng { get; set; }
    public int Timestamp { get; set; }
    public Marker Marker { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsPOI { get; set; }

    public string IdFromMarker { get; set; }

    public List<string> ImagesForPOI { get; set; }

    public TimeMarkerObject()
    {
        // initialize list for images
        ImagesForPOI = new List<string>();
    }
}
