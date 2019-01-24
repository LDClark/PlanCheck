
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

namespace VMS.TPS
{
    public class Script
    {
        public void Run(
        User user,
        Patient patient,
        Image image,
        StructureSet structureSet,
        PlanSetup planSetup,
        IEnumerable<PlanSetup> planSetupsInScope,
        IEnumerable<PlanSum> planSumsInScope,
        Window mainWindow)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string scriptVersion = fvi.FileVersion;
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            string eclipseVersion = System.Reflection.Assembly.GetAssembly
                (typeof(VMS.TPS.Common.Model.API.Application)).GetName().Version.ToString();
            if (planSumsInScope == null && planSetup == null)
            {
                mainWindow.Title = "No plans open";
                mainWindow.Content = "Please open a plan or plansum";
            }
            if (planSumsInScope.Count() > 1 && planSetup == null)  //plansums and/or plan(s) are in the scope window
            {
                var planSelectViewModel = new PlanSelectViewModel(user, patient, scriptVersion, planSetup, planSetupsInScope, planSumsInScope, mainWindow);
                var planSelectView = new PlanSelectView(planSelectViewModel);
                mainWindow.Title = "Select a plan and constraint template";
                mainWindow.Content = planSelectView;
            }
            else 
            {
                PlanningItem selectedPlanningItem;
                if (planSumsInScope.Count() == 1 && planSetup == null)  //only one plansum in scope window
                {
                    selectedPlanningItem = planSumsInScope.FirstOrDefault();
                }
                else //only plansetups are in scope window
                {
                    selectedPlanningItem = planSetup;
                }
                DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
                string firstFileName = constraintDir.GetFiles().FirstOrDefault().ToString();
                string firstConstraintFilePath = Path.Combine(constraintDir.ToString(), firstFileName);
                var activeConstraintPath = new ConstraintViewModel(firstConstraintFilePath);
                var planningItemList = PlanningItemListViewModel.GetPlanningItemList(planSetupsInScope, planSumsInScope);
                var mainViewModel = new MainViewModel(user, patient, scriptVersion, activeConstraintPath.ConstraintPath, planningItemList, new PlanningItemViewModel(selectedPlanningItem));
                mainWindow.Title = mainViewModel.Title;
                var mainView = new MainView(mainViewModel);
                mainWindow.Content = mainView;
            }
        }

	public void Execute(ScriptContext scriptContext, Window mainWindow)
	{
            Run(scriptContext.CurrentUser,
            scriptContext.Patient,
            scriptContext.Image,
            scriptContext.StructureSet,
            scriptContext.PlanSetup,
            scriptContext.PlansInScope,
            scriptContext.PlanSumsInScope,
            mainWindow);
        }
    }
}
