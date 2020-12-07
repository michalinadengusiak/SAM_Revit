﻿using Autodesk.Revit.DB;
using SAM.Core.Revit;
using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Analytical.Revit
{
    public static partial class Convert
    {
        public static List<Element> ToRevit(this AdjacencyCluster adjacencyCluster, Document document, ConvertSettings convertSettings)
        {
            if (adjacencyCluster == null || document == null)
                return null;

            List<Element> result = convertSettings?.GetObjects<Element>(adjacencyCluster.Guid);
            if (result != null)
                return result;

            Dictionary<Space, Shell> dictionary = adjacencyCluster.ShellDictionary();
            if (dictionary == null)
                return null;

            //List<Level> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().ToList();
            //if (levels == null || levels.Count == 0)
            //    return null;

            result = new List<Element>();

            HashSet<System.Guid> guids = new HashSet<System.Guid>();
            foreach (KeyValuePair<Space, Shell> keyValuePair in dictionary)
            {
                Space space = keyValuePair.Key;

                List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                if (panels_Space != null && panels_Space.Count != 0)
                {
                    foreach (Panel panel in panels_Space)
                    {
                        if (guids.Contains(panel.Guid))
                            continue;

                        guids.Add(panel.Guid);

                        HostObject hostObject = panel.ToRevit(document, convertSettings);
                        if (hostObject == null)
                            continue;

                        result.Add(hostObject);
                    }
                }

                Autodesk.Revit.DB.Mechanical.Space space_Revit = space.ToRevit(document, convertSettings);
                if (space_Revit == null)
                    continue;

                result.Add(space_Revit);

                BoundingBox3D boundingBox3D = keyValuePair.Value.GetBoundingBox();
                if (boundingBox3D == null)
                    continue;

                Parameter parameter;

                parameter = space_Revit.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL);
                Level level = document.ClosestLevel(boundingBox3D.Max.Z);
                if (level == null)
                    continue;

                parameter.Set(level.Id);

                if (level.Id != space_Revit.LevelId && level.Elevation > (document.GetElement(space_Revit.LevelId) as Level).Elevation)
                {
                    parameter = space_Revit.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET);
                    parameter.Set(0);
                }
            }

            List<Panel> panels = adjacencyCluster.GetShadingPanels();
            if(panels != null && panels.Count != 0)
            {
                foreach(Panel panel in panels)
                {
                    HostObject hostObject = panel.ToRevit(document, convertSettings);
                    if (hostObject == null)
                        continue;

                    result.Add(hostObject);
                }
            }

            convertSettings?.Add(adjacencyCluster.Guid, result);

            return result;
        }
    }
}