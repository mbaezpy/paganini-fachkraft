using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Valid;
using NetTopologySuite.Simplify;
using UnityEngine;

namespace LocationTools
{
    public enum NavigationIssue
    {
        Deviation = 1,
        WrongDirection = 2,
        MissedTurn = 3,
        WrongTurn = 4
    }

    public static class GPSSmooth
    {
        public static List<Pathpoint> Smooth(List<Pathpoint> coordinates, int windowSize)
        {
            var smoothed = new List<Pathpoint>();

            for (int i = 0; i < coordinates.Count; i++)
            {
                // Calculate the average of the coordinates in the window
                double sumLat = 0;
                double sumLon = 0;
                double sumAcc = 0;
                int count = 0;
                for (int j = i - windowSize; j <= i + windowSize; j++)
                {
                    if (j >= 0 && j < coordinates.Count)
                    {
                        sumLat += coordinates[j].Latitude;
                        sumLon += coordinates[j].Longitude;
                        sumAcc += coordinates[j].Accuracy;
                        count++;
                    }
                }
                double avgLat = sumLat / count;
                double avgLon = sumLon / count;
                double avgAcc = sumAcc / count;

                // Add the average coordinate to the smoothed list
                smoothed.Add(new Pathpoint
                {
                    Latitude = avgLat,
                    Longitude = avgLon,
                    Accuracy = avgAcc
                });
            }

            return smoothed;
        }

        public static List<Pathpoint> SmoothInterpolation(List<Pathpoint> coordinates, int numInterpolatedPoints)
        {
            // Create a list of the interpolated coordinates
            var interpolated = new List<Pathpoint>();

            // Extract the latitude and longitude values from the coordinates
            double[] lats = new double[coordinates.Count];
            double[] lons = new double[coordinates.Count];
            for (int i = 0; i < coordinates.Count; i++)
            {
                lats[i] = coordinates[i].Latitude;
                lons[i] = coordinates[i].Longitude;
            }

            // Create a spline interpolation for the latitude and longitude values
            CubicSpline latSpline = CubicSpline.InterpolateAkimaSorted(lats, lats);
            CubicSpline lonSpline = CubicSpline.InterpolateAkimaSorted(lons, lons);

            // Interpolate the latitude and longitude values at evenly-spaced intervals



            for (int i = 0; i <= numInterpolatedPoints; i++)
            {
                double t = (double)i / numInterpolatedPoints;
                interpolated.Add(new Pathpoint
                {
                    Latitude = latSpline.Interpolate(t),
                    Longitude = lonSpline.Interpolate(t)
                });
            }

            return interpolated;
        }

