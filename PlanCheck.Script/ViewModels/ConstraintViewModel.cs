using System;
using System.IO;
using System.Windows;

namespace PlanCheck
{
    public class ConstraintViewModel
    {
        public string ConstraintName { get; set; }
        public string ConstraintPath { get; set; }

        public ConstraintViewModel(string constraintPath)
        {
            ConstraintName = Path.GetFileNameWithoutExtension(constraintPath);
            ConstraintPath = constraintPath;          
        }
    }
}
