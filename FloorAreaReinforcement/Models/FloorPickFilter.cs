using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace FloorAreaReinforcement.Models
{
	class FloorPickFilter : ISelectionFilter
	{
		public bool AllowElement(Element e)
		{
			return (e.Category.Id.IntegerValue.
				Equals((int)BuiltInCategory.OST_Floors));
		}
		public bool AllowReference(Reference r, XYZ p)
		{
			return false;
		}
	}
}