        public static List<Pathpoint> SmoothKalman(List<Pathpoint> coordinates)
        {
            var smoothed = new List<Pathpoint>();

            // Initialize the Kalman filter
            var state = Matrix<double>.Build.Dense(4, 1);
            var errorCovariance = Matrix<double>.Build.DenseIdentity(4);
            var processNoise = Matrix<double>.Build.Dense(4, 4);
            var measurementNoise = Matrix<double>.Build.Dense(2, 2);
            var transitionMatrix = Matrix<double>.Build.Dense(4, 4);
            var measurementMatrix = Matrix<double>.Build.Dense(2, 4);

            // Set the process and measurement noise matrices
            processNoise[0, 0] = 0.1;
            processNoise[1, 1] = 0.1;
            processNoise[2, 2] = 0.1;
            processNoise[3, 3] = 0.1;
            measurementNoise[0, 0] = 0.1;
            measurementNoise[1, 1] = 0.1;

            // Set the transition matrix
            transitionMatrix[0, 0] = 1;
            transitionMatrix[0, 1] = 1;
            transitionMatrix[1, 0] = 1;
            transitionMatrix[1, 1] = 1;
            transitionMatrix[2, 2] = 1;
            transitionMatrix[2, 3] = 1;
            transitionMatrix[3, 2] = 1;
            transitionMatrix[3, 3] = 1;

            // Set the measurement matrix
            measurementMatrix[0, 0] = 1;
            measurementMatrix[0, 2] = 1;
            measurementMatrix[1, 1] = 1;
            measurementMatrix[1, 3] = 1;

            // Iterate over the coordinates
            foreach (var coord in coordinates)
            {
                // Predict the next state
                state = transitionMatrix * state;
                errorCovariance = transitionMatrix * errorCovariance * transitionMatrix.Transpose() + processNoise;

                // Compute the Kalman gain
                var measurement = Matrix<double>.Build.Dense(2, 1);
                measurement[0, 0] = coord.Latitude;
                measurement[1, 0] = coord.Longitude;

                var innovation = measurement - measurementMatrix * state;
                var innovationCovariance = measurementMatrix * errorCovariance * measurementMatrix.Transpose() + measurementNoise;
                var kalmanGain = errorCovariance * measurementMatrix.Transpose() * innovationCovariance.Inverse();

                // Update the state and error covariance
                state += kalmanGain * innovation;
                errorCovariance = (Matrix<double>.Build.DenseIdentity(4) - kalmanGain * measurementMatrix) * errorCovariance;

                // Add the smoothed coordinate to the result list
                smoothed.Add(new Pathpoint
                {
                    Latitude = state[0, 0],
                    Longitude = state[1, 0],
                    Accuracy = Math.Sqrt(errorCovariance[0, 0] + errorCovariance[1, 1])
                });
            }

            return smoothed;
        }


        public static List<Pathpoint> SimplifyTrack(List<Pathpoint> coordinates, double tolerance)
        {

            List<Coordinate> list = new List<Coordinate>();
            List<Pathpoint> simplified = new List<Pathpoint>();

            for (int i = 1; i < coordinates.Count - 1; i++)
            {
                list.Add(new Coordinate(coordinates[i].Latitude, coordinates[i].Longitude));                                    
            }

            var geomFactory = new GeometryFactory();
            var lineString = geomFactory.CreateLineString(list.ToArray());
            Geometry output = NetTopologySuite.Simplify.TopologyPreservingSimplifier.Simplify(lineString, tolerance);

            Debug.Log("TopologyPreservingSimplifier - Count: " + output.Coordinates.Count());

            foreach (var coordinate in output.Coordinates.ToList())
            {
                simplified.Add(new Pathpoint
                {
                    Latitude = coordinate[0],
                    Longitude = coordinate[1]
                });
            }

            return simplified;
        }

        public static List<Pathpoint> SimplifyTrackVWSimplifier(List<Pathpoint> pathpoints, double tolerance)
        {
            
            List<Coordinate> list = new List<Coordinate>();
            List<Pathpoint> simplified = new List<Pathpoint>();
            Dictionary<Coordinate, Pathpoint> CoordToPathpoint = new Dictionary<Coordinate, Pathpoint> { };

            // creating a list of coordinates from pathpoint
            for (int i = 1; i < pathpoints.Count - 1; i++)
            {
                var coord = new Coordinate(pathpoints[i].Latitude, pathpoints[i].Longitude);
                list.Add(coord);

                if (!CoordToPathpoint.ContainsKey(coord))
                {
                    CoordToPathpoint.Add(coord, pathpoints[i]);
                }
                
            }

            // we need at least 2 points to continue
            if (list.Count < 2) return pathpoints;

            var geomFactory = new GeometryFactory();
            var lineString = geomFactory.CreateLineString(list.ToArray());
            var simplifier = new VWSimplifier(lineString);
            simplifier.DistanceTolerance = tolerance;
            Geometry output = simplifier.GetResultGeometry();

            foreach (var coordinate in output.Coordinates.ToList())
            {
                simplified.Add(CoordToPathpoint[coordinate]);
            }

            return simplified;
        }

