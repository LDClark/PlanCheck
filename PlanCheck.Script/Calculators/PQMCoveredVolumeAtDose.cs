using System;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck.Calculators
{
    class PQMCoveredVolumeAtDose
    {
        public static string GetCoveredVolumeAtDose(StructureSet structureSet, PlanningItemViewModel planningItem, StructureViewModel evalStructure, MatchCollection testMatch, Group evalunit)
        {
            try
            {
                var structure = structureSet.Structures.FirstOrDefault(x => x.Id == evalStructure.Id);
                //check for sufficient sampling and dose coverage
                DVHData dvh = planningItem.Object.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
                if ((dvh.SamplingCoverage < 0.9) || (dvh.Coverage < 0.9))
                {
                    return "Unable to calculate - insufficient dose or sampling coverage";
                }
                Group eval = testMatch[0].Groups["evalpt"];
                Group unit = testMatch[0].Groups["unit"];
                DoseValue.DoseUnit du = (unit.Value.CompareTo("%") == 0) ? DoseValue.DoseUnit.Percent :
                        (unit.Value.CompareTo("cGy") == 0) ? DoseValue.DoseUnit.cGy : DoseValue.DoseUnit.Unknown;
                VolumePresentation vp = (unit.Value.CompareTo("%") == 0) ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                DoseValue dv = new DoseValue(double.Parse(eval.Value), du);
                double volume = double.Parse(eval.Value);
                VolumePresentation vpFinal = (evalunit.Value.CompareTo("%") == 0) ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                DoseValuePresentation dvpFinal = (evalunit.Value.CompareTo("%") == 0) ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
                double volumeAchieved = planningItem.Object.GetVolumeAtDose(structure, dv, vpFinal);
                double organVolume = Convert.ToDouble(evalStructure.VolumeValue);
                double coveredVolume = organVolume - volumeAchieved;
                return string.Format("{0:0.00} {1}", coveredVolume, evalunit.Value);   // todo: better formatting based on VolumePresentation
            }
            catch (NullReferenceException)
            {
                return "Unable to calculate - DVH is not valid";
            }
        }
    }
}
