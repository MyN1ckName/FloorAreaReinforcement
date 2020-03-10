using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;

namespace FloorAreaReinforcement.Windows.MainWundow
{
	class ViewModel : INotifyPropertyChanged
	{
		Document doc;
		Floor floor;
		ObservableCollection<Models.RebarArea> rebarAreaList =
			new ObservableCollection<Models.RebarArea>();
		public ViewModel(Floor floor)
		{
			this.floor = floor;
			doc = floor.Document;
			rebarAreaList = CreateRebarAreaList(floor);
		}

		private ObservableCollection<Models.RebarArea> CreateRebarAreaList(Floor floor)
		{
			ObservableCollection<Models.RebarArea> rebarAreaList =
				new ObservableCollection<Models.RebarArea>()
				{
					new Models.RebarArea(floor, "Верхняя X"),
					new Models.RebarArea(floor, "Верхняя Y"),
					new Models.RebarArea(floor, "Нижняя X"),
					new Models.RebarArea(floor, "Нижняя Y")
					//new Models.RebarArea(uidoc,"default")
				};
			return rebarAreaList;
		}

		// Коллекция типаразмеров армирования
		public ObservableCollection<Models.RebarArea> RebarAreaList
		{
			get { return rebarAreaList; }
		}

		// Выбранный элемент из RebarAreaList
		private Models.RebarArea selectedRebarArea;
		public Models.RebarArea SelectedRebarArea
		{
			get { return selectedRebarArea; }
			set
			{
				selectedRebarArea = value;
				OnPropertyChanged("SelectedRebarArea");
			}
		}

		private RelayCommand ok;
		public RelayCommand Ok
		{
			get
			{
				return ok ?? (ok = new RelayCommand(obj =>
				{
					try
					{
						Window window = obj as Window;

						using (TransactionGroup tg = new TransactionGroup(doc,
							"Create Rebar Area"))
						{
							XYZ majorDirection =
							Models.CreateRebarArea.GetMajorDirection(floor);

							tg.Start();
							foreach (Models.RebarArea rebarArea in rebarAreaList)
							{
								if (rebarArea.IsChecked)
								{
									Models.CreateRebarArea.Create(rebarArea,majorDirection);
								}
							}
							tg.Assimilate();
						}
						window.Close();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Ошибка");
					}
				}));
			}
		}

		private RelayCommand close;
		public RelayCommand Close
		{
			get
			{
				return close ?? (close = new RelayCommand(obj =>
				{
					try
					{
						Window window = obj as Window;
						window.Close();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Ошибка");
					}
				}));
			}
		}

		// INotifyPropertyChanged interface implementation
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
		}
	}
}