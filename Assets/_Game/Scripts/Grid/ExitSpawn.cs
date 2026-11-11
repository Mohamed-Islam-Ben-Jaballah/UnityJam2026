using System;
using UnityEngine;

namespace DiceDrive.Grid
{
    public enum DestinationKind
    {
        Map,
        Prefab
    }

    [Serializable]
    public struct MapDestination
    {
        public DestinationKind kind;
        public MapData map;
        public ThemeData theme;
        public GameObject prefab;

        public MapData ResolveMap()
        {
            if (kind == DestinationKind.Map)
            {
                return map;
            }

            var view = ResolveView();
            return view != null ? view.mapData : null;
        }

        public ThemeData ResolveTheme()
        {
            if (kind == DestinationKind.Map)
            {
                return theme;
            }

            var view = ResolveView();
            return view != null ? view.themeData : null;
        }

        public bool IsValid
        {
            get
            {
                if (kind == DestinationKind.Map)
                {
                    return map != null;
                }

                return ResolveMap() != null;
            }
        }

        private MapView ResolveView()
        {
            return prefab != null ? prefab.GetComponentInChildren<MapView>(true) : null;
        }
    }

    [Serializable]
    public struct ExitSpawn
    {
        public GridCoord coord;
        public MapDestination destination;
    }
}
