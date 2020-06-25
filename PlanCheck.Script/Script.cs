
////////////////////////////////////////////////////////////////////////////////
///A version 15.5 read-only plugin script that checks Plan DVH metrics including
///planSums and planSetups, checks for collisions between Body/Support/Gantry 
///(gantry distances were from a Trilogy), and hard-coded plan/structure/dose
///checks.  Uses EclipsePluginRunner.
////////////////////////////////////////////////////////////////////////////////

using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using PlanCheck;
using System.Diagnostics;
using System.Linq;
using System.IO;
using PlanCheck.Helpers;
using EsapiEssentials.Plugin;

namespace VMS.TPS
{
    public class Script : ScriptBase
    {
        public override void Run(PluginScriptContext context)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string scriptVersion = fvi.FileVersion;
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            string eclipseVersion = System.Reflection.Assembly.GetAssembly
                (typeof(VMS.TPS.Common.Model.API.Application)).GetName().Version.ToString();
            if (context.PlanSumsInScope == null && context.PlanSetup == null)
            {
                MessageBox.Show("Please open a plan or plansum");
            }
            if (context.PlanSumsInScope.Count() > 1 && context.PlanSetup == null)  //plansums and/or plan(s) are in the scope window
            {
                var planSelectViewModel = new PlanSelectViewModel(context.CurrentUser, context.Patient, scriptVersion, context.PlanSetup, context.PlansInScope, context.PlanSumsInScope);
                var planSelectView = new PlanSelectView(planSelectViewModel);
            }
            else 
            {
                PlanningItem selectedPlanningItem;
                if (context.PlanSumsInScope.Count() == 1 && context.PlanSetup == null)  //only one plansum in scope window
                {
                    selectedPlanningItem = context.PlanSumsInScope.FirstOrDefault();
                }
                else //only plansetups are in scope window
                {
                    selectedPlanningItem = context.PlanSetup;
                }
                DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
                string firstFileName = constraintDir.GetFiles().FirstOrDefault().ToString();
                string firstConstraintFilePath = Path.Combine(constraintDir.ToString(), firstFileName);
                var activeConstraintPath = new ConstraintViewModel(firstConstraintFilePath);
                var planningItemList = PlanningItemListViewModel.GetPlanningItemList(context.PlansInScope, context.PlanSumsInScope);
                var mainViewModel = new MainViewModel(context.CurrentUser, context.Patient, scriptVersion, planningItemList, new PlanningItemViewModel(selectedPlanningItem));
                Window window = new MainWindow(mainViewModel);
                window.DataContext = mainViewModel;
                window.ShowDialog();
            }
        }
    }
}
