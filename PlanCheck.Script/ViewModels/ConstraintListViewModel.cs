using PlanCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace PlanCheck
{
    public class ConstraintListViewModel
    {
        public ObservableCollection<ConstraintViewModel> ConstraintList { get; set; }

        public ConstraintListViewModel()
        {
            ConstraintList = new ObservableCollection<ConstraintViewModel>();
            DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
            if (!Directory.Exists(constraintDir.FullName))
            {
                if (MessageBox.Show(string.Format("The template folder 'ConstraintTemplates' does not exist.  " +
                    "Create the directory and a default constraint?", constraintDir.FullName), 
                    "Folder not found", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var file = Path.Combine(constraintDir.FullName, "Default.csv");
                    Directory.CreateDirectory(constraintDir.FullName);
                    File.Create(file).Dispose();
                    MessageBox.Show(string.Format("The file {0} has been created.", file));
                }
            }
            foreach (string file in Directory.EnumerateFiles(constraintDir.ToString(), "*.csv"))
            {
                var constraintViewModel = new ConstraintViewModel(file);
                ConstraintList.Add(constraintViewModel);
            }
        }
    }
}