        public static List<Pathpoint> SimplifyTrackDouglasPeucker(List<Pathpoint> coordinates, double tolerance)
        {
            List<Coordinate> list = new List<Coordinate>();
            List<Pathpoint> simplified = new List<Pathpoint>();

            for (int i = 1; i < coordinates.Count - 1; i++)
            {
                list.Add(new Coordinate(coordinates[i].Latitude, coordinates[i].Longitude));
            }

            var geomFactory = new GeometryFactory();
            var lineString = geomFactory.CreateLineString(list.ToArray());
            var simplifier = new DouglasPeuckerSimplifier(lineString);
            

            simplifier.DistanceTolerance = tolerance;
            Geometry output = simplifier.GetResultGeometry();

            foreach (var coordinate in output.Coordinates.ToList())
            {
                simplified.Add(new Pathpoint
                {
                    Latitude = coordinate[0],
                    Longitude = coordinate[1]
                });
            }

            return simplified;
        }


        /// <summary>
        /// Estimates the tolerance value for the Simplification functions based on the characteristics of the coordinates.
        /// The estimation is based on a fraction of the standard deviation of the coordinates,
        /// computed as std(coordinates) * distanceFraction
        /// </summary>
        /// <param name="coordinates">The list of GPS coordinates.</param>
        /// <param name="distanceFraction">The fraction (0-1] of the STD to consider</param>
        /// <returns>The standard deviation of the coordinates.</returns>
        public static double EstimateSimplicationDistanceTolerance(List<Pathpoint> coordinates, double distanceFraction)
        {
            // Calculate the mean of the coordinates
            double meanLatitude = coordinates.Average(c => c.Latitude);
            double meanLongitude = coordinates.Average(c => c.Longitude);

            // Calculate the mean of the squared differences
            double varianceLatitude = coordinates.Average(c => (c.Latitude - meanLatitude) * (c.Latitude - meanLatitude));
            double varianceLongitude = coordinates.Average(c => (c.Longitude - meanLongitude) * (c.Longitude - meanLongitude));
            double variance = (varianceLatitude + varianceLongitude) / 2;

            // Calculate the standard deviation
            return distanceFraction * Math.Sqrt(variance);
        }




