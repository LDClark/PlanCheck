using System.Windows;
using EsapiEssentials.PluginRunner;
using VMS.TPS;
using VMS.TPS.Common.Model.API;

namespace PlanCheck.Runner
{
    public partial class App : System.Windows.Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            // Note: EsapiEssentials and EsapiEssentials.PluginRunner must be referenced,
            // as well as the project that contains the Script class
            ScriptRunner.Run(new Script());
        }

        // Fix UnauthorizedScriptingAPIAccessException
        public void DoNothing(PlanSetup plan) { }
    }
}