﻿namespace RouteAPI
{
    public interface IRouteManager
    {
        /// <summary>
        /// Registers a Route 
        /// </summary>
        /// <param name="fromLandMark"></param>
        /// <param name="toLandMark"></param>
        /// <param name="weightedDistance"></param>
        /// <returns>Returns true if registered successfully</returns>
        bool RegisterRoute(string fromLandMark, string toLandMark, int weightedDistance);

        /// <summary>
        /// Gets the distance for a route
        /// </summary>
        /// <param name="route"></param>
        /// <returns>Int</returns>
        int GetDistance(string route);

        /// <summary>
        /// Gets the number of routes having maximum<param name="maxHops"></param> hops
        /// </summary>
        /// <param name="landmarks"></param>
        /// <param name="maxHops"></param>
        /// <returns></returns>
        int GetRoutesForLandMarksWithSpecifiedNumberOfHops(string[] landmarks, int maxHops);
    }
}