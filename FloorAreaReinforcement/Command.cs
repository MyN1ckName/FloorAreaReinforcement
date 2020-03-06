using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Attributes;

namespace FloorAreaReinforcement
{
	[TransactionAttribute(TransactionMode.Manual)]
	[RegenerationAttribute(RegenerationOption.Manual)]
	public class Command : IExternalCommand
	{
		UIApplication uiapp;

		public Result Execute(ExternalCommandData commandData,
			ref string messege,
			ElementSet elements)
		{
			uiapp = commandData.Application;

			try
			{
				//Models.Class1.Create(uiapp.ActiveUIDocument);
				Floor floor = GetHost(uiapp.ActiveUIDocument);
				Windows.MainWundow.MainWindow window = new Windows.MainWundow.MainWindow()
				{
					DataContext = new Windows.MainWundow.ViewModel(floor)
				};
				window.ShowDialog();
				return Result.Succeeded;
			}

			catch (Autodesk.Revit.Exceptions.OperationCanceledException)
			{
				return Result.Cancelled;
			}
			catch (Exception ex)
			{
				messege = ex.Message;
				return Result.Failed;
			}
		}

		static Floor GetHost(UIDocument uidoc)
		{
			Selection sel = uidoc.Application.ActiveUIDocument.Selection;
			Models.FloorPickFilter floorFilter = new Models.FloorPickFilter();
			Reference pickRef = sel.PickObject(ObjectType.Element, floorFilter
				, "Select Floor");
			Element e = uidoc.Document.GetElement(pickRef.ElementId);
			return e as Floor;
		}
	}
}