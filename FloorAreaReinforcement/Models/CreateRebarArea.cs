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
	public static class CreateRebarArea
	{
		const double mmToft = 0.00328084;

		// TODO: назначение стадии по хосту?

		public static AreaReinforcement Create(Models.RebarArea rebarArea)
		{
			Document doc = rebarArea.Document;
			Element hostElement = rebarArea.Host;
			IList<Curve> curveArray = GetCurveArray(hostElement);
			XYZ majorDirection = GetMajorDirection(curveArray);
			ElementId areaReinforcementTypeId = rebarArea.AreaReinforcementType.Id;

			ElementId rebarBarTypeId;
			if (rebarArea.RebarBarType != null)
			{
				rebarBarTypeId = rebarArea.RebarBarType.Id;
			}
			else
			{
				throw new Exception(
					string.Format("{0} - не выбран типоразмер арматуры",
					rebarArea.AreaReinforcementType.Name));
			}

			ElementId rebarHookTypeId = ElementId.InvalidElementId;

			using (Transaction t = new Transaction(doc, "CreateAreaReinforcement"))
			{
				t.Start();
				AreaReinforcement areaReinforcement = AreaReinforcement.Create(
					doc, hostElement, curveArray, majorDirection,
					areaReinforcementTypeId, rebarBarTypeId, rebarHookTypeId);

				SetDirectionAndSpacing(areaReinforcement, rebarArea);

				t.Commit();

				return areaReinforcement;
			}
		}

		// TODO: Поправить GetCurveArray в зависемости от direction
		// TODO: продольный защитный слой
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

		// TODO: Поправить GetMajorDirection в зависемости от direction
		static XYZ GetMajorDirection(IList<Curve> curves)
		{
			Line firstLine = (Line)(curves[0]);

			XYZ majorDirection = new XYZ(
				firstLine.GetEndPoint(1).X - firstLine.GetEndPoint(0).X,
				firstLine.GetEndPoint(1).Y - firstLine.GetEndPoint(0).Y,
				firstLine.GetEndPoint(1).Z - firstLine.GetEndPoint(0).Z);

			return majorDirection;
		}

		private static AreaReinforcement SetDirectionAndSpacing(
			AreaReinforcement areaReinforcement, RebarArea rebarArea)
		{
			Direction direction = rebarArea.Direction;
			double rebarSpacing = rebarArea.Spacing * mmToft;

			Parameter direction_top_major_X =
				areaReinforcement.get_Parameter
				(BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_1);

			Parameter direction_top_minor_Y =
				areaReinforcement.get_Parameter
				(BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_2);

			Parameter direction_bottom_major_X =
				areaReinforcement.get_Parameter(
					BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_1);

			Parameter direction_bottom_minor_Y =
				areaReinforcement.get_Parameter
				(BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_2);

			switch (direction)
			{
				case Direction.TopMajor:
					direction_top_major_X.Set(1);
					direction_top_minor_Y.Set(0);
					direction_bottom_major_X.Set(0);
					direction_bottom_minor_Y.Set(0);

					Parameter spacing_top_major_X =
						areaReinforcement.get_Parameter
						(BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_1);
					spacing_top_major_X.Set(rebarSpacing);
					break;

				case Direction.TopMinor:
					direction_top_major_X.Set(0);
					direction_top_minor_Y.Set(1);
					direction_bottom_major_X.Set(0);
					direction_bottom_minor_Y.Set(0);
					Parameter spacing_top_minor_Y =
						areaReinforcement.get_Parameter
						(BuiltInParameter.REBAR_SYSTEM_SPACING_TOP_DIR_2);
					spacing_top_minor_Y.Set(rebarSpacing);
					break;

				case Direction.BottomMajor:
					direction_top_major_X.Set(0);
					direction_top_minor_Y.Set(0);
					direction_bottom_major_X.Set(1);
					direction_bottom_minor_Y.Set(0);

					Parameter spacing_bottom_major_X =
						areaReinforcement.get_Parameter
						(BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_1);
					spacing_bottom_major_X.Set(rebarSpacing);
					break;

				case Direction.BottomMinor:
					direction_top_major_X.Set(0);
					direction_top_minor_Y.Set(0);
					direction_bottom_major_X.Set(0);
					direction_bottom_minor_Y.Set(1);

					Parameter spacing_bottom_minor_Y =
						areaReinforcement.get_Parameter
						(BuiltInParameter.REBAR_SYSTEM_SPACING_BOTTOM_DIR_2);
					spacing_bottom_minor_Y.Set(rebarSpacing);
					break;
			}
			return areaReinforcement;
		}
	}
}