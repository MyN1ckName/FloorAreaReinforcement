using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;

namespace FloorAreaReinforcement.Models
{
	public static class Class1
	{
		#region Help Info
		// Для создания армирования использовать эту перегрузку
		// https://www.revitapidocs.com/2019/69267708-f0ad-3fd5-2018-fa624e763fa5.htm
		static AreaReinforcement Create(
		Document document,
		Element hostElement,
		IList<Curve> curveArray,
		XYZ majorDirection,
		ElementId areaReinforcementTypeId,
		ElementId rebarBarTypeId,
		ElementId rebarHookTypeId)
		{ return null; }
		#endregion

		public static AreaReinforcement Create(UIDocument uidoc)
		{
			Document doc = uidoc.Document;
			Element hostElement = GetHost(uidoc);
			IList<Curve> curveArray = GetCurveArray(hostElement);
			XYZ majorDirection = GetMajorDirection(curveArray);
			ElementId areaReinforcementTypeId = doc
				.GetDefaultElementTypeId(ElementTypeGroup.AreaReinforcementType);
			ElementId rebarBarTypeId = doc
				.GetDefaultElementTypeId(ElementTypeGroup.RebarBarType);
			ElementId rebarHookTypeId = ElementId.InvalidElementId;

			using (Transaction t = new Transaction(doc, "CreateAreaReinforcement"))
			{
				t.Start();
				AreaReinforcement areaReinforcement = AreaReinforcement.Create(
					doc, hostElement, curveArray, majorDirection,
					areaReinforcementTypeId, rebarBarTypeId, rebarHookTypeId);
				t.Commit();

				return areaReinforcement;
			}
		}

		static Floor GetHost(UIDocument uidoc)
		{
			Selection sel = uidoc.Application.ActiveUIDocument.Selection;
			FloorPickFilter floorFilter = new FloorPickFilter();
			Reference pickRef = sel.PickObject(ObjectType.Element, floorFilter
				, "Select Floor");
			Element e = uidoc.Document.GetElement(pickRef.ElementId);
			return e as Floor;
		}

		static IList<Curve> GetCurveArray(Element e)
		{
			AnalyticalModel analyticalModel = e.GetAnalyticalModel() as AnalyticalModel;
			if (null == analyticalModel)
			{
				throw new Exception("Can't get AnalyticalModel from the selected Floor");
			}

			IList<Curve> curves = analyticalModel.GetCurves(AnalyticalCurveType
				.ActiveCurves);
			return curves;
		}

		static XYZ GetMajorDirection(IList<Curve> curves)
		{
			Line firstLine = (Line)(curves[0]);

			XYZ majorDirection = new XYZ(
				firstLine.GetEndPoint(1).X - firstLine.GetEndPoint(0).X,
				firstLine.GetEndPoint(1).Y - firstLine.GetEndPoint(0).Y,
				firstLine.GetEndPoint(1).Z - firstLine.GetEndPoint(0).Z);

			return majorDirection;

		}
	}
}