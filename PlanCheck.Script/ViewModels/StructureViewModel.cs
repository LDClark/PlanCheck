using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck
{
    public class StructureViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string NameWithCode { get; set; }
        public string VolumeValue { get; set; }
        public string VolumeUnit {get;set;}
        public double AssignedHU { get; set; }
        public Structure Object { get; set; }

        public StructureViewModel(Structure structure)
        {
            if (structure != null)
            {
                Id = structure.Id;
                Name = structure.Name;
                Code = structure.StructureCodeInfos.FirstOrDefault().Code;
                NameWithCode = Id + " : " + Code;
                VolumeValue = structure.Volume.ToString("0.0");
                VolumeUnit = VolumePresentation.AbsoluteCm3.ToString();
                structure.GetAssignedHU(out double huValue);
                AssignedHU = huValue;
                Object = structure;
            }
        }
    }
}
