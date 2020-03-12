using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
	public class RebarArea
	{
		Floor floor;
		Direction direction;
		public RebarArea(Floor floor, string areaReinforcementTypeName)
		{
			this.floor = floor;
			Document doc = floor.Document;
			areaReinforcementType = SetAreaReinforcementType(doc,
				areaReinforcementTypeName);
			availableRebarBarType = GetAvailableRebarBarType(doc);
			direction = SetDirection(areaReinforcementTypeName);
		}

		public Document Document
		{
			get { return floor.Document; }
		}
		public Floor Host
		{
			get { return floor; }
		}

		public bool IsChecked { get; set; }

		AreaReinforcementType areaReinforcementType;
		public AreaReinforcementType AreaReinforcementType
		{
			get { return areaReinforcementType; }
		}

		RebarBarType rebarBarType;
		public RebarBarType RebarBarType
		{
			get { return rebarBarType; }
			set { rebarBarType = value; }
		}

		double alongRebarCover = 50;
		public double AlongRebarCover
		{
			get { return alongRebarCover; }
			set { alongRebarCover = value; }
		}

		double spacing = 200;
		public double Spacing
		{
			get { return spacing; }
			set { spacing = value; }
		}

		public Direction Direction
		{
			get { return direction; }
		}

		private Direction SetDirection(string areaReinforcementTypeName)
		{
			Models.Direction direction;
			switch (areaReinforcementTypeName)
			{
				case "Верхняя X":
					direction = Direction.TopMajor;
					break;
				case "Верхняя Y":
					direction = Direction.TopMinor;
					break;
				case "Нижняя X":
					direction = Direction.BottomMajor;
					break;
				case "Нижняя Y":
					direction = Direction.BottomMinor;
					break;
				default:
					direction = Direction.Default;
					break;
			}
			return direction;
		}

		// Назначение типаразмера армирования
		private AreaReinforcementType SetAreaReinforcementType(Document doc, string name)
		{
			FilteredElementCollector collector = new FilteredElementCollector(doc);
			collector.OfClass(typeof(AreaReinforcementType))
				.OfCategory(BuiltInCategory.OST_AreaRein);

			AreaReinforcementType areaReinforcementType =
				(from item in collector
				 where item.Name.Equals(name)
				 select item).First() as AreaReinforcementType;

			if (areaReinforcementType == null)
				throw new ArgumentNullException("This AreaReinforcementType not find");
			else return areaReinforcementType;
		}

		private List<RebarBarType> availableRebarBarType;
		public List<RebarBarType> AvailableRebarBarType
		{
			get { return availableRebarBarType; }
		}

		// Получение доступных типаразмеров арматуры (RebarBarType)
		// в зависимости от типаразмера армирования (AreaReinforcementType)
		private List<RebarBarType> GetAvailableRebarBarType(Document doc)
		{
			List<RebarBarType> availableRebarBarType = new List<RebarBarType>();
			FilteredElementCollector collector = new FilteredElementCollector(doc);
			collector.OfClass(typeof(RebarBarType)).OfCategory(BuiltInCategory.OST_Rebar);

			foreach (RebarBarType rebarBarType in collector)
			{
				if (IsValidRebarBarType(rebarBarType))
				{
					availableRebarBarType.Add(rebarBarType);
				}
			}
			return availableRebarBarType;
		}

		// Фильт для типаразмеров арматуры по имени
		private bool IsValidRebarBarType(RebarBarType rebarBarType)
		{
			string pattern = @"\w*";
			if (Regex.IsMatch(AreaReinforcementType.Name, @"\w*нижняя\w*",
				RegexOptions.IgnoreCase))
			{
				pattern = @"^standart\w*нижняя\w*";
			}
			if (Regex.IsMatch(AreaReinforcementType.Name, @"\w*верхняя\w*",
				RegexOptions.IgnoreCase))
			{
				pattern = @"^standart\w*верхняя\w*";
			}
			return Regex.IsMatch(rebarBarType.Name, pattern, RegexOptions.IgnoreCase);
		}
	}
}