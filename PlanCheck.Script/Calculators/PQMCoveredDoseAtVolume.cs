using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;

namespace PlanCheck.Calculators
{
    class PQMCoveredDoseAtVolume
    {
        public static string GetCoveredDoseAtVolume(PlanningItemViewModel planningItem, StructureViewModel evalStructure, MatchCollection testMatch, Group evalunit)
        {
            System.Console.WriteLine("Covered Dose at Volume");
            return "Not supported";
        }
    }
}
