using System.IO;

namespace PlanCheck.Helpers
{
    public static class AssemblyHelper
    {
        public static string GetAssemblyDirectory()
        {
            return Path.GetDirectoryName(GetAssemblyPath());
        }

        public static string GetAssemblyPath()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }
    }
}
