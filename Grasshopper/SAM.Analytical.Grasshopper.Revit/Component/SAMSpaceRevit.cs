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
    public class SAMSpaceRevit : RhinoInside.Revit.GH.Components.TransactionComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("54ddeae2-2245-424e-9aa3-c1a5c30a2052");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Revit;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMSpaceRevit()
          : base("SAMSpace.Revit", "SAMSpace.Revit",
              "Convert SAM Space to Revit Space",
              "SAM", "Revit")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            inputParamManager.AddParameter(new GooSpaceParam(), "_space", "_space", "SAM Analytical Space", GH_ParamAccess.item);

            int index = inputParamManager.AddGenericParameter("_convertSettings_", "_convertSettings_", "SAM Convert Settings", GH_ParamAccess.item);
            inputParamManager[index].Optional = true;

            inputParamManager.AddBooleanParameter("_run_", "_run_", "Run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new RhinoInside.Revit.GH.Parameters.SpatialElement(), "Space", "Space", "Revit Space", GH_ParamAccess.item);
        }

        protected override void TrySolveInstance(IGH_DataAccess dataAccess)
        {
            bool run = false;
            if (!dataAccess.GetData(2, ref run) || !run)
                return;

            ConvertSettings convertSettings = null;
            dataAccess.GetData(1, ref convertSettings);

            if (convertSettings == null)
                convertSettings = Core.Revit.Query.ConvertSettings();

            Space space = null;
            if (!dataAccess.GetData(0, ref space))
                return;

            Document document = RhinoInside.Revit.Revit.ActiveDBDocument;

            if (convertSettings.RemoveExisting)
            {
                ElementId elementId = space.ElementId();
                if (elementId != null && elementId != ElementId.InvalidElementId)
                {
                    Element element = document.GetElement(elementId) as SpatialElement;
                    if (element != null)
                        document.Delete(elementId);
                }
            }

            Autodesk.Revit.DB.Mechanical.Space space_Revit = Analytical.Revit.Convert.ToRevit(document, space, new ConvertSettings(true, true, true));

            dataAccess.SetData(0, space_Revit);
        }
    }
}