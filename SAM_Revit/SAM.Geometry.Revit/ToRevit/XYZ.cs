﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAM.Geometry.Spatial;

using Autodesk.Revit.DB;


namespace SAM.Geometry.Revit
{
    public static partial class Convert
    {
        public static XYZ ToRevit(this Point3D point3D)
        {
            double scale = Units.Query.ToImperial(1, Units.UnitType.Meter);

            return new XYZ(point3D.X * scale, point3D.Y * scale, point3D.Z * scale);
        }

        public static XYZ ToRevit(this Vector3D vector3D)
        {
            double scale = Units.Query.ToImperial(1, Units.UnitType.Meter);

            return new XYZ(vector3D.X * scale, vector3D.Y * scale, vector3D.Z * scale);
        }
    }
}
