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
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public class PlanSelectViewModel : UserControl
    {
        public string ScriptVersion { get; set; }
        public User User { get; set; }
        public Patient Patient { get; set; }
        public Window MainWindow { get; set; }
        public ObservableCollection<PlanningItemViewModel> PlanningItemList { get; set; }
        public ObservableCollection<PlanSelectDetailViewModel> PlanningItemSummaries { get; set; }
        public ObservableCollection<ConstraintViewModel> ConstraintComboBoxList { get; set; }
        public ConstraintViewModel ActiveConstraintPath { get; set; }
        public PlanningItemViewModel ActivePlanningItem { get; set; }

        public PlanSelectViewModel(User user, Patient patient, string scriptVersion, PlanSetup planSetup, IEnumerable<PlanSetup> planSetupsInScope, IEnumerable<PlanSum> planSumsInScope, Window mainWindow)
        {
            User = user;
            Patient = patient;
            ScriptVersion = scriptVersion;
            MainWindow = mainWindow;
            PlanningItemList = PlanningItemListViewModel.GetPlanningItemList(planSetupsInScope, planSumsInScope);
            var psc = new PlanSelectCalculator();
            PlanningItemSummaries = psc.Calculate(PlanningItemList);
            DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
            ConstraintComboBoxList = ConstraintListViewModel.GetConstraintList(constraintDir.ToString());
        }
    }
}
