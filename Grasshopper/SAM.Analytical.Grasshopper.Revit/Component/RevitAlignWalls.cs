﻿using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;

using Autodesk.Revit.DB;

using SAM.Analytical.Grasshopper.Revit.Properties;
using SAM.Core.Revit;
using SAM.Geometry.Spatial;
using SAM.Geometry.Planar;
using SAM.Geometry.Revit;


namespace SAM.Analytical.Grasshopper.Revit
{
    public class RevitAlignWalls : GH_Component
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("40eea184-d5f1-45f3-a4cc-45e11ff510fb");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Revit;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public RevitAlignWalls()
          : base("Revit.AlignWalls", "Revit.AlignWalls",
              "Align Revit Walls",
              "SAM", "Revit")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            inputParamManager.AddParameter(new RhinoInside.Revit.GH.Parameters.Level(), "_level", "_level", "Revit Level", GH_ParamAccess.item);
            inputParamManager.AddParameter(new RhinoInside.Revit.GH.Parameters.Level(), "_referenceLevel", "_refLvl", "Revit Reference Level", GH_ParamAccess.item);
            inputParamManager.AddNumberParameter("_maxDistance", "_max", "Max Distance", GH_ParamAccess.item, 0.5);
            inputParamManager.AddBooleanParameter("_run_", "_run_", "Run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new RhinoInside.Revit.GH.Parameters.HostObject(), "Walls", "Walls", "Revit Walls", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            bool run = false;
            if (!dataAccess.GetData(3, ref run) || !run)
                return;

            double maxDistance = 0.5;
            if (!dataAccess.GetData(2, ref maxDistance))
                return;

            RhinoInside.Revit.GH.Types.Level level_GH = null;
            if (!dataAccess.GetData(0, ref level_GH))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            RhinoInside.Revit.GH.Types.Level referenceLevel_GH = null;
            if (!dataAccess.GetData(1, ref referenceLevel_GH))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Document document = level_GH.Document;

            Level level = document.GetElement(level_GH.Value) as Level;
            if(level == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Level referenceLevel = document.GetElement(referenceLevel_GH.Value) as Level;
            if (level == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double elevation = UnitUtils.ConvertFromInternalUnits(level.Elevation, DisplayUnitType.DUT_METERS);
            double referenceElevation = UnitUtils.ConvertFromInternalUnits(referenceLevel.Elevation, DisplayUnitType.DUT_METERS);

            IEnumerable<Wall> walls_All = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>();
            if (walls_All == null || walls_All.Count() == 0)
                return;

            List<Panel> panels = new List<Panel>();
            List<Panel> panels_Reference = new List<Panel>();
            foreach (Wall wall in walls_All)
            {
                List<Panel> panels_Temp = Analytical.Revit.Convert.ToSAM(wall);
                foreach (Panel panel in panels_Temp)
                {
                    double max = panel.MaxElevation();
                    double min = panel.MinElevation();

                    if (Math.Abs(min - elevation) < Core.Tolerance.Distance || (min - Core.Tolerance.Distance < elevation && max - Core.Tolerance.Distance > elevation))
                        panels.Add(panel);

                    if (Math.Abs(min - referenceElevation) < Core.Tolerance.Distance || (min - Core.Tolerance.Distance < referenceElevation && max - Core.Tolerance.Distance > referenceElevation))
                        panels_Reference.Add(panel);
                }

            }

            IEnumerable<ElementId> elementIds = panels.ConvertAll(x => x.ElementId()).Distinct();
            IEnumerable<ElementId> elementIds_Reference = panels_Reference.ConvertAll(x => x.ElementId()).Distinct();

            Geometry.Spatial.Plane plane = new Geometry.Spatial.Plane(new Point3D(0, 0, elevation), Vector3D.BaseZ);

            List<Segment2D> segment2Ds = new List<Segment2D>();
            foreach(ElementId elementId in elementIds_Reference)
            {
                LocationCurve locationCurve = document.GetElement(elementId).Location as LocationCurve;
                ISegmentable3D segmentable3D = locationCurve.ToSAM() as ISegmentable3D;
                if (segmentable3D == null)
                    continue;

                segment2Ds.AddRange(segmentable3D.GetSegments().ConvertAll(x => plane.Convert(plane.Project(x))));
            }

            Dictionary<Segment2D, ElementId> dictionary = new Dictionary<Segment2D, ElementId>();
            foreach (ElementId elementId in elementIds)
            {
                LocationCurve locationCurve = document.GetElement(elementId).Location as LocationCurve;
                Segment3D segment3D = locationCurve.ToSAM() as Segment3D;
                if (segment3D == null)
                    continue;

                dictionary[plane.Convert(plane.Project(segment3D))] = elementId;
            }

            Dictionary<Segment2D, ElementId> dictionary_Result = new Dictionary<Segment2D, ElementId>();
            foreach (Segment2D segment2D in segment2Ds)
            {
                List<Segment2D> segment2Ds_Temp = dictionary.Keys.ToList().FindAll(x => x.Colinear(segment2D) && x.Distance(segment2D) <= maxDistance);
                if (segment2Ds_Temp == null || segment2Ds_Temp.Count == 0)
                    continue;

                foreach(Segment2D segment2D_Temp in segment2Ds_Temp)
                {
                    Segment2D segment2D_Project = segment2D.Project(segment2D_Temp);
                    if (segment2D_Project == null)
                        continue;

                    dictionary_Result[segment2D_Project] = dictionary[segment2D_Temp];
                    dictionary.Remove(segment2D_Temp);
                }
            }


            List<HostObject> result = new List<HostObject>();
            foreach (KeyValuePair<Segment2D, ElementId> keyValuePair in dictionary_Result)
            {
                Wall wall = document.GetElement(keyValuePair.Value) as Wall;
                if (wall == null || !wall.IsValidObject)
                    continue;

                Segment2D segment2D = keyValuePair.Key;

                LocationCurve locationCurve = wall.Location as LocationCurve;

                double z = locationCurve.Curve.GetEndPoint(0).Z;

                JoinType[] joinTypes = new JoinType[] { locationCurve.get_JoinType(0), locationCurve.get_JoinType(1) };
                WallUtils.DisallowWallJoinAtEnd(wall, 0);
                WallUtils.DisallowWallJoinAtEnd(wall, 1);

                Segment3D segment3D = new Segment3D(new Point3D(segment2D[0].X, segment2D[0].Y, z), new Point3D(segment2D[1].X, segment2D[1].Y, z));

                Line line = Geometry.Revit.Convert.ToRevit(segment3D);

                locationCurve.Curve = line;

                WallUtils.AllowWallJoinAtEnd(wall, 0);
                locationCurve.set_JoinType(0, joinTypes[0]);

                WallUtils.AllowWallJoinAtEnd(wall, 1);
                locationCurve.set_JoinType(1, joinTypes[1]);

                result.Add(wall);
            }

            dataAccess.SetDataList(0, result);
        }
    }
}