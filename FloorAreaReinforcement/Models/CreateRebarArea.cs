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
		const double epsilon = 0.00001;

		// Создание арматурной сетки по параметрам обьекта RebarArea rebarArea
		public static AreaReinforcement Create(Models.RebarArea rebarArea,
			XYZ majorDirection)
		{
			Document doc = rebarArea.Document;
			Element hostElement = rebarArea.Host;
			IList<Curve> curveArray = GetCurveArray(rebarArea, majorDirection);
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

		static IList<Curve> GetCurveArray(RebarArea rebarArea, XYZ majorDirection)
		{
			Floor floor = rebarArea.Host;

			AnalyticalModel analyticalModel = floor.GetAnalyticalModel() as AnalyticalModel;
			if (null == analyticalModel)
			{
				throw new Exception("Не удалось получить аналитическую модель");
			}

			IList<Curve> curves = analyticalModel.GetCurves(AnalyticalCurveType
				.ActiveCurves);

			List<Line> lines = new List<Line>();
			foreach (Line line in curves)
			{
				if (rebarArea.Direction == Direction.TopMajor ||
					rebarArea.Direction == Direction.BottomMajor)
				{
					if ((line.Direction.X.EqualTo(majorDirection.X, epsilon)
						&&
						line.Direction.Y.EqualTo(majorDirection.Y, epsilon))
						||
						(line.Direction.Negate().X.EqualTo(majorDirection.X, epsilon)
						&&
						line.Direction.Negate().Y.EqualTo(majorDirection.Y, epsilon)))
					{
						lines.Add(line);
					}
				}

				if (rebarArea.Direction == Direction.TopMinor ||
					rebarArea.Direction == Direction.BottomMinor)
				{
					if ((line.Direction.X.EqualTo(majorDirection.Negate().Y, epsilon)
						&&
						line.Direction.Y.EqualTo(majorDirection.X, epsilon))
						||
						(line.Direction.Negate().X.EqualTo(majorDirection.Negate().Y, epsilon)
						&&
						line.Direction.Negate().Y.EqualTo(majorDirection.X, epsilon)))
					{
						lines.Add(line);
					}
				}
			}

			// TODO: Перед получением точек линии нужно сместить на продольный защитный слой

			List<XYZ> points = new List<XYZ>();
			double offset = (rebarArea.AlongRebarCover * mmToft) - (rebarArea.RebarBarType.BarDiameter / 2);
			foreach (Line line in lines)
			{
				points.Add(AddAlongRebarCover(line.GetEndPoint(0), line.Direction, offset));
				points.Add(AddAlongRebarCover(line.GetEndPoint(1), line.Direction, offset));
			}

			IList<Curve> newLines = new List<Curve>();
			XYZ q = points[points.Count - 1];
			foreach (XYZ p in points)
			{
				newLines.Add(Line.CreateBound(q, p));
				q = p;
			}
			return newLines;
		}

		private static XYZ AddAlongRebarCover(XYZ point, XYZ lineDirection, double offset)
		{
			XYZ p = new XYZ(
				point.X - (lineDirection.Y * offset),
				point.Y + (lineDirection.X * offset),
				point.Z);
			return p;
		}

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

			XYZ direction = XYZ.BasisY;

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