using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck
{
    public class StructureViewModel
    {
        public string StructureName { get; set; }
        public string StructureCode { get; set; }
        public string StructureNameWithCode { get; set; }
        public string VolumeValue { get; set; }
        public string VolumeUnit {get;set;}

        public StructureViewModel(Structure structure)
        {
            if (structure != null)
            {
                StructureName = structure.Id;
                StructureCode = structure.StructureCodeInfos.FirstOrDefault().Code;
                StructureNameWithCode = StructureName + " : " + StructureCode;
                VolumeValue = structure.Volume.ToString("0.0");
                VolumeUnit = VolumePresentation.AbsoluteCm3.ToString();
            }
        }
    }
}
