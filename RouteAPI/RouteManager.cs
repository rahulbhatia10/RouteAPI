﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using GrapgDS;
using RouteAPI.DataAccess;
using RouteAPI.DataAccess.Entities;
using RouteAPI.Entities;
using RouteAPI.Exceptions;

namespace RouteAPI
{
    public class RouteManager : IRouteManager
    {
        private readonly ILandMarkManager _manager;
        private readonly IRoutesRepository _routesRepository;

        private HashSet<Landmark> _landmarks = new HashSet<Landmark>();

        public RouteManager(ILandMarkManager manager, IRoutesRepository routesRepository)
        {
            _manager = manager;
            _routesRepository = routesRepository;
        }

        public Route RegisterRoute(string from, string to, int weightedDistance)
        {
            try
            {
                if (string.Equals(to, from, StringComparison.InvariantCultureIgnoreCase))
                    throw new RouteException(HttpStatusCode.BadRequest, Constants.ExceptionMessageForInvalidRoute);

                if (_routesRepository.GetRoute(from, to) != null)
                    throw new RouteException(HttpStatusCode.BadRequest,
                        Constants.ExceptionMessageWhenRouteAlreadyExists);

                if (!_manager.UpdateNeighbours(from, to))
                    throw new Exception();

                return _routesRepository.SaveRoute(from, to, weightedDistance);

            }
            catch (RouteException)
            {
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw new RouteException(HttpStatusCode.InternalServerError, Constants.ServerError);
            }
        }

        public int GetDistance(string route)
        {
            if (!IsRouteValid(route))
                throw new RouteException(HttpStatusCode.BadRequest, Constants.ExceptionMessageWhenRouteDoesNotExists);

            int distance = 0;
            var paths = route.Split("-");
            for (var i = 0; i < paths.Length - 1; i++)
            {
                var path = _routesRepository.GetRoute(paths[i], paths[i + 1]);
                if (path == null)
                    throw new RouteException(HttpStatusCode.BadRequest,
                        Constants.ExceptionMessageWhenRouteDoesNotExists);
                distance += path.Distance;
            }
            return distance;
        }

        private IEnumerable<Route> GetPathsForRoute(string incomingRoute)
        {
            var routes = new List<Route>();
            var paths = incomingRoute.Split("-");
            for (var i = 0; i < paths.Length - 1; i++)
            {
                var route = _routesRepository.GetRoute(paths[i], paths[i + 1]);
                if (route == null)
                    return null;
                routes.Add(route);
            }
            return routes;
        }

        public IEnumerable<Route> GetAllRoutes()
        {
            return _routesRepository.GetRoutes();
        }

        public int GetRoutesForLandMarksWithSpecifiedNumberOfHops(string origin, string destination, int maxHops)
        {
            var fromLandmark = _manager.GetLandmark(origin);

            if (fromLandmark == null)
                throw new RouteException(HttpStatusCode.BadRequest, Constants.ExceptionMessageWhenRouteDoesNotExists);

            var toLandmark = _manager.GetLandmark(destination);

            if (toLandmark == null)
                throw new RouteException(HttpStatusCode.BadRequest, Constants.ExceptionMessageWhenRouteDoesNotExists);

            var routes = GetRoutes(fromLandmark, toLandmark);
            return routes.Count(r => r.Length <= maxHops + 2);
        }

        public void Remove()
        {
            _routesRepository.RemoveAll();
        }

        public void RemoveRoute(string @from, string to)
        {
            _routesRepository.Remove(from, to);
        }

        #region Helper methods

        internal IEnumerable<string> GetRoutes(Landmark origin, Landmark destination)
        {
            // Collection to back track landmarks
            var backTrackPaths = new Dictionary<string, List<string>>();
            backTrackPaths.Add(origin.Name, new List<string>() { origin.Name });

            Queue<Landmark> queue = new Queue<Landmark>();
            HashSet<string> results = new HashSet<string>();
            queue.Enqueue(origin);
            HashSet<string> isVisited = new HashSet<string>();

            while (queue.Count > 0)
            {
                var source = queue.Dequeue();
                foreach (Landmark neighbor in source.AdjacentLandmarks)
                {
                    foreach (var path in backTrackPaths[source.Name].ToList())
                    {
                        if (!backTrackPaths.ContainsKey(neighbor.Name))
                        {
                            backTrackPaths.Add(neighbor.Name, new List<string>() { path + neighbor.Name });
                        }
                        else
                        {
                            if (!backTrackPaths[neighbor.Name].Any(p => path.Contains(neighbor.Name)))
                            {
                                backTrackPaths[neighbor.Name].Add(path + neighbor.Name);
                            }
                        }
                    }

                    if (neighbor.Equals(destination))
                    {
                        results.UnionWith(backTrackPaths[neighbor.Name]);
                        continue;
                    }

                    var route = source.Name + neighbor.Name;

                    if (!isVisited.Contains(route) && neighbor.Name != origin.Name)
                    {
                        isVisited.Add(route);
                        queue.Enqueue(neighbor);
                    }

                }
            }

            return results;
        }

      

        

        internal bool IsRouteValid(string route)
        {
            if (string.IsNullOrEmpty(route))
                return false;

            var regex = @"((\w)+-(\w)+)";
            var isRouteValid = Regex.IsMatch(route, regex);
            if (!isRouteValid)
                return false;

            var paths = route.Split("-");
            var uniquePaths = paths.Distinct().ToArray();

            var landmarks = uniquePaths.Select(lm => _manager.GetLandmark(lm));

            if (uniquePaths.Count() < 2 || landmarks.Any(lm => lm == null)
                                        || uniquePaths.Count() != paths.Length)
                return false;

            return true;
        }

        #endregion
    }
}
