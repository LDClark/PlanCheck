using System;
using System.Collections.ObjectModel;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class StructureSetViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }    
        public string ImageId { get; set; }
        public DateTime ImageCreationDateTime { get; set; }
        public ObservableCollection<StructureViewModel> Structures { get; set; }
        public StructureSet Object { get; set; }

        public StructureSetViewModel(StructureSet structureSet)
        {
            Id = structureSet.Id;   
            Name = structureSet.Name;
            ImageId = structureSet.Image.Id;
            ImageCreationDateTime = structureSet.Image.CreationDateTime.Value;
            Object = structureSet;
            var StructureList = new ObservableCollection<StructureViewModel>();
            foreach (Structure structure in structureSet.Structures)
            {
                if (!structure.IsEmpty)
                {
                    var structureViewModel = new StructureViewModel(structure);
                    StructureList.Add(structureViewModel);
                }
            }
            Structures = new ObservableCollection<StructureViewModel>(StructureList.OrderBy(x => x.Id));
        }
    }
}
