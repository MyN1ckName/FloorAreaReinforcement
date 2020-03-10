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
		const double epsilon = 0.001;

		// TODO: назначение стадии по хосту?

		// Создание арматурной сетки по параметрам обьекта RebarArea rebarArea
		public static AreaReinforcement Create(Models.RebarArea rebarArea,
			XYZ majorDirection)
		{
			Document doc = rebarArea.Document;
			Element hostElement = rebarArea.Host;
			IList<Curve> curveArray = GetCurveArray(hostElement);
			//XYZ majorDirection = majorDirection;
			//XYZ majorDirection = new XYZ(0, 1, 0);
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

		// Получение контура армирования из аналитической модели плиты
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
		// Получение главного направления армирования
		public static XYZ GetMajorDirection(Element e)
		{
			AnalyticalModel analyticalModel = e.GetAnalyticalModel() as AnalyticalModel;
			if (null == analyticalModel)
			{
				throw new Exception("Can't get AnalyticalModel from the selected Floor");
			}

			IList<Curve> curves = analyticalModel.GetCurves(AnalyticalCurveType
				.ActiveCurves);

			XYZ direction = new XYZ(0, 1, 0);

			List<Line> lines = new List<Line>();

			foreach (Line line in curves)
			{
				if ((line.Direction.X > 0 || line.Direction.X.EqualTo(0, epsilon))
					&&
					(line.Direction.Y > 0 || line.Direction.Y.EqualTo(0, epsilon)))
				{
					lines.Add(line);
				}
			}

			foreach (Line line in lines)
			{
				if (line.Direction.X.EqualTo(direction.X, epsilon) &&
					line.Direction.Y.EqualTo(direction.Y, epsilon))
				{
					return direction;
				}
			}
			return lines.First().Direction;
		}

		// Назначение параметров для RebarBarType
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