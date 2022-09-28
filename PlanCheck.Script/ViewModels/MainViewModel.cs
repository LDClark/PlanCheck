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

        private async void Start()
        {
            DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
            string firstFileName = constraintDir.GetFiles().FirstOrDefault().FullName;
            string firstConstraintFilePath = Path.Combine(constraintDir.ToString(), firstFileName);
            Constraints = new ConstraintListViewModel(constraintDir.ToString()).ConstraintList;
            SelectedConstraint = new ConstraintViewModel(firstConstraintFilePath);
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

            // make sure the workbook template exists
            if (!System.IO.File.Exists(SelectedConstraint.ConstraintPath))
            {
                System.Windows.MessageBox.Show(string.Format("The template file '{0}' chosen does not exist.", SelectedConstraint.ConstraintPath));
            }
            PQMViewModel[] pqms = ObjectiveViewModel.GetObjectives(SelectedConstraint);

            _dialogService.ShowProgressDialog("Calculating dose metrics...", structures.Count(),
                async progress =>
                {
                    PQMs = new ObservableCollection<PQMViewModel>();
                    foreach (var structure in structures)
                    {
                        try
                        {
                            foreach (var pqm in pqms.Where(i => i != null))
                            {
                                string templateSelected = "";
                                if (structure.Id.ToUpper().CompareTo(pqm.TemplateId.ToUpper()) == 0) //id matches
                                {
                                    templateSelected = pqm.TemplateId;
                                }
                                else if (structure.Code != null)
                                {
                                    foreach (var code in pqm.TemplateCodes)
                                        if (code == structure.Code) //code matches
                                        {
                                            templateSelected = code;
                                        }
                                }
                                else
                                {
                                    foreach (string id in pqm.TemplateAliases)
                                    {
                                        if (structure.Id.ToUpper().CompareTo(id.ToUpper()) == 0) //id matches alias
                                        {
                                            templateSelected = id;
                                        }
                                    }
                                }
                                if (templateSelected != structure.Code)
                                {
                                    if (structure.Id.ToUpper().CompareTo(templateSelected.ToUpper()) != 0) //template code or name not found
                                        continue;
                                }
                                else
                                    templateSelected = pqm.TemplateId + " : " + templateSelected; // if template code matches, fill in the PQM id

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
                                    PlanId = SelectedPlan.Id
                                });
                                progress.Increment();
                            }
                        }
                        catch
                        {
                        }
                    }
                });

            _dialogService.ShowProgressDialog("Finding errors...",
                async progress =>
                {
                    ErrorGrid = await _esapiService.GetErrorsAsync(courseId, planId);
                });
        }

        private void UpdatePQM()
        {
            _dialogService.ShowProgressDialog("Recalculating...", PQMs.Count(),
            async progress =>
            {
                foreach (var pqm in PQMs)
                {
                    var achieved = await _esapiService.CalculateMetricDoseAsync(SelectedPlan.CourseId, SelectedPlan.Id, pqm.SelectedStructure.Id, pqm.SelectedStructure.Code, pqm.DVHObjective);
                    pqm.StructureVolume = pqm.SelectedStructure.VolumeValue;
                    pqm.Achieved = achieved;
                    pqm.Met = await _esapiService.EvaluateMetricDoseAsync(pqm.Achieved, pqm.Goal, pqm.Variation);
                    var tuple = PQMColors.GetAchievedRatio(pqm.SelectedStructure, pqm.Goal, pqm.DVHObjective, achieved);
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
                    foreach (var beamId in beamIds)
                    {
                        var collisionSummary = await _esapiService.GetBeamCollisionsAsync(courseId, planId, beamId);
                        CollisionSummaries.Add(collisionSummary);
                        progress.Increment();
                    }
                });

                _dialogService.ShowProgressDialog("Getting camera position...", 1,
                async progress =>
                {
                    CameraPosition = await _esapiService.GetCameraPositionAsync(courseId, planId, beamIds.FirstOrDefault());
                    progress.Increment();
                });

                _dialogService.ShowProgressDialog("Getting isocenter...", 1,
                async progress =>
                {
                    Isoctr = await _esapiService.GetIsocenterAsync(courseId, planId, beamIds.FirstOrDefault());
                    progress.Increment();
                });

                _dialogService.ShowProgressDialog("Adding body and couch...", 1,
                async progress =>
                {
                    CouchBodyModel = new Model3DGroup();
                    var couchBodyModel = await _esapiService.AddCouchBodyAsync(courseId, planId);
                    CouchBodyModel.Children.Add(couchBodyModel);
                    progress.Increment();
                });

                _dialogService.ShowProgressDialog("Adding models...", beamIds.Length,
                    async progress =>
                    {
                        UpDir = new Vector3D(0, -1, 0);
                        LookDir = new Vector3D(0, 0, 1);
                        CollimatorModel = new Model3DGroup();
                        int i = 0;
                        foreach (var beamId in beamIds)
                        {
                            var status = CollisionSummaries[i].Status;
                            var fieldModel = await _esapiService.AddFieldMeshAsync(CollimatorModel, courseId, planId, beamId, status);
                            CollimatorModel.Children.Add(fieldModel);
                            progress.Increment();
                            i++;
                        }
                    });
            }
        }

        private async void PrintPlan()
        {
            var reportService = new ReportPdf();
            var reportPQMs = new ReportPQMs();

            int numPqms = PQMs.Count();
            ReportPQM[] reportPQMList = new ReportPQM[numPqms];
            var reportPQM = new ReportPQM();
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