        public static List<Pathpoint> ClusterPoints(List<Pathpoint> points, double distanceThreshold)
        {
            List<Pathpoint> clusteredPoints = new List<Pathpoint>();

            int currentClusterStart = 0;

            while (currentClusterStart < points.Count)
            {
                Pathpoint currentCluster = points[currentClusterStart];

                if (currentCluster.PhotoFilenames == null)
                {
                    currentCluster.PhotoFilenames = new List<Pathpoint.PhotoMetaData>();                    
                }

                currentCluster.PhotoFilenames.Add(new Pathpoint.PhotoMetaData(currentCluster.Timestamp, currentCluster.PhotoFilename));

                // we don't cluster start and the destination, as they are associated to a specific picture via the interface
                if (currentCluster.POIType != Pathpoint.POIsType.WayStart && currentCluster.POIType != Pathpoint.POIsType.WayDestination) {

                    // Look for other points within the distance threshold in the window
                    for (int  i = currentClusterStart + 1; i < points.Count; i++)
                    {
                        Pathpoint candidatePoint = points[i];
                        // we don't cluster the destination, that's a separate point
                        if (candidatePoint.POIType == Pathpoint.POIsType.WayDestination){
                            break;
                        }

                        double distance = GPSUtils.HaversineDistance(currentCluster, candidatePoint);                    

                        if (distance <= distanceThreshold)
                        {
                            // Add the candidate point to the current cluster
                            currentCluster.PhotoFilenames.Add(new Pathpoint.PhotoMetaData(candidatePoint.Timestamp, candidatePoint.PhotoFilename));
                            currentClusterStart++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                currentClusterStart++;

                // Add the current cluster to the list of clustered points
                clusteredPoints.Add(currentCluster);
            }

            return clusteredPoints;
        }



    }


    public class GPSCleaningPipeline
    {

        public double ToleranceSimplify = 0.001;
        public double MaxAccuracyRadio = 15;
        public double DistanceOutlier = 100;
        public int SegmentSplit = 300;
        public double OutlierFactor = 1.5;
        public double MinEvenly = 2;
        public double MaxEvenly = 3;
        public double POIClusterDistance = 10;


        public GPSCleaningPipeline() { }


        public  List<Pathpoint> CleanRoute(List<Pathpoint> points)
        {
            // select only points
            var ppList = points.Where(item => item.POIType == Pathpoint.POIsType.Point).ToList(); 

            // select only POIs
            List<Pathpoint> POIList = points.Where(item => item.POIType != Pathpoint.POIsType.Point).OrderBy(item => item.TimeInVideo).ToList();
            POIList = LocationTools.GPSSmooth.ClusterPoints(POIList, POIClusterDistance);


            //// keep track of where to insert POIs?
            var tolerance = GPSSmooth.EstimateSimplicationDistanceTolerance(ppList, ToleranceSimplify);
            ppList = LocationTools.GPSUtils.RemoveInnacuratePoints(ppList, MaxAccuracyRadio);
            ppList = LocationTools.GPSUtils.RemoveOutliers(ppList, DistanceOutlier);
            ppList = LocationTools.GPSUtils.RemoveSelfIntersections(ppList);
            //ppList = LocationUtils.GPSUtils.RemoveInnacuratePointsBasedOnData(ppList, SegmentSplit, OutlierFactor);
            //ppList = LocationUtils.GPSSmooth.SimplifyTrackVWSimplifier(ppList, tolerance);
            ppList = LocationTools.GPSUtils.EvenlySpaced(ppList, MinEvenly, MaxEvenly);

            // merge
            ppList = MergeSortedLists(POIList, ppList);

            // Remove all the 'points'  in the list that appear before the first landmark
            // which will become the start
            int removeIndex = ppList.FindIndex(p => p.POIType != Pathpoint.POIsType.Point);
            if (removeIndex > 0)
            {
                ppList.RemoveRange(0, removeIndex);
            }

            return ppList;
        }

        /// <summary>
        /// Merges two sorted lists of Pathpoint objects and sorts the resulting list based on the Timestamp attribute.
        /// </summary>
        /// <param name="ListA">The first sorted list of Pathpoint objects to be merged.</param>
        /// <param name="ListB">The second sorted list of Pathpoint objects to be merged.</param>
        /// <returns>A sorted list of Pathpoint objects that combines the elements of the input lists.</returns>
        private  List<Pathpoint> MergeSortedLists(List<Pathpoint> ListA, List<Pathpoint> ListB)
        {
            int i = 0, j = 0;
            var mergedList = new List<Pathpoint>();

            while (i < ListA.Count && j < ListB.Count)
            {
                if (ListA[i].Timestamp < ListB[j].Timestamp)
                {
                    mergedList.Add(ListA[i]);
                    i++;
                }
                else
                {
                    mergedList.Add(ListB[j]);
                    j++;
                }
            }

            // Append any remaining elements from POIList
            while (i < ListA.Count)
            {
                mergedList.Add(ListA[i]);
                i++;
            }

            // Append any remaining elements from ppList
            while (j < ListB.Count)
            {
                mergedList.Add(ListB[j]);
                j++;
            }

            // Sort the merged list based on Timestamp
            mergedList.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));

            return mergedList;
        }
    }


    public static class GPSUtils
    {
        private const double EARTH_RADIUS = 6371e3; // Earth radius in meters

        public static double HaversineDistance(Pathpoint coord1, Pathpoint coord2)
        {
            double lat1 = coord1.Latitude * Math.PI / 180;
            double lon1 = coord1.Longitude * Math.PI / 180;
            double lat2 = coord2.Latitude * Math.PI / 180;
            double lon2 = coord2.Longitude * Math.PI / 180;

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EARTH_RADIUS * c;
        }

