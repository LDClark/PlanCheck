using System.Windows;
using System.Windows.Controls;

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
            //var selectedItem = (PlanSelectDetailViewModel)planningItemSummariesDataGrid.SelectedItem;
            //_psvm.ActivePlanningItem = selectedItem.ActivePlanningItem;
           // var mainViewModel = new MainViewModel(
            //_psvm.MainWindow.Title = mainViewModel.Title;
            //var mainView = new MainView(mainViewModel);
            //_psvm.MainWindow.Content = mainView;
        }
    }
}
