﻿using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;


namespace SAM.Core.Revit
{
    public static partial class Convert
    {
        public static double ToSI(this double value, ParameterType parameterType)
        {
            if (double.IsNaN(value))
                return value;

            if (parameterType == ParameterType.Invalid)
                return value;
            
            switch(parameterType)
            {
                case ParameterType.Length:
                    return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_METERS);
                case ParameterType.Weight:
                    return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_KILOGRAMS_MASS);
                case ParameterType.HVACTemperature:
                    return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_KELVIN);
                case ParameterType.PipingTemperature:
                    return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_KELVIN);
                case ParameterType.ElectricalCurrent:
                    return UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_AMPERES);
            }

            return value;
        }
    }
}
