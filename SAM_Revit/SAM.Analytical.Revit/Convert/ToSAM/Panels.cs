﻿using Autodesk.Revit.DB;
using SAM.Geometry.Revit;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Revit
{
    public static partial class Convert
    {
        public static List<Panel> ToSAM(this HostObject hostObject)
        {
            if (hostObject == null)
                return null;

            ElementId elementId_Type = hostObject.GetTypeId();
            if (elementId_Type == null || elementId_Type == ElementId.InvalidElementId)
                return null;

            PanelType panelType = Query.PanelType(hostObject);
            Construction construction = ((HostObjAttributes)hostObject.Document.GetElement(elementId_Type)).ToSAM();
            if (construction == null)
                construction = Analytical.Query.Construction(panelType); //Default Construction

            List<Geometry.Spatial.Face3D> face3Ds = hostObject.Profiles();

            List<Panel> result = new List<Panel>();

            LogicalOrFilter logicalOrFilter = new LogicalOrFilter(new List<ElementFilter>() { new ElementCategoryFilter(BuiltInCategory.OST_Windows), new ElementCategoryFilter(BuiltInCategory.OST_Doors) });
            IEnumerable<ElementId> elementIds = hostObject.GetDependentElements(logicalOrFilter);

            if (hostObject is Wall)
            {
                List<Autodesk.Revit.DB.Panel> panels = Create.Panels((Wall)hostObject);
                if (panels != null && panels.Count > 0)
                {
                    List<ElementId> elementIds_Temp = panels.ConvertAll(x => x.Id);
                    if (elementIds != null && elementIds.Count() > 0)
                        elementIds_Temp.AddRange(elementIds);

                    elementIds = elementIds_Temp;
                }
            }

            foreach (Geometry.Spatial.Face3D face3D in face3Ds)
            {
                if (face3D == null)
                    continue;

                Panel panel = new Panel(construction, panelType, face3D);
                panel.Add(Core.Revit.Query.ParameterSet(hostObject));

                if (elementIds != null && elementIds.Count() > 0)
                {
                    Geometry.Spatial.Plane plane = panel.PlanarBoundary3D.Plane;

                    foreach (ElementId elementId in elementIds)
                    {
                        Element element = hostObject.Document.GetElement(elementId);
                        if (element == null)
                            continue;

                        if (!(element is FamilyInstance))
                            continue;

                        Aperture aperture = ToSAM_Aperture((FamilyInstance)element, plane, panelType);
                        panel.AddAperture(aperture);
                    }
                }

                result.Add(panel);
            }

            return result;
        }

        public static List<Panel> ToSAM_Panels(this RevitLinkInstance revitLinkInstance)
        {
            Document document_Source = revitLinkInstance.GetLinkDocument();

            //LogicalOrFilter logicalOrFilter = new LogicalOrFilter((new List<System.Type> { typeof(Wall), typeof(Floor), typeof(RoofBase) }).ConvertAll(x => (ElementFilter)(new ElementClassFilter(x))));

            LogicalOrFilter logicalOrFilter = new LogicalOrFilter((new List<BuiltInCategory> { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs }).ConvertAll(x => (ElementFilter)(new ElementCategoryFilter(x))));

            IEnumerable<HostObject> hostObjects = new FilteredElementCollector(document_Source).WherePasses(logicalOrFilter).OfClass(typeof(HostObject)).WhereElementIsNotElementType().Cast<HostObject>();
            if (hostObjects == null || hostObjects.Count() == 0)
                return null;

            List<Panel> result = new List<Panel>();
            foreach (HostObject hostObject in hostObjects)
            {
                List<Panel> panelList = hostObject.ToSAM();
                if (panelList != null && panelList.Count > 0)
                    result.AddRange(panelList);
            }

            return result;
        }
    }
}