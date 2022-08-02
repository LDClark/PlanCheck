using PlanCheck.Reporting;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public interface IEsapiService
    {
        Task<PlanningItemViewModel[]> GetPlansAsync();
        Task<ReportPatient> GetReportPatientAsync();
        Task<ObservableCollection<StructureViewModel>> GetStructuresAsync(string courseId, string planId);
        Task<string[]> GetBeamIdsAsync(string courseId, string planId);
        Task<Point3D> GetIsocenterAsync(string courseId, string planId, string beamId);
        Task<ObservableCollection<ErrorViewModel>> GetErrorsAsync(string courseId, string planId);
        Task<CollisionCheckViewModel> GetBeamCollisionsAsync(string courseId, string planId, string beamId);
        Task<Model3DGroup> AddFieldMeshAsync(Model3DGroup modelGroup, string courseId, string planId, string beamId, string status);
        Task<Model3DGroup> AddCouchBodyAsync(string courseId, string planId);
        Task<Point3D> GetCameraPositionAsync(string courseId, string planId, string beamId);
        Task<string> CalculateMetricDoseAsync(string courseId, string planId, string structureId, string structureCode, string dvhObjective);
        Task<string> EvaluateMetricDoseAsync(string result, string goal, string variation);
    }
}
