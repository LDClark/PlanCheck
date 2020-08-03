using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PlanCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
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

        private Plan[] _plans;
        public Plan[] Plans
        {
            get => _plans;
            set => Set(ref _plans, value);
        }

        private Plan _selectedPlan;
        public Plan SelectedPlan
        {
            get => _selectedPlan;
            set => Set(ref _selectedPlan, value);
        }

        private Plan _selectedPlanCompare1;
        public Plan SelectedPlanCompare1
        {
            get => _selectedPlanCompare1;
            set => Set(ref _selectedPlanCompare1, value);
        }

        private Plan _selectedPlanCompare2;
        public Plan SelectedPlanCompare2
        {
            get => _selectedPlanCompare2;
            set => Set(ref _selectedPlanCompare2, value);
        }

        private Plan _selectedPlanCompare3;
        public Plan SelectedPlanCompare3
        {
            get => _selectedPlanCompare3;
            set => Set(ref _selectedPlanCompare3, value);
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
        public ICommand AnalyzePlanCommand => new RelayCommand(AnalyzePlan);
        public ICommand AnalyzeCollisionCommand => new RelayCommand(GetCollisionSummary);

        private async void Start()
        {


            Plans = await _esapiService.GetPlansAsync();

            DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
            string firstFileName = constraintDir.GetFiles().FirstOrDefault().ToString();
            var filenames = constraintDir.GetFiles();
            string firstConstraintFilePath = Path.Combine(constraintDir.ToString(), firstFileName);
            Constraints = ConstraintListViewModel.GetConstraintList(constraintDir.ToString());
            SelectedConstraint = new ConstraintViewModel(firstConstraintFilePath);
        }

        public async void AnalyzePlan()
        {
            var courseId = SelectedPlan?.CourseId;
            var planId = SelectedPlan?.PlanId;

            var type = SelectedPlan?.PlanType;
            if (type == "VMS.TPS.Common.Model.API.PlanSum")
                CCIsEnabled = false;
            if (type == "VMS.TPS.Common.Model.API.ExternalPlanSetup")
                CCIsEnabled = true;

            if (courseId == null || planId == null)
                return;

            var structures = await _esapiService.GetStructuresAsync(courseId, planId);

            CollimatorModel = null;
            CouchBodyModel = null;

            // make sure the workbook template exists
            if (!System.IO.File.Exists(SelectedConstraint.ConstraintPath))
            {
                System.Windows.MessageBox.Show(string.Format("The template file '{0}' chosen does not exist.", SelectedConstraint.ConstraintPath));
            }
            PQMViewModel[] pqms = Objectives.GetObjectives(SelectedConstraint);

            _dialogService.ShowProgressDialog("Calculating dose metrics", structures.Count(),
                async progress =>
                {
                    PQMs = new ObservableCollection<PQMViewModel>();
                    foreach (var structure in structures)
                    {

                        string result = "";
                        string resultCompare1 = "";
                        string resultCompare2 = "";
                        string resultCompare3 = "";
                        string goal = "";
                        string met = "";
                        string variation = "";
                        try
                        {
                            foreach (var pqm in pqms)
                            {
                                if (pqm.TemplateId == structure.StructureName)
                                {
                                    result = "";
                                    resultCompare1 = "";
                                    resultCompare2 = "";
                                    resultCompare3 = "";
                                    goal = pqm.Goal;
                                    variation = pqm.Variation;
                                    result = await _esapiService.CalculateMetricDoseAsync(courseId, planId, structure.StructureName, pqm.TemplateId, pqm.DVHObjective, pqm.Goal, pqm.Variation);
                                    met = await _esapiService.EvaluateMetricDoseAsync(result, goal, variation);

                                    var planCompare1 = SelectedPlanCompare1?.PlanId;
                                    if (planCompare1 != null)
                                        resultCompare1 = await _esapiService.CalculateMetricDoseAsync(courseId, planCompare1, structure.StructureName, pqm.TemplateId, pqm.DVHObjective, pqm.Goal, pqm.Variation);

                                    var planCompare2 = SelectedPlanCompare2?.PlanId;
                                    if (planCompare2 != null)
                                        resultCompare2 = await _esapiService.CalculateMetricDoseAsync(courseId, planCompare2, structure.StructureName, pqm.TemplateId, pqm.DVHObjective, pqm.Goal, pqm.Variation);

                                    var planCompare3 = SelectedPlanCompare3?.PlanId;
                                    if (planCompare3 != null)
                                        resultCompare3 = await _esapiService.CalculateMetricDoseAsync(courseId, planCompare3, structure.StructureName, pqm.TemplateId, pqm.DVHObjective, pqm.Goal, pqm.Variation);

                                    PQMs.Add(new PQMViewModel
                                    {
                                        TemplateId = structure.StructureName,
                                        StructureList = structures,
                                        SelectedStructure = structure,
                                        StructVolume = structure.VolumeValue,
                                        DVHObjective = pqm.DVHObjective,
                                        Goal = goal,
                                        Met = met,
                                        Achieved = result,
                                        ResultCompare1 = resultCompare1,
                                        ResultCompare2 = resultCompare2,
                                        ResultCompare3 = resultCompare3,
                                    });
                                }
                                else
                                {
                                }
                            }
                        }
                        catch
                        {
                            result = "";
                        }
                        progress.Increment();
                    }
                });

            ErrorGrid = await _esapiService.GetErrorsAsync(courseId, planId);
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
            var planId = SelectedPlan?.PlanId;
            var courseId = SelectedPlan?.CourseId;
            var type = SelectedPlan?.PlanType;
            if (type == "VMS.TPS.Common.Model.API.ExternalPlanSetup")
            {
                var beamIds = await _esapiService.GetBeamIdsAsync(courseId, planId);

                _dialogService.ShowProgressDialog("Calculating collisions...", beamIds.Length,
                async progress =>
                {
                    CollisionSummaries = new ObservableCollection<CollisionCheckViewModel>();
                    foreach (var beamId in beamIds)
                    {

                        var collisionSummary = await _esapiService.GetBeamCollisionsAsync(courseId, planId, beamId);
                        CollisionSummaries.Add(collisionSummary.Item1);
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
    }
}
