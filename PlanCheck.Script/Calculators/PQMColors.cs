using System;
using System.Text.RegularExpressions;
using System.Windows.Media;
using VMS.TPS.Common.Model.API;

namespace PlanCheck.Calculators
{
    public class PQMColors
    {
        public static Tuple<SolidColorBrush, double> GetAchievedRatio(StructureViewModel structure, string goal, string DVHObjective, string Achieved)
        {
            var achievedColor = new SolidColorBrush();
            double achievedDouble;
            double achievedRatio = 0;
            try
            {
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA7F3A4")); //green
                achievedDouble = Convert.ToDouble(Regex.Match(Achieved.ToString(), @"\d+").Value);
            }
            catch (FormatException)
            {
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF2B2B2B")); //black
                achievedDouble = 0;
            }
            if (structure == null)
            {
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF2B2B2B")); //black
                achievedDouble = 0;
                return Tuple.Create(achievedColor, achievedRatio);
            }          
            double goalDouble = Convert.ToDouble(Regex.Match(goal.ToString(), @"(\d+(\.\d+)?)|(\.\d+)").Value);
            achievedRatio = achievedDouble / goalDouble;
            if (goal.Contains("<"))  //D at V, V at D, serial tissue
                achievedColor = GetDoseVolumeSolidColorBrush(achievedRatio);
            if (goal.Contains(">") && DVHObjective.ToString().Contains("CV"))  //CV parallel tissue
                achievedColor = GetCriticalVolumeSolidColorBrush(achievedRatio);
            else if (goal.ToString().Contains(">")) //Target
                achievedColor = GetTargetSolidColorBrush(achievedRatio);
            return Tuple.Create(achievedColor, achievedRatio);
        }

        public static SolidColorBrush GetDoseVolumeSolidColorBrush(double achievedRatio)
        {
            var achievedColor = new SolidColorBrush();
            if (achievedRatio >= 1)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#870707")); //red
            if (achievedRatio >= 0.75 && achievedRatio < 1)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#905e03")); //orange;
            if (achievedRatio >= 0.50 && achievedRatio < 0.75)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#787800")); //yellow;
            if (achievedRatio >= 0.25 && achievedRatio < 0.50)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#004500")); //light green
            if (achievedRatio >= 0 && achievedRatio < 0.25)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#004500")); //light green
            if (achievedRatio < 0)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#004500")); //light green
            return achievedColor;
        }

        public static SolidColorBrush GetCriticalVolumeSolidColorBrush(double achievedRatio)
        {
            var achievedColor = new SolidColorBrush();
            if (achievedRatio >= 1)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#004500")); //light green
            if (achievedRatio < 1)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#870707")); //red;
            return achievedColor;
        }

        public static SolidColorBrush GetTargetSolidColorBrush(double achievedRatio)
        {
            var achievedColor = new SolidColorBrush();
            if (achievedRatio >= 100)
            {
                achievedRatio = 100;
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA7F3A4")); //light green
            }
            if (achievedRatio >= 90 && achievedRatio < 100)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA7F3A4")); //light green
            if (achievedRatio >= 50 && achievedRatio < 90)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFEEA862")); //orange;
            if (achievedRatio < 50)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFE07979")); //red;
            return achievedColor;
        }
    }
}
