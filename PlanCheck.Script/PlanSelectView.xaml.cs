using PlanCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using Path = System.IO.Path;

namespace PlanCheck
{
    public partial class PlanSelectView : UserControl
    {
        private PlanSelectViewModel _psvm;

        public PlanSelectView(PlanSelectViewModel planSelectViewModel)
        {
            _psvm = planSelectViewModel;
            InitializeComponent();
            DataContext = _psvm;
        }

        private void ExitButton_OnClick(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);

            if (window != null)
            {
                window.Close();
            }
        }

        private void RunButton_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = (PlanSelectDetailViewModel)planningItemSummariesDataGrid.SelectedItem;
            _psvm.ActivePlanningItem = selectedItem.ActivePlanningItem;
            var mainViewModel = new MainViewModel(_psvm.User, _psvm.Patient, _psvm.ScriptVersion, _psvm.PlanningItemList, _psvm.ActivePlanningItem);

            _psvm.MainWindow.Title = mainViewModel.Title;
            var mainView = new MainView(mainViewModel);
            _psvm.MainWindow.Content = mainView;
        }
    }
}
