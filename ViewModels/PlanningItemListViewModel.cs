using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    class PlanningItemListViewModel : ViewModelBase
    {
        static public ObservableCollection<PlanningItemViewModel> GetPlanningItemList(IEnumerable<PlanSetup> planSetupsInScope, IEnumerable<PlanSum> planSumsInScope)
        {
            var PlanningItemComboBoxList = new ObservableCollection<PlanningItemViewModel>();
            foreach (PlanSetup planSetup in planSetupsInScope)
            {
                var planningItemViewModel = new PlanningItemViewModel(planSetup);
                PlanningItemComboBoxList.Add(planningItemViewModel);
            }

            foreach (PlanSum planSum in planSumsInScope)
            {
                var planningItemViewModel = new PlanningItemViewModel(planSum);
                PlanningItemComboBoxList.Add(planningItemViewModel);
            }
            return PlanningItemComboBoxList;
        }
    }
}
