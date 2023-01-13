using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PlanCheck.Calculators;
using PlanCheck.Helpers;
using PlanCheck.Reporting;
using PlanCheck.Reporting.MigraDoc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{

    public class MainViewModel : ViewModelBase
    {
        private readonly IEsapiService _esapiService;
        private readonly IDialogService _dialogService;

        public MainViewModel(IEsapiService esapiService, IDialogService dialogService)
        {
            _esapiService = esapiService;
            _dialogService = dialogService;
        }

        private PlanningItemViewModel[] _plans;
        public PlanningItemViewModel[] Plans
        {
            get => _plans;
            set => Set(ref _plans, value);
        }

        private PlanningItemViewModel _selectedPlan;
        public PlanningItemViewModel SelectedPlan
        {
            get => _selectedPlan;
            set => Set(ref _selectedPlan, value);
        }

        private ObservableCollection<PQMViewModel> _pqms;
        public ObservableCollection<PQMViewModel> PQMs
        {
            get => _pqms;
            set => Set(ref _pqms, value);
        }

        private ObservableCollection<ErrorViewModel> _errorGrid;
        public ObservableCollection<ErrorViewModel> ErrorGrid
        {
            get => _errorGrid;
            set => Set(ref _errorGrid, value);
        }

        private ObservableCollection<ConstraintViewModel> _constraints;
        public ObservableCollection<ConstraintViewModel> Constraints
        {
            get => _constraints;
            set => Set(ref _constraints, value);
        }

        private ConstraintViewModel _selectedConstraint;
        public ConstraintViewModel SelectedConstraint
        {
            get => _selectedConstraint;
            set => Set(ref _selectedConstraint, value);
        }

        private ObservableCollection<CollisionCheckViewModel> _collisionSummaries;
        public ObservableCollection<CollisionCheckViewModel> CollisionSummaries
        {
            get => _collisionSummaries;
            set => Set(ref _collisionSummaries, value);
        }

        private Model3DGroup _collimatorModel;
        public Model3DGroup CollimatorModel
        {
            get => _collimatorModel;
            set => Set(ref _collimatorModel, value);
        }

        private Model3DGroup _couchBodyModel;
        public Model3DGroup CouchBodyModel
        {
            get => _couchBodyModel;
            set => Set(ref _couchBodyModel, value);
        }

        private Point3D _isoctr;
        public Point3D Isoctr
        {
            get => _isoctr;
            set => Set(ref _isoctr, value);
        }

        private Point3D _cameraPosition;
        public Point3D CameraPosition
        {
            get => _cameraPosition;
            set => Set(ref _cameraPosition, value);
        }

        private Vector3D _upDir;
        public Vector3D UpDir
        {
            get => _upDir;
            set => Set(ref _upDir, value);
        }

        private Vector3D _lookDir;
        public Vector3D LookDir
        {
            get => _lookDir;
            set => Set(ref _lookDir, value);
        }

        private double _sliderValue;
        public double SliderValue
        {
            get => _sliderValue;
            set
            {
                _sliderValue = value;
                RaisePropertyChanged("SliderValue");
                GetCameraPosition();
            }
        }

        private bool _CCIsEnabled;
        public bool CCIsEnabled
        {
            get => _CCIsEnabled;
            set => Set(ref _CCIsEnabled, value);
        }

        public ICommand StartCommand => new RelayCommand(Start);
        public ICommand AnalyzePlanCommand => new RelayCommand(AnalyzePlanPQMs);
        public ICommand AnalyzeCollisionCommand => new RelayCommand(GetCollisionSummary);
        public ICommand PrintCommand => new RelayCommand(PrintPlan);
        public ICommand UpdatePQMCommand => new RelayCommand(UpdatePQM);
        public ICommand AddFieldCommand => new RelayCommand(ChangeFieldModelCC);
        public ICommand RemoveFieldCommand => new RelayCommand(ChangeFieldModelCC);

        private async void Start()
        {
            Constraints = new ConstraintListViewModel().ConstraintList;
            SelectedConstraint = Constraints.First();
            Plans = await _esapiService.GetPlansAsync();
        }

        public async void AnalyzePlanPQMs()
        {
            var courseId = SelectedPlan?.CourseId;
            var planId = SelectedPlan?.Id;
            var structureSetId = SelectedPlan?.StructureSetId;
            var type = SelectedPlan?.Type;
            if (type == "PlanSum")
                CCIsEnabled = false;
            if (type == "Plan")
                CCIsEnabled = true;

            if (courseId == null || planId == null || structureSetId == null)
                return;
            var structures = new ObservableCollection<StructureViewModel>();

            structures = await _esapiService.GetStructuresAsync(courseId, planId);
            CollimatorModel = null;
            CouchBodyModel = null;
            CollisionSummaries = null;
            
            if (type == "Plan")
            {
                var beamIds = await _esapiService.GetBeamIdsAsync(courseId, planId);

                _dialogService.ShowProgressDialog("Adding body and couch...",
                async progress =>
                {
                    CouchBodyModel = new Model3DGroup();
                    var couchBodyModel = await _esapiService.AddCouchBodyAsync(courseId, planId);
                    CouchBodyModel.Children.Add(couchBodyModel);
                });

                _dialogService.ShowProgressDialog("Getting camera position...",
                async progress =>
                {
                    CameraPosition = await _esapiService.GetCameraPositionAsync(courseId, planId, beamIds.FirstOrDefault());
                });

                _dialogService.ShowProgressDialog("Getting isocenter...",
                async progress =>
                {
                    Isoctr = await _esapiService.GetIsocenterAsync(courseId, planId, beamIds.FirstOrDefault());
                });

                _dialogService.ShowProgressDialog("Adding collimator...", beamIds.Length,
                async progress =>
                {
                    CollisionSummaries = new ObservableCollection<CollisionCheckViewModel>();
                    UpDir = new Vector3D(0, -1, 0);
                    LookDir = new Vector3D(0, 0, 1);
                    CollimatorModel = new Model3DGroup();
                    foreach (var beamId in beamIds)
                    {
                        var collisionSummary = new CollisionCheckViewModel
                        {
                            View = true,
                            FieldID = beamId,
                            Status = "Not calculated"
                        }
;                       CollisionSummaries.Add(collisionSummary);
                        var collimatorModel = await _esapiService.AddFieldMeshAsync(CollimatorModel, courseId, planId, beamId, collisionSummary.Status);
                        CollimatorModel.Children.Add(collimatorModel);
                        progress.Increment();
                    }
                });
            }

            PQMViewModel[] pqms = ObjectiveViewModel.GetObjectives(SelectedConstraint);

            _dialogService.ShowProgressDialog("Calculating dose metrics...", structures.Count(),
                async progress => {
                    PQMs = new ObservableCollection<PQMViewModel>();
                    foreach (var structure in structures)
                    {
                        foreach (var pqm in pqms.Where(i => i != null))
                        {
                            string templateSelected = string.Empty;
                            if (structure.Id.ToUpper().CompareTo(pqm.TemplateId.ToUpper()) == 0) //id matches
                            {
                                if (structure.Code != null)
                                    templateSelected = pqm.TemplateId + " : " + structure.Code; //add in the structure code
                                else
                                    templateSelected = pqm.TemplateId;
                                await GetPQM(courseId, planId, structure, pqm, templateSelected, structures);
                                progress.Increment();
                                continue;
                            }
                            else
                            {
                                foreach (string alias in pqm.TemplateAliases)
                                {
                                    if (structure.Id.ToUpper().CompareTo(alias.ToUpper()) == 0) //alias matches
                                    {
                                        //add in the structure code and alias
                                        if (structure.Code != null)
                                            templateSelected = pqm.TemplateId + "|" + alias + " : " + structure.Code; 
                                        else
                                            templateSelected = alias;
                                        await GetPQM(courseId, planId, structure, pqm, templateSelected, structures);
                                        progress.Increment();
                                        continue;
                                    }
                                }
                            }
                            if (structure.Code != null)
                            {
                                //check to see if structure has already been found by name
                                if (PQMs.Any(x => x.StructureNameWithCode != structure.NameWithCode)) 
                                {
                                    foreach (var code in pqm.TemplateCodes)
                                        if (code == structure.Code) //code matches
                                        {
                                            templateSelected = pqm.TemplateId + " : " + code; //add in the PQM id
                                            await GetPQM(courseId, planId, structure, pqm, templateSelected, structures);
                                            progress.Increment();
                                            continue;
                                        }
                                }
                            }                           
                        }                      
                    }
                });


            _dialogService.ShowProgressDialog("Finding errors...",
                async progress =>
                {
                    ErrorGrid = await _esapiService.GetErrorsAsync(courseId, planId);
                });
        }

        private async Task GetPQM(string courseId, string planId, StructureViewModel structure, PQMViewModel pqm, 
            string templateSelected, ObservableCollection<StructureViewModel> structures)
        {
            string achieved = await _esapiService.CalculateMetricDoseAsync(courseId, planId, structure.Id, structure.Code, pqm.DVHObjective);
            string met = await _esapiService.EvaluateMetricDoseAsync(achieved, pqm.Goal, pqm.Variation);
            var tuple = PQMColors.GetAchievedRatio(structure, pqm.Goal, pqm.DVHObjective, achieved);
            var ratio = tuple.Item2;
            var color = tuple.Item1;
            var percentage = ratio * 100;
            PQMs.Add(new PQMViewModel
            {
                TemplateId = templateSelected,
                StructureList = structures,
                SelectedStructure = structure,
                StructureNameWithCode = structure.NameWithCode,
                StructureVolume = structure.VolumeValue,
                AchievedPercentageOfGoal = percentage,
                AchievedColor = color,
                DVHObjective = pqm.DVHObjective,
                Goal = pqm.Goal,
                Met = met,
                Achieved = achieved,
                PlanId = planId,
                CourseId = courseId,
            });
        }

        private void UpdatePQM()
        {
            _dialogService.ShowProgressDialog("Recalculating PQMs...", PQMs.Count(),
            async progress =>
            {
                foreach (var pqm in PQMs)
                {
                    pqm.Achieved = await _esapiService.CalculateMetricDoseAsync(SelectedPlan.CourseId, SelectedPlan.Id, pqm.SelectedStructure.Id, 
                        pqm.SelectedStructure.Code, pqm.DVHObjective);
                    pqm.StructureVolume = pqm.SelectedStructure.VolumeValue;
                    pqm.Met = await _esapiService.EvaluateMetricDoseAsync(pqm.Achieved, pqm.Goal, pqm.Variation);
                    var tuple = PQMColors.GetAchievedRatio(pqm.SelectedStructure, pqm.Goal, pqm.DVHObjective, pqm.Achieved);
                    pqm.AchievedColor = tuple.Item1;
                    pqm.AchievedPercentageOfGoal = tuple.Item2 * 100;
                }
                progress.Increment();
            });
        }

        public void GetCameraPosition()
        {
            var angle = SliderValue * Math.PI / 180;
            double x = -2000 * Math.Sin(angle);
            double z = -2000 * Math.Cos(angle);
            LookDir = new Vector3D(-x, 0, -z);
            CameraPosition = new Point3D(x, 0, z);
        }

        public async void GetCollisionSummary()
        {
            var planId = SelectedPlan?.Id;
            var courseId = SelectedPlan?.CourseId;
            var type = SelectedPlan?.Type;
            if (type == "Plan")
            {
                var beamIds = await _esapiService.GetBeamIdsAsync(courseId, planId);

                _dialogService.ShowProgressDialog("Calculating collisions...", beamIds.Length,
                async progress =>
                {
                    CollisionSummaries = new ObservableCollection<CollisionCheckViewModel>();
                    UpDir = new Vector3D(0, -1, 0);
                    LookDir = new Vector3D(0, 0, 1);
                    CollimatorModel = new Model3DGroup();
                    foreach (var beamId in beamIds)
                    {
                        var collisionSummary = await _esapiService.GetBeamCollisionsAsync(courseId, planId, beamId);
                        CollisionSummaries.Add(collisionSummary);
                        var collimatorModel = await _esapiService.AddFieldMeshAsync(CollimatorModel, courseId, planId, beamId, collisionSummary.Status);
                        CollimatorModel.Children.Add(collimatorModel);
                        progress.Increment();
                    }
                });
            }
        }

        private async void ChangeFieldModelCC()
        {
            if (SelectedPlan?.Type == "Plan")
            {
                var planId = SelectedPlan?.Id;
                var courseId = SelectedPlan?.CourseId;
                var beamIds = await _esapiService.GetBeamIdsAsync(courseId, planId);
                _dialogService.ShowProgressDialog("Changing field view...",
                async progress =>
                {
                    int i = 0;
                    CollimatorModel = new Model3DGroup();
                    foreach (var beamId in beamIds)
                    {

                        if (CollisionSummaries[i].View)
                        {
                            var collimatorModel = await _esapiService.AddFieldMeshAsync(CollimatorModel, courseId, planId, beamId, CollisionSummaries[i].Status);
                            CollimatorModel.Children.Add(collimatorModel);
                            progress.Increment();
                        }
                        i++;
                    }
                    progress.Increment();
                });
            }
        }

        private async void PrintPlan()
        {
            var reportService = new ReportPdf();
            var reportPQMs = new ReportPQMs();

            int numPqms = PQMs.Count();
            ReportPQM[] reportPQMList = new ReportPQM[numPqms];
            int i = 0;
            foreach (var pqm in PQMs)
            {
                reportPQMList[i] = new ReportPQM();
                reportPQMList[i].Achieved = pqm.Achieved;
                reportPQMList[i].DVHObjective = pqm.DVHObjective;
                reportPQMList[i].Goal = pqm.Goal;
                reportPQMList[i].StructureNameWithCode = pqm.StructureNameWithCode;
                reportPQMList[i].StructVolume = pqm.StructureVolume;
                reportPQMList[i].TemplateId = pqm.TemplateId;
                reportPQMList[i].Variation = pqm.Variation;
                reportPQMList[i].Met = pqm.Met;
                i++;
            }
            reportPQMs.PQMs = reportPQMList;

            var reportData =  new ReportData
            {
                ReportPatient = await _esapiService.GetReportPatientAsync(),
                ReportPlanningItem = new ReportPlanningItem
                {
                    Id = SelectedPlan.Id,
                    Type = SelectedPlan.Type,
                    Created = SelectedPlan.CreationDateTime
                },
                ReportStructureSet = new ReportStructureSet
                {
                    Id = SelectedPlan.StructureSetId,
                    Image = new ReportImage
                    {
                        Id = SelectedPlan.PlanImageId,
                        CreationTime = SelectedPlan.PlanImageCreation
                    }
                },
                ReportPQMs = reportPQMs,
            };

            var path = GetTempPdfPath();
            reportService.Export(path, reportData);

            Process.Start(path);
        }

        private static string GetTempPdfPath()
        {
            return Path.GetTempFileName() + ".pdf";
        }
    }
}
