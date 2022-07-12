using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace PlanCheck
{
    public class ConstraintListViewModel
    {
        public ObservableCollection<ConstraintViewModel> ConstraintList { get; set; }

        public ConstraintListViewModel(string constraintDir)
        {
            ConstraintList = new ObservableCollection<ConstraintViewModel>();
            foreach (string file in Directory.EnumerateFiles(constraintDir, "*.csv"))
            {
                var constraintViewModel = new ConstraintViewModel(file);
                ConstraintList.Add(constraintViewModel);
            }
        }
    }
}
