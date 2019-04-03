using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;

namespace PlanCheck.Calculators
{
    class PQMCoveredDoseAtVolume
    {
        public static string GetCoveredDoseAtVolume(StructureSet structureSet, PlanningItemViewModel planningItem, Structure evalStructure, MatchCollection testMatch, Group evalunit)
        {
            System.Console.WriteLine("Covered Dose at Volume");
            return "Not supported";
        }
    }
}
