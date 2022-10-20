using System.Text.RegularExpressions;
using System;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Linq;

namespace PlanCheck.Calculators
{
    class PQMVolumeAtDose
    {
        public static string GetVolumeAtDose(PlanningItemViewModel planningItem, StructureViewModel evalStructure, MatchCollection testMatch, Group evalunit)
        {
            try
            {
                //check for sufficient sampling and dose coverage
                DVHData dvh = planningItem.Object.GetDVHCumulativeData(evalStructure.Object, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
                if ((dvh.SamplingCoverage < 0.9) || (dvh.Coverage < 0.9))
                {
                    return "Unable to calculate - insufficient dose or sampling coverage";
                }
                Group eval = testMatch[0].Groups["evalpt"];
                Group unit = testMatch[0].Groups["unit"];
                DoseValue.DoseUnit du = (unit.Value.CompareTo("%") == 0) ? DoseValue.DoseUnit.Percent :
                        (unit.Value.CompareTo("cGy") == 0) ? DoseValue.DoseUnit.cGy : DoseValue.DoseUnit.Gy;
                VolumePresentation vp = (unit.Value.CompareTo("%") == 0) ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                DoseValue dv = new DoseValue(double.Parse(eval.Value), du);

                if ((unit.Value.CompareTo("cGy") == 0) && (dvh.MaxDose.Unit.CompareTo(DoseValue.DoseUnit.Gy) == 0))
                    dv = new DoseValue(double.Parse(eval.Value) / 100, DoseValue.DoseUnit.Gy);
                if ((unit.Value.CompareTo("Gy") == 0) && (dvh.MaxDose.Unit.CompareTo(DoseValue.DoseUnit.cGy) == 0))
                    dv = new DoseValue(double.Parse(eval.Value) * 100, DoseValue.DoseUnit.cGy);

                double volume = double.Parse(eval.Value);
                VolumePresentation vpFinal = (evalunit.Value.CompareTo("%") == 0) ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                DoseValuePresentation dvpFinal = (evalunit.Value.CompareTo("%") == 0) ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
                double volumeAchieved = planningItem.Object.GetVolumeAtDose(evalStructure.Object, dv, vpFinal);

                return string.Format("{0:0.00} {1}", volumeAchieved, evalunit.Value);   // todo: better formatting based on VolumePresentation
            }
            catch (NullReferenceException)
            {
                return "Unable to calculate - DVH is not valid";
            }
        }
    }
}