        public static List<Pathpoint> EvenlySpaced(List<Pathpoint> coordinates, double minDistance, double maxDistance)
        {
            // if not enough points to evenly distribute, return the input list
            if (coordinates.Count < 3) return coordinates;

            var spaced = new List<Pathpoint>();
            spaced.Add(coordinates[0]); // Add the first point to the result list

            for (int i = 1; i < coordinates.Count; i++)
            {
                Pathpoint prevCoord = spaced[spaced.Count - 1];
                double distance = HaversineDistance(prevCoord, coordinates[i]);
                if (distance > maxDistance)
                {
                    // Compute the number of evenly spaced points between the current point and the previous point
                    int numPoints = (int)Math.Floor(distance / maxDistance);

                    // Compute the latitude and longitude differences between the current point and the previous point
                    double latDiff = (coordinates[i].Latitude - prevCoord.Latitude) / numPoints;
                    double lonDiff = (coordinates[i].Longitude - prevCoord.Longitude) / numPoints;
                    double altDiff = (coordinates[i].Altitude - prevCoord.Altitude) / numPoints;
                    double accDiff = (coordinates[i].Accuracy - prevCoord.Accuracy) / numPoints;

                    // Compute the time difference between the current point and the previous point
                    double timeDiff = coordinates[i].Timestamp - prevCoord.Timestamp;

                    Debug.Log("Distance: " + distance + " numPoints->" + numPoints);

                    // Add evenly spaced points between the current point and the previous point
                    for (int j = 1; j < numPoints; j++)
                    {
                        spaced.Add(new Pathpoint
                        {
                            Latitude = prevCoord.Latitude + latDiff * j,
                            Longitude = prevCoord.Longitude + lonDiff * j,
                            Altitude = prevCoord.Altitude + altDiff * j,
                            Accuracy = prevCoord.Accuracy + accDiff * j,
                            POIType = Pathpoint.POIsType.Point,
                            Timestamp = prevCoord.Timestamp + (long)(timeDiff * j / numPoints)
                        });
                    }
                } else if (distance > minDistance) {
                    spaced.Add(coordinates[i]);
                }

                
            }
            return spaced;
        }

        public static List<Pathpoint> RemoveInnacuratePoints(List<Pathpoint> coordinates, double maxAccuracyRadio)
        {
            List<Pathpoint> list = new List<Pathpoint>();

            for (int i = 1; i < coordinates.Count - 1; i++)
            {
                if (coordinates[i].Accuracy < maxAccuracyRadio)
                {
                    list.Add(coordinates[i]);
                }
            }

            return list;
        }

        public static List<Pathpoint> RemoveInnacuratePointsBasedOnData(List<Pathpoint> coordinates, int maxSegments, double outlierFactor)
        {

            List<Pathpoint> outliers = IdentifyInnacuratePoints(coordinates, maxSegments, outlierFactor);

            Debug.Log($"TOTAL OUTLIERS: {outliers.Count} COORDINATES: {coordinates.Count}");

            coordinates.RemoveAll(outliers.Contains);
            //outliers.ForEach(point => point.Accuracy = 100);

            Debug.Log($"TOTAL OUTLIERS: {outliers.Count} COORDINATES (cleaned): {coordinates.Count}");
            return coordinates;
        }

