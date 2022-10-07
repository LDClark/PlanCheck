using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck.Calculators
{
    class PQMConformationNumber
    {
        public static string GetConformationNumber(PlanningItemViewModel planningItem, StructureViewModel evalStructure, MatchCollection testMatch, Group evalunit)
        {
            try
            {
                DVHData dvh = planningItem.Object.GetDVHCumulativeData(evalStructure.Object, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
                DoseValue prescribedDose;
                double planDoseDouble = 0;
                if ((dvh.SamplingCoverage < 0.9) || (dvh.Coverage < 0.9))
                {
                    return "Unable to calculate - insufficient dose or sampling coverage";
                }
                if (planningItem.Object is PlanSum)
                {
                    PlanSum planSum = (PlanSum)planningItem.Object;
                    foreach (PlanSetup planSetup in planSum.PlanSetups)
                    {
                        planDoseDouble += planSetup.TotalDose.Dose;
                    }

                }
                if (planningItem.Object is PlanSetup)
                {
                    PlanSetup planSetup = (PlanSetup)planningItem.Object;
                    planDoseDouble = planSetup.TotalDose.Dose;
                }
                prescribedDose = new DoseValue(planDoseDouble, DoseValue.DoseUnit.cGy);
                Group eval = testMatch[0].Groups["evalpt"];
                Group unit = testMatch[0].Groups["unit"];
                DoseValue.DoseUnit du = (unit.Value.CompareTo("%") == 0) ? DoseValue.DoseUnit.Percent :
                    (unit.Value.CompareTo("cGy") == 0) ? DoseValue.DoseUnit.cGy : DoseValue.DoseUnit.Unknown;
                var body = planningItem.StructureSet.Structures.Where(x => x.Id.Contains("BODY")).First().Object;
                //VolumePresentation vpFinal = (evalunit.Value.CompareTo("%") == 0) ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                VolumePresentation vpFinal = VolumePresentation.AbsoluteCm3;
                DoseValuePresentation dvpFinal = (evalunit.Value.CompareTo("%") == 0) ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
                DoseValue dv = new DoseValue(double.Parse(eval.Value) / 100 * prescribedDose.Dose, DoseValue.DoseUnit.cGy);
                double bodyWithPrescribedDoseVolume = planningItem.Object.GetVolumeAtDose(body, prescribedDose, vpFinal);
                double targetWithPrescribedDoseVolume = planningItem.Object.GetVolumeAtDose(evalStructure.Object, dv, vpFinal);
                double targetVolume = Convert.ToDouble(evalStructure.VolumeValue);
                var cn = (targetWithPrescribedDoseVolume / targetVolume) * (targetWithPrescribedDoseVolume / bodyWithPrescribedDoseVolume);
                return string.Format("{0:0.0}", cn);
            }
            catch (NullReferenceException)
            {
                return "Unable to calculate - DVH is not valid";
            }
        }
    }
}
