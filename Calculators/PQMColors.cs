using System;
using System.Text.RegularExpressions;
using System.Windows.Media;
using VMS.TPS.Common.Model.API;

namespace PlanCheck.Calculators
{
    public class PQMColors
    {
        public static Tuple<SolidColorBrush, double> GetAchievedColor(Structure structure, string goal, string DVHObjective, string Achieved)
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
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFF")); //white
                achievedDouble = 0;
            }
            if (structure == null)
            {
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFF")); //white
                achievedDouble = 0;
                return Tuple.Create(achievedColor, achievedRatio);
            }          
            achievedRatio = 0;
            double goalDouble = Convert.ToDouble(Regex.Match(goal.ToString(), @"(\d+(\.\d+)?)|(\.\d+)").Value);
            if (goal.Contains("<"))  //D at V, V at D, serial tissue
            {
                achievedRatio = Convert.ToInt32(achievedDouble / goalDouble * 100);
                achievedColor = GetNormalTissueSolidColorBrush(achievedRatio);
            }
            if (goal.Contains(">") && DVHObjective.ToString().Contains("CV"))  //CV parallel tissue
            {
                var structVol = structure.Volume;
                achievedRatio = Convert.ToInt32((structVol - achievedDouble) / (structVol - goalDouble));
                achievedColor = GetNormalTissueSolidColorBrush(achievedRatio);
            }

            else if (goal.ToString().Contains(">")) //Target
            {
                achievedRatio = Convert.ToInt32(achievedDouble / goalDouble * 100);
                achievedColor = GetTargetSolidColorBrush(achievedRatio);
            }
            return Tuple.Create(achievedColor, achievedRatio);
        }

        private static SolidColorBrush GetNormalTissueSolidColorBrush(double achievedRatio)
        {
            var achievedColor = new SolidColorBrush();
            if (achievedRatio >= 100)
            {
                achievedRatio = 100;
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFE07979")); //red
            }
            if (achievedRatio >= 75 && achievedRatio < 100)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFEEA862")); //orange;
            if (achievedRatio >= 50 && achievedRatio < 75)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFF0F3A4")); //yellow;
            if (achievedRatio >= 25 && achievedRatio < 50)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA7F3A4")); //light green
            if (achievedRatio >= 0 && achievedRatio < 25)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA7F3A4")); //light green
            if (achievedRatio < 0)
                achievedColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA7F3A4")); //light green
            return achievedColor;
        }

        private static SolidColorBrush GetTargetSolidColorBrush(double achievedRatio)
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