        private static List<Pathpoint> IdentifyInnacuratePoints(List<Pathpoint> coordinates, int maxSegments, double outlierFactor)
        {
            if (coordinates.Count <= maxSegments)
            {            
                double[] accuracies = coordinates.Select(point => point.Accuracy).ToArray();

                double q1 = Statistics.LowerQuartile(accuracies);
                double q3 = Statistics.UpperQuartile(accuracies);

                double outlierThreshold = q3 + (q3 - q1) * outlierFactor;

                Debug.Log($"Outlier threshold ->{outlierThreshold}");
                List<Pathpoint> list = new List<Pathpoint>();

                for (int i = 0; i < coordinates.Count; i++)
                {
                    
                    if (coordinates[i].Accuracy > outlierThreshold)
                    {
                        list.Add(coordinates[i]);
                        Debug.Log($"[{i}] Accuracy ->{coordinates[i].Accuracy}");
                    }
                }

                return list;
            }
            int mid = coordinates.Count / 2;
            Debug.Log($"We will split, PATH size:{coordinates.Count} MID: [{mid}]");
            var out1 = IdentifyInnacuratePoints(coordinates.GetRange(0, mid), maxSegments, outlierFactor);
            var out2 = IdentifyInnacuratePoints(coordinates.GetRange(mid, coordinates.Count - mid), maxSegments, outlierFactor);

            return out1.Concat(out2).ToList();
        }

        

        public static List<Pathpoint> RemoveOutliers(List<Pathpoint> coordinates, double maxDistance)
        {
            List<Pathpoint> list = IdentifyOutliers(coordinates, maxDistance);
            coordinates.RemoveAll(list.Contains);
            return coordinates;
        }

        public static List<Pathpoint> IdentifyOutliers(List<Pathpoint> coordinates, double maxDistance)
        {
            double _maxDistance = 0;
            var outliers = new List<Pathpoint>();

            for (int i = 1; i < coordinates.Count - 1; i++)
            {
                // Compute the distances from the current point to the previous and next points
                double prevDistance = HaversineDistance(coordinates[i - 1], coordinates[i]);
                double nextDistance = HaversineDistance(coordinates[i + 1], coordinates[i]);

                //Debug.Log(i + ": prevDistance->" + prevDistance + " nextDistance->" + nextDistance);

                if (_maxDistance < prevDistance)
                {
                    _maxDistance = prevDistance;
                }
                if (_maxDistance < nextDistance)
                {
                    _maxDistance = nextDistance;
                }

                // If either distance is greater than the maximum allowed distance, add the current point to the outliers list
                if (prevDistance > maxDistance && nextDistance > maxDistance)
                {
                    outliers.Add(coordinates[i]);
                }
                // First one is the outlier
                else if (prevDistance > maxDistance && i==1)
                {
                    outliers.Add(coordinates[i-1]);
                }
                // Checking if the last point is the outlier
                else if (nextDistance > maxDistance && (i== coordinates.Count-2))
                {
                    outliers.Add(coordinates[i + 1]);
                }
                else if (prevDistance > maxDistance && !outliers.Contains(coordinates[i - 1])){
                    outliers.Add(coordinates[i]);
                }

            }

            Debug.Log("MaxDistance between pairs: " + _maxDistance);

            return outliers;
        }


        public static List<Pathpoint> RemoveSelfIntersections(List<Pathpoint> coordinates)
        {
            // if there are not enough points for an intersection, return the same list
            if (coordinates.Count < 4) return coordinates;

            var cleaned = new List<Pathpoint>();

            // Check every pair of coordinates for intersections
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                cleaned.Add(coordinates[i]);

                for (int j = i + 2; j < coordinates.Count - 1; j++)
                {
                    //Debug.Log($"Checking :[{i}-{i+1}] - [{j}-{j + 1}]");
                    if (LinesIntersect(coordinates[i], coordinates[i + 1], coordinates[j], coordinates[j + 1]))
                    {
                        Debug.Log($"({i}) Removing :{i + 1} ({coordinates[i + 1]}) to {j} ({coordinates[j]})");
                        coordinates.RemoveRange(i + 1, j - i - 1);
                        break;
                    }
                }

            }

            // Add the last coordinate to the cleaned list
            cleaned.Add(coordinates[coordinates.Count - 1]);

            return cleaned;
        }

