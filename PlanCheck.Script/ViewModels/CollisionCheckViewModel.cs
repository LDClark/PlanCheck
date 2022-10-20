using System.Windows.Media.Media3D;

namespace PlanCheck
{
    public class CollisionCheckViewModel
    {
        public bool View { get; set; }
        public string FieldID { get; set; }
        public string GantryToTable { get; set; }
        public string GantryToBody { get; set; }
        public string Status { get; set; }
    }
}
