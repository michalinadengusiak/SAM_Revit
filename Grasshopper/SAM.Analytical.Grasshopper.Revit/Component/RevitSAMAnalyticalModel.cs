﻿using Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Revit.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Revit
{
    public class RevitSAMAnalyticalModel : RhinoInside.Revit.GH.Components.TransactionalComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("fba18b3c-3778-44aa-8ea5-4cc4a783cc07");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Revit;

        protected override ParamDefinition[] Inputs
        {
            get
            {
                ParamDefinition[] paramDefinitions = new ParamDefinition[1];

                Param_Boolean param_Boolean = new Param_Boolean();
                param_Boolean.Access = GH_ParamAccess.item;
                paramDefinitions[0] = ParamDefinition.FromParam(param_Boolean, ParamVisibility.Mandatory, false);

                return paramDefinitions;
            }
        }

        protected override ParamDefinition[] Outputs
        {
            get
            {
                ParamDefinition[] paramDefinitions = new ParamDefinition[1];

                GooAnalyticalModelParam gooAnalyticalModelParam = new GooAnalyticalModelParam();
                gooAnalyticalModelParam.Access = GH_ParamAccess.item;
                paramDefinitions[0] = ParamDefinition.FromParam(gooAnalyticalModelParam);

                return paramDefinitions;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public RevitSAMAnalyticalModel()
          : base("Revit.SAMAnalyticalModel", "Revit.SAMAnalyticalModel",
              "Convert Revit To SAM Analytical Model, using Revit gbXML Analytical model generation",
              "SAM", "Revit")
        {
        }

        protected override void TrySolveInstance(IGH_DataAccess dataAccess)
        {
            bool run = false;
            if (!dataAccess.GetData(0, ref run) || !run)
                return;

            Document document = RhinoInside.Revit.Revit.ActiveDBDocument;

            AnalyticalModel analyticalModel = null;

            using (Transaction transaction = new Transaction(document, "GetAnalyticalModel"))
            {
                FailureHandlingOptions failureHandlingOptions = transaction.GetFailureHandlingOptions();
                failureHandlingOptions.SetFailuresPreprocessor(new Core.Revit.WarningSwallower());
                transaction.SetFailureHandlingOptions(failureHandlingOptions);

                transaction.Start();

                analyticalModel = Analytical.Revit.Convert.ToSAM_AnalyticalModel(document);

                transaction.RollBack();
            }

            dataAccess.SetData(0, new GooAnalyticalModel(analyticalModel));
        }
    }
}