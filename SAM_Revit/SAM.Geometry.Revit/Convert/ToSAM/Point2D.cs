﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAM.Geometry.Planar;

using Autodesk.Revit.DB;


namespace SAM.Geometry.Revit
{
    public static partial class Convert
    {
        public static Point2D ToSAM(this UV uV)
        {
            return new Point2D(uV.U * Units.Revit.Query.Factor_FromFeetToMeter, uV.V * Units.Revit.Query.Factor_FromFeetToMeter);
        }
    }
}
