﻿using Autodesk.Revit.DB;
using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Revit.Properties;
using SAM.Analytical.Revit;
using SAM.Core.Revit;
using SAM.Geometry.Planar;
using SAM.Geometry.Revit;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Revit
{
    public class SAMAdjacencyClusterRevit : RhinoInside.Revit.GH.Components.TransactionComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("54287c2b-9aaf-478d-ac32-4d258aec2548");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Revit;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAdjacencyClusterRevit()
          : base("SAMAdjacencyCluster.Revit", "SAMAdjacencyCluster.Revit",
              "SAM AdjacencyCluster to Revit",
              "SAM", "Revit")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            inputParamManager.AddParameter(new GooAdjacencyClusterParam(), "_adjacencyCluster", "_adjacencyCluster", "SAM Analytical AdjacencyCluster", GH_ParamAccess.item);
            inputParamManager.AddBooleanParameter("_run_", "_run_", "Run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new RhinoInside.Revit.GH.Parameters.Element(), "Elements", "Elements", "Revit Elements", GH_ParamAccess.list);
        }

        protected override void TrySolveInstance(IGH_DataAccess dataAccess)
        {
            bool run = false;
            if (!dataAccess.GetData(1, ref run) || !run)
                return;

            ConvertSettings convertSettings = null;
            dataAccess.GetData(1, ref convertSettings);

            if (convertSettings == null)
                convertSettings = Core.Revit.Query.ConvertSettings();

            AdjacencyCluster adjacencyCluster = null;
            if (!dataAccess.GetData(0, ref adjacencyCluster))
                return;

            Document document = RhinoInside.Revit.Revit.ActiveDBDocument;

            List<Element> elements = Analytical.Revit.Convert.ToRevit(adjacencyCluster, document, new ConvertSettings(true, true, true));

            dataAccess.SetDataList(0, elements);
        }
    }
}