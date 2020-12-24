using System.Linq;
using System.Threading.Tasks;
using EsapiEssentials.Plugin;
using VMS.TPS.Common.Model.API;
using System.IO;
using PlanCheck.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using System;
using PlanCheck.Reporting;

namespace PlanCheck
{
    public class EsapiService : EsapiServiceBase<PluginScriptContext>, IEsapiService
    {
        private readonly MetricCalculator _metricCalc;

        public EsapiService(PluginScriptContext context) : base(context)
        {
            _metricCalc = new MetricCalculator();
        }

        public Task<Plan[]> GetPlansAsync() =>
           RunAsync(context => context.Patient.Courses?
               .SelectMany(x => x.GetPlanSetupsAndSums())
               .Select(x => new Plan
               {
                   PlanId = x.Id,
                   CourseId = x.GetCourse().Id,
                   PlanType = Extensions.GetPlanType(x),
                   PlanCreation = Extensions.GetCreationDateTime(x),
                   PlanStructureSetId = Extensions.GetStructureSetId(x),
                   PlanImageId = Extensions.GetPlanImageId(x),
                   PlanImageCreation = Extensions.GetPlanImageCreation(x),
                   PlanIdWithFractionation = x.Id + Extensions.GetFractionation(x) 
               })
               .ToArray());

        public Task<ObservableCollection<StructureViewModel>> GetStructuresAsync(string courseId, string planId) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var ss = planningItem?.StructureSet;
                var ssvm = StructureSetListViewModel.GetStructureList(ss);
                return ssvm;
            });

        public Task<string[]> GetBeamIdsAsync(string courseId, string planId) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var planSetup = (PlanSetup)planningItem;          
                return planSetup.Beams.Where(x => x.IsSetupField != true).Select(x => x.Id).ToArray() ?? new string[0];
            });

        public Task<Point3D> GetCameraPositionAsync(string courseId, string planId, string beamId) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var plan = (PlanSetup)planningItem;
                var beam = plan.Beams.FirstOrDefault(x => x.Id == beamId);
                return CollisionSummariesCalculator.GetCameraPosition(beam);
            });

        public Task<Point3D> GetIsocenterAsync(string courseId, string planId, string beamId) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var plan = (PlanSetup)planningItem;
                var beam = plan.Beams.FirstOrDefault(x => x.Id == beamId);
                return CollisionSummariesCalculator.GetIsocenter(beam);
            });

        public Task<ObservableCollection<ErrorViewModel>> GetErrorsAsync(string courseId, string planId) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var calculator = new ErrorCalculator();
                var errorGrid = calculator.Calculate(planningItem);
                return errorGrid;
            });

        public Task<CollisionCheckViewModel> GetBeamCollisionsAsync(string courseId, string planId, string beamId) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var calculator = new CollisionSummariesCalculator();
                var plan = (PlanSetup)planningItem;
                var beam = plan.Beams.FirstOrDefault(x => x.Id == beamId);
                return calculator.CalculateBeamCollision(plan, beam);
            });

        public Task<Model3DGroup> AddCouchBodyAsync(string courseId, string planId) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var structureSet = planningItem.StructureSet;
                var body = structureSet.Structures.Where(x => x.Id.Contains("BODY")).First();
                Structure couch = null;
                try
                {
                    foreach (Structure structure in structureSet.Structures)
                    {
                        if (structure.StructureCodeInfos.FirstOrDefault().Code == "Support")
                        {
                            couch = structure;
                        }
                    }
                }
                catch
                {
                }
                return CollisionSummariesCalculator.AddCouchBodyMesh(body, couch);
            });
        public Task<Model3DGroup> AddFieldMeshAsync(Model3DGroup modelGroup, string courseId, string planId, string beamId, string status) =>
            RunAsync(context =>
            {
                var planningItem = Extensions.GetPlanningItem(context.Patient, courseId, planId);
                var plan = (PlanSetup)planningItem;
                var beam = plan.Beams.FirstOrDefault(x => x.Id == beamId);
                return CollisionSummariesCalculator.AddFieldMesh(plan, beam, status);
            });

        public Task<PQMViewModel[]> GetObjectivesAsync(ConstraintViewModel constraint) =>
            RunAsync(context =>
            {
                var objectives = Objectives.GetObjectives(constraint);
                return objectives.ToArray() ?? new PQMViewModel[0];
            });

        public Task<string> CalculateMetricDoseAsync(string courseId, string planId, string structureCode, string templateCode, string dvhObjective, string goal, string variation) =>
            RunAsync(context => CalculateMetricDose(context.Patient, courseId, planId, structureCode, templateCode, dvhObjective, goal, variation));

        public string CalculateMetricDose(Patient patient, string courseId, string planId, string structureCode, string templateCode, string dvhObjective, string goal, string variation)
        {
            var plan = Extensions.GetPlanningItem(patient, courseId, planId);
            var planVM = new PlanningItemViewModel(plan);
            var structure = Extensions.GetStructure(plan, structureCode);

            DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
            string firstFileName = constraintDir.GetFiles().FirstOrDefault().ToString();
            string firstConstraintFilePath = Path.Combine(constraintDir.ToString(), firstFileName);

            // make sure the workbook template exists
            if (!System.IO.File.Exists(firstConstraintFilePath))
            {
                System.Windows.MessageBox.Show(string.Format("The template file '{0}' chosen does not exist.", firstConstraintFilePath));
            }
            var structureVM = new StructureViewModel(structure);
            string metric = "";
            string result = "";

            if (templateCode == structureCode)
            {
                metric = dvhObjective;
                result = _metricCalc.CalculateMetric(planVM.PlanningItemStructureSet, structureVM, planVM, metric);
            }                 
            else
                result = "";                
            return result;
        }

        public string EvaluateMetricDose(string result, string goal, string variation)
        {
            var met = "";
            met = _metricCalc.EvaluateMetric(result, goal, variation);
            return met;
        }

        public Task<string> EvaluateMetricDoseAsync(string result, string goal, string variation) =>
            RunAsync(context => EvaluateMetricDose(result, goal, variation));

        public Task<ReportPatient> GetReportPatientAsync() =>
            RunAsync(context => new ReportPatient
            {
                Id = context.Patient.Id,
                FirstName = context.Patient.FirstName,
                LastName = context.Patient.LastName,
                Sex = GetPatientSex(context.Patient.Sex),
                Birthdate = (DateTime)context.Patient.DateOfBirth,
                Doctor = new Doctor
                {
                    Name = context.Patient.PrimaryOncologistId
                }
            });

        private Sex GetPatientSex(string sex)
        {
            switch (sex)
            {
                case "Male": return Sex.Male;
                case "Female": return Sex.Female;
                case "Other": return Sex.Other;
                default: throw new ArgumentOutOfRangeException();
            }

        }
    }
}
