using System.Collections.ObjectModel;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    class StructureSetListViewModel : ViewModelBase
    {
        static public ObservableCollection<StructureViewModel> GetStructureList(StructureSet structureSet)
        {
            var StructureList = new ObservableCollection<StructureViewModel>();
            foreach (Structure structure in structureSet.Structures)
            {
                if (!structure.IsEmpty && structure.DicomType != "SUPPORT")
                {
                    var structureViewModel = new StructureViewModel(structure);
                    StructureList.Add(structureViewModel);
                }
            }
            var StructureComboBoxList = new ObservableCollection<StructureViewModel>(StructureList.OrderBy(x => x.StructureName));
            return StructureComboBoxList;
        }
    }
}
