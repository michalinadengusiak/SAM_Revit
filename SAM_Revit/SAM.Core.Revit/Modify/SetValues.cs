﻿using Autodesk.Revit.DB;
using SAM.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Revit
{
    public static partial class Modify
    {
        /// <summary>
        /// Sets Revit element parameters using values in given ParameterSet
        /// </summary>
        /// <param name="element">Revit Element</param>
        /// <param name="parameterSet">SAM ParameterSet values will be teaken</param>
        /// <param name="parameterNames_Excluded"> Parameter Names to be skipped</param>
        /// <param name="parameterNames_Included"> Parameter Names to be included (rest to be skipped)</param>
        /// <returns></returns>
        public static bool SetValues(this Element element, ParameterSet parameterSet, IEnumerable<string> parameterNames_Included = null, IEnumerable<string> parameterNames_Excluded = null)
        {
            if (parameterSet == null || element == null)
                return false;

            foreach (string name in parameterSet.Names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (parameterNames_Excluded != null && parameterNames_Excluded.Contains(name))
                    continue;

                if (parameterNames_Included != null && !parameterNames_Included.Contains(name))
                    continue;

                IEnumerable<Parameter> parameters = element.GetParameters(name);
                if (parameters == null || parameters.Count() == 0)
                    continue;

                foreach (Parameter parameter in parameters)
                {
                    if (!parameter.IsReadOnly)
                        SetValue(parameter, parameterSet.ToObject(name));
                }
                    
            }

            return true;
        }

        public static bool SetValues(this Element element, ParameterSet parameterSet, IEnumerable<BuiltInParameter> builtInParameters_Excluded)
        {
            if (parameterSet == null || element == null)
                return false;

            List<int> Ids = null;
            if (builtInParameters_Excluded != null)
                Ids = builtInParameters_Excluded.ToList().ConvertAll(x => (int)x);

            foreach (string name in parameterSet.Names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                IEnumerable<Parameter> parameters = element.GetParameters(name);
                if (parameters == null || parameters.Count() == 0)
                    continue;

                foreach (Parameter parameter in parameters)
                {
                    if (Ids != null && Ids.Contains(parameter.Id.IntegerValue))
                        continue;

                    SetValue(parameter, parameterSet.ToObject(name));
                }
            }

            return true;
        }

        public static bool SetValues(this Element element, SAMObject sAMObject, Setting setting)
        {
            if (element == null)
                return false;

            TypeMap typeMap;
            if (!setting.TryGetValue(ActiveSetting.Name.ParameterMap, out typeMap) || typeMap == null)
                return false;

            return SetValues(element, sAMObject, typeMap);
        }

        public static bool SetValues(this Element element, SAMObject sAMObject, TypeMap typeMap)
        {
            if (element == null || typeMap == null)
                return false;

            Type type_SAMObject = sAMObject.GetType();
            Type type_Revit = element.GetType();

            Dictionary<string, object> dictionary = null;

            List<string> names_SAM = typeMap.GetNames(type_SAMObject, type_Revit);
            if (names_SAM != null || names_SAM.Count > 0)
            {
                foreach (string name_SAM in names_SAM.Distinct())
                {
                    List<string> names_Revit = typeMap.GetNames(type_SAMObject, type_Revit, name_SAM);
                    if (names_Revit != null || names_Revit.Count > 0)
                    {
                        for (int i = 0; i < names_Revit.Count; i++)
                        {
                            string name_Revit = names_Revit[i];

                            if (string.IsNullOrWhiteSpace(name_Revit))
                                continue;

                            if (name_Revit.StartsWith("="))
                            {
                                if (dictionary == null)
                                {
                                    dictionary = new Dictionary<string, object>();
                                    dictionary["Object_1"] = sAMObject;
                                    dictionary["Object_2"] = element;
                                    dictionary["Name_1"] = name_SAM;
                                    dictionary["Name_2"] = name_Revit;
                                }

                                if (Core.Query.TryCompute(new Expression(name_Revit.Substring(0, name_Revit.Length - 1)), out string name_Revit_Temp, dictionary))
                                    name_Revit = name_Revit_Temp;
                            }

                            SetValue(element, name_Revit, sAMObject, name_SAM);
                        }
                    }
                }
            }

            return true;
        }

        public static bool SetValues(this Element element, SAMObject sAMObject, IEnumerable<string> parameterNames_Included = null, IEnumerable<string> parameterNames_Excluded = null)
        {
            if (sAMObject == null || element == null)
                return false;

            List<ParameterSet> parameterSets = sAMObject.GetParamaterSets();
            if(parameterSets != null && parameterSets.Count != 0)
            {
                foreach (ParameterSet parameterSet in parameterSets)
                    element.SetValues(parameterSet, parameterNames_Included, parameterNames_Excluded);
            }

            Setting setting = ActiveSetting.Setting;

            if (!element.SetValues(sAMObject, setting))
                return false;

            return true;
        }

        public static bool SetValues(this Element element, SAMObject sAMObject, IEnumerable<BuiltInParameter> builtInParameters_Excluded)
        {
            if (sAMObject == null || element == null)
                return false;

            ParameterSet parameterSet = sAMObject.GetParameterSet(element.GetType()?.Assembly);
            if (parameterSet != null)
            {
                if (!element.SetValues(parameterSet, builtInParameters_Excluded))
                    return false;
            }

            Setting setting = ActiveSetting.Setting;

            if (!element.SetValues(sAMObject, setting))
                return false;

            return true;
        }
    }
}