        /// <summary>
        /// Determines whether the line segments defined by the given points intersect.
        /// </summary>
        /// <param name="p1">The first point of the first line segment.</param>
        /// <param name="p2">The second point of the first line segment.</param>
        /// <param name="p3">The first point of the second line segment.</param>
        /// <param name="p4">The second point of the second line segment.</param>
        /// <returns>True if the line segments intersect, false otherwise.</returns>
        private static bool LinesIntersect(Pathpoint p1, Pathpoint p2, Pathpoint p3, Pathpoint p4)
        {
            // Compute the line equations
            double a1 = p2.Longitude - p1.Longitude;
            double b1 = p1.Latitude - p2.Latitude;
            double c1 = a1 * p1.Latitude + b1 * p1.Longitude;

            double a2 = p4.Longitude - p3.Longitude;
            double b2 = p3.Latitude - p4.Latitude;
            double c2 = a2 * p3.Latitude + b2 * p3.Longitude;

            // Compute the intersection point
            double determinant = a1 * b2 - a2 * b1;
            if (determinant == 0)
            {
                return false; // Lines are parallel
            }
            else
            {
                double x = (b2 * c1 - b1 * c2) / determinant;
                double y = (a1 * c2 - a2 * c1) / determinant;

                // Check if the intersection point lies within the line segments
                if (Math.Min(p1.Latitude, p2.Latitude) <= x && x <= Math.Max(p1.Latitude, p2.Latitude) &&
                    Math.Min(p3.Latitude, p4.Latitude) <= x && x <= Math.Max(p3.Latitude, p4.Latitude) &&
                    Math.Min(p1.Longitude, p2.Longitude) <= y && y <= Math.Max(p1.Longitude, p2.Longitude) &&
                    Math.Min(p3.Longitude, p4.Longitude) <= y && y <= Math.Max(p3.Longitude, p4.Longitude))
                {
                    return true; // Lines intersect
                }
                else
                {
                    return false; // Lines do not intersect
                }
            }
        }

        public static List<Pathpoint> RemoveInvalidCoordinates(List<Pathpoint> coordinates)
        {
            List<Coordinate> list = new List<Coordinate>();
            var cleaned = new List<Pathpoint>();

            for (int i = 1; i < coordinates.Count - 1; i++)
            {
                list.Add(new Coordinate(coordinates[i].Latitude, coordinates[i].Longitude));
            }


            // Create a LineString from the input coordinates
            var lineString = new LineString(list.ToArray());

            // Check if the LineString is valid
            var validator = new IsValidOp(lineString);


            if (!validator.IsValid)
            {
                // If the LineString is not valid, get the invalid component
                var invalidComponent = validator.ValidationError;
                Debug.Log(invalidComponent.Message);

                list.Remove(invalidComponent.Coordinate);

                foreach (var coordinate in list)
                {
                    cleaned.Add(new Pathpoint
                    {
                        Latitude = coordinate[0],
                        Longitude = coordinate[1]
                    });
                }
            }
            else
            {
                Debug.Log("No errors were identified in the coordinates");
                // If the LineString is valid, return the original coordinates
                cleaned = coordinates;
            }
            return cleaned;
        }


    }


    public static class GPSStats
    {

        public class Stats
        {
            public double Mean { get; set; }
            public double Median { get; set; }
            public double Mode { get; set; }
            public double Minimum { get; set; }
            public double Maximum { get; set; }
            public double Range { get; set; }
            public double Variance { get; set; }
            public double StandardDeviation { get; set; }
            public double OutlierThreshold { get; set; }
        }

        public static Stats BasicStats(List<Pathpoint> coordinates)
        {
            var gpsStats = new Stats();
            double[] accuracies = coordinates.Select(point => point.Accuracy).ToArray();
            var stats = new DescriptiveStatistics(accuracies);

            gpsStats.Mean = stats.Mean;
            gpsStats.Median = Statistics.Median(accuracies);

            double q1 = Statistics.LowerQuartile(accuracies);
            double q3 =  Statistics.UpperQuartile(accuracies);

            gpsStats.OutlierThreshold = q3 + (q3 - q1) * 1.5;

            return gpsStats;

        }


    }


}
