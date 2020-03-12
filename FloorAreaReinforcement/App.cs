using System;
using Autodesk.Revit.UI;

namespace FloorAreaReinforcement
{
	class App : IExternalApplication
	{
		static readonly string ExecutingAssemblyPath = System.Reflection.Assembly
			.GetExecutingAssembly().Location;

		public Result OnStartup(UIControlledApplication app)
		{
			RibbonPanel rvtRibbonPanel = app.CreateRibbonPanel("Reinforcement");
			PushButtonData dataButton = new PushButtonData("Button", "FloorArea"
				, ExecutingAssemblyPath, "FloorAreaReinforcement.Command");

			PushButton button = rvtRibbonPanel.AddItem(dataButton) as PushButton;

			button.LargeImage = new System.Windows.Media.Imaging.BitmapImage
				(new Uri("pack://application:,,,/FloorAreaReinforcement;component/img/icon.png"
				, UriKind.Absolute));

			button.ToolTip =
				"Основное армирование для плиты перекрытия";

			return Result.Succeeded;
		}

		public Result OnShutdown(UIControlledApplication app)
		{
			return Result.Succeeded;
		}
	}
}