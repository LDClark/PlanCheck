using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class CollisionCalculator
    {
        static readonly DiffuseMaterial redMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
        static readonly DiffuseMaterial darkBlueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkBlue));
        static readonly DiffuseMaterial greenYellowMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.GreenYellow));
        static readonly DiffuseMaterial lightBlueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));
        static readonly DiffuseMaterial magentaMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Magenta));
        static readonly DiffuseMaterial greenMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
        static readonly DiffuseMaterial darkOrangeMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));

        //linac gantry parameters in mm
        static readonly double distToCollFace = 415.0;  //iso to collimator
        static readonly double collimatorDiameter1 = 470.0;  //gantry face diameter (metal plate)
        static readonly double collimatorDiameter2 = 750.0; //gantry diameter (plastic hub)
        static readonly double collimatorFaceThickness = 27.5;  //metal plate thickness
        static readonly double collimatorTopThickness = 200.0; //gantry plastic hub thickness
        static readonly double distToCollFaceElectron = 50.0;  //5cm from iso to electron cone block
        static readonly double collimatorThicknessElectron = 30.0;  //thickness of electron cone
        static readonly double distToCollFaceSRS = 255.0;  //distance from iso to srs cone
        static readonly double collimatorDiameterSRS = 70.0;  //diameter of srs cone
        static readonly double collimatorThicknessSRS = 100.0;  //length of srs cone

        //warnings in cm
        static readonly double collisionDistance = 3.0;
        static readonly double warningDistance = 4.0;

        public CollisionCheckViewModel CalculateBeamCollision(PlanSetup planSetup, Beam beam)
        {
            var calculator = new CollisionCalculator();
            var modelGroup = new Model3DGroup();
            var isoctr = GetIsocenter(beam);           
            var iso3DMesh = CalculateIsoMesh(isoctr);
            var body = planSetup.StructureSet.Structures.Where(x => x.Id.Contains("BODY")).First();
            Structure couch = null;
            MeshGeometry3D couchMesh = null;
            try
            {
                foreach (Structure structure in planSetup.StructureSet.Structures)
                {
                    if (structure.StructureCodeInfos.FirstOrDefault().Code == "Support")
                    {
                        couch = structure;
                        couchMesh = couch.MeshGeometry;
                    }
                }
            }
            catch
            {
                couch = null;
            }
            var bodyMesh = body.MeshGeometry;
            MeshGeometry3D collimatorMesh = GetCollimatorMesh(beam, isoctr);
            string shortestDistanceBody;
            string shortestDistanceTable;
            string status = "Clear";
            shortestDistanceBody = ShortestDistance(collimatorMesh, bodyMesh);
            if (couch != null)
            {
                shortestDistanceTable = ShortestDistance(collimatorMesh, couchMesh);
            }
            else
            {
                shortestDistanceTable = " - ";
                status = " - ";
            }
            Console.WriteLine(beam.Id + " - gantry to body is " + shortestDistanceBody + " cm");
            Console.WriteLine(beam.Id + " - gantry to table is " + shortestDistanceTable + " cm");
            if (shortestDistanceTable != " - ")
            {
                if ((Convert.ToDouble(shortestDistanceBody) < collisionDistance) || (Convert.ToDouble(shortestDistanceTable) < collisionDistance))
                {
                    status = "Collision";
                }
                if ((Convert.ToDouble(shortestDistanceBody) < warningDistance) || (Convert.ToDouble(shortestDistanceTable) < warningDistance))
                {
                    status = "Warning";
                }
            }
            var collisionSummary = new CollisionCheckViewModel
            {
                View = true,
                FieldID = beam.Id,
                GantryToBodyDistance = shortestDistanceBody + " cm",
                GantryToTableDistance = shortestDistanceTable + " cm",
                Status = status
            };
            return collisionSummary;
        }

        public static Model3DGroup AddFieldMesh(Beam beam, string status)
        {
            var collimatorMaterialStatic = greenMaterial;
            var collimatorMaterialVMAT = greenYellowMaterial;
            if (status == "Collision")
            {
                collimatorMaterialStatic = redMaterial;
                collimatorMaterialVMAT = redMaterial;
            }
            if (status == "Warning")
            {
                collimatorMaterialStatic = darkOrangeMaterial;
                collimatorMaterialVMAT = darkOrangeMaterial;
            }

            var collimatorMaterial = greenMaterial;
            var modelGroup = new Model3DGroup();
            var isoctr = GetIsocenter(beam);
            var iso3DMesh = CalculateIsoMesh(isoctr);
            if (beam.MLCPlanType.ToString() == "VMAT" || beam.Technique.Id.Contains("ARC"))
            {
                collimatorMaterial = collimatorMaterialVMAT;
            }
            if (beam.Technique.ToString().Contains("STATIC"))
            {
                collimatorMaterial = collimatorMaterialStatic;
            }
            MeshGeometry3D collimatorMesh = GetCollimatorMesh(beam, isoctr);
            modelGroup.Children.Add(new GeometryModel3D { Geometry = collimatorMesh, Material = collimatorMaterial, BackMaterial = darkBlueMaterial });
            modelGroup.Children.Add(new GeometryModel3D { Geometry = iso3DMesh, Material = redMaterial, BackMaterial = redMaterial });
            modelGroup.Freeze();
            return modelGroup;
        }

        public static Model3DGroup AddCouchBodyMesh(Structure body, Structure couch)
        {
            var modelGroup = new Model3DGroup();

            if (couch != null)
                modelGroup.Children.Add(new GeometryModel3D { Geometry = couch.MeshGeometry, Material = magentaMaterial, BackMaterial = darkBlueMaterial });
            modelGroup.Children.Add(new GeometryModel3D { Geometry = body.MeshGeometry, Material = lightBlueMaterial, BackMaterial = darkBlueMaterial });
            modelGroup.Freeze();
            return modelGroup;
        }


        public static Point3D GetIsocenter(Beam beam)
        {
            Point3D iso = new Point3D();
            iso.X = beam.IsocenterPosition.x;
            iso.Y = beam.IsocenterPosition.y;
            iso.Z = beam.IsocenterPosition.z;
            return iso;
        }

        public static Point3D GetCameraPosition(Beam beam)
        {
            Point3D cameraPosition = new Point3D();
            cameraPosition.X = beam.IsocenterPosition.x;
            cameraPosition.Y = beam.IsocenterPosition.y;
            cameraPosition.Z = beam.IsocenterPosition.z - 2000;
            return cameraPosition;
        }

        public static MeshGeometry3D GetCollimatorMesh(Beam beam, Point3D iso)
        {
            var meshBuilder = new MeshBuilder(false, false);
            int thetaDiv = 20;
            double tableAngle = beam.ControlPoints.First().PatientSupportAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().PatientSupportAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().PatientSupportAngle - 360.0f);
            double gantryAngle;

            if (beam.MLCPlanType.ToString() == "VMAT" || beam.Technique.Id.Contains("ARC"))
            {
                int i = 0;
                int arcAngleResolution = 0;  //number of degrees to skip
                foreach (ControlPoint cp in beam.ControlPoints)
                {
                    if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f) : (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f);
                    else
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * cp.GantryAngle : (Math.PI / 180.0f) * (cp.GantryAngle - 360.0f);
                    i++;
                    if ((cp.Index == beam.ControlPoints.First().Index) ||
                        (beam.GantryDirection.ToString() == "Clockwise" && cp.Index == (beam.ControlPoints.First().Index + arcAngleResolution)) ||
                        (beam.GantryDirection.ToString() == "CounterClockwise" && cp.Index == (beam.ControlPoints.First().Index + arcAngleResolution)) ||
                        (cp.Index == beam.ControlPoints.Last().Index))
                    {
                        AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorFaceThickness, iso, collimatorDiameter1);
                        AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorTopThickness, iso, collimatorDiameter2);
                    }
                        
                    if (i > arcAngleResolution)
                        arcAngleResolution = arcAngleResolution + 20;
                }
            }
            if (beam.Technique.ToString().Contains("STATIC"))
            {
                if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0 ? (Math.PI / 180.0) * (beam.ControlPoints.First().GantryAngle - 180.0) : (Math.PI / 180.0) * (beam.ControlPoints.First().GantryAngle - 180.0);
                else
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0 ? (Math.PI / 180.0) * beam.ControlPoints.First().GantryAngle : (Math.PI / 180.0) * (beam.ControlPoints.First().GantryAngle - 360.0);
                AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorFaceThickness, iso, collimatorDiameter1);
                AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorTopThickness, iso, collimatorDiameter2);
            }
            if (beam.EnergyModeDisplayName.Contains("E"))
            {
                double fieldSize = Double.Parse(Regex.Match(beam.Applicator.Id, @"\d+").Value);
                double collimaterDiameterElectron = fieldSize * 10.0 + 30.0; //convert to mm and add 30 mm safety margin

                if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0 ? (Math.PI / 180.0) * (beam.ControlPoints.First().GantryAngle - 180.0) : (Math.PI / 180.0) * (beam.ControlPoints.First().GantryAngle - 180.0);
                else
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0 ? (Math.PI / 180.0) * beam.ControlPoints.First().GantryAngle : (Math.PI / 180.0) * (beam.ControlPoints.First().GantryAngle - 360.0);
                AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFaceElectron, collimatorThicknessElectron, iso, collimaterDiameterElectron);
            }
            if (beam.EnergyModeDisplayName.Contains("SRS"))
            {
                thetaDiv = 10;
                double gantryAngleLast;

                if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")              
                    gantryAngleLast = beam.ControlPoints.Last().GantryAngle <= 180.0 ? (Math.PI / 180.0) * (beam.ControlPoints.Last().GantryAngle - 180.0) : (Math.PI / 180.0) * (beam.ControlPoints.Last().GantryAngle - 180.0);
                else
                    gantryAngleLast = beam.ControlPoints.Last().GantryAngle <= 180.0 ? (Math.PI / 180.0) * beam.ControlPoints.Last().GantryAngle : (Math.PI / 180.0) * (beam.ControlPoints.Last().GantryAngle - 360.0);

                foreach (ControlPoint cp in beam.ControlPoints)
                {
                    if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                        gantryAngle = cp.GantryAngle <= 180.0 ? (Math.PI / 180.0) * (cp.GantryAngle - 180.0) : (Math.PI / 180.0) * (cp.GantryAngle - 180.0);
                    else
                        gantryAngle = cp.GantryAngle <= 180.0 ? (Math.PI / 180.0) * cp.GantryAngle : (Math.PI / 180.0) * (cp.GantryAngle - 360.0);                  
                    if ((cp.Index == beam.ControlPoints.First().Index) ||
                        (cp.Index == beam.ControlPoints.Last().Index))
                    {
                        AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFaceSRS, collimatorThicknessSRS, iso, collimatorDiameterSRS);
                    }
                    if (beam.GantryDirection.ToString() == "Clockwise" && cp.Index == beam.ControlPoints.First().Index)
                        for (double x = gantryAngle; x < gantryAngleLast; x = x + thetaDiv * (Math.PI / 180))
                            AddCylinderToMesh(meshBuilder, x, tableAngle, thetaDiv, distToCollFaceSRS, collimatorThicknessSRS, iso, collimatorDiameterSRS);
                        
                    if (beam.GantryDirection.ToString() == "CounterClockwise" && cp.Index == beam.ControlPoints.First().Index)
                        for (double x = gantryAngle; x > gantryAngleLast; x = x - thetaDiv * (Math.PI / 180))
                            AddCylinderToMesh(meshBuilder, x, tableAngle, thetaDiv, distToCollFaceSRS, collimatorThicknessSRS, iso, collimatorDiameterSRS);
                }
            }
            return meshBuilder.ToMesh(true);
        }

        public static MeshBuilder AddCylinderToMesh(MeshBuilder meshBuilder, double gantryAngle, double tableAngle, int thetaDiv, double distanceFromIso, double cylinderThickness, Point3D iso, double diameter)
        {
            Point3D circleCenter1 = new Point3D();
            Point3D circleCenter2 = new Point3D();

            circleCenter1.X = iso.X + distanceFromIso * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
            circleCenter1.Y = iso.Y - distanceFromIso * Math.Cos(gantryAngle);
            circleCenter1.Z = iso.Z - distanceFromIso * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

            circleCenter2.X = iso.X + (distanceFromIso + cylinderThickness) * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
            circleCenter2.Y = iso.Y - (distanceFromIso + cylinderThickness) * Math.Cos(gantryAngle);
            circleCenter2.Z = iso.Z - (distanceFromIso + cylinderThickness) * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

            meshBuilder.AddCylinder(circleCenter1, circleCenter2, diameter, thetaDiv);
            return meshBuilder;
        }

        public static string ShortestDistance(MeshGeometry3D mesh1, MeshGeometry3D mesh2)
        {
            Point3DCollection meshPositions1 = mesh1.Positions;
            Point3DCollection meshPositions2 = mesh2.Positions;
            double shortestDistance = 2000000;
            foreach (Point3D point1 in meshPositions1)
            {
                foreach (Point3D point2 in meshPositions2)
                {
                    double distance = (Math.Sqrt((Math.Pow((point2.X - point1.X), 2)) + (Math.Pow((point2.Y - point1.Y), 2)) + (Math.Pow((point2.Z - point1.Z), 2)))) / 10;
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                    }
                }
            }
            return shortestDistance.ToString("0.0");
        }

        public static MeshGeometry3D CalculateIsoMesh(Point3D iso)
        {
            var meshBuilder = new MeshBuilder(false, false);
            meshBuilder.AddSphere(iso, 10);

            return meshBuilder.ToMesh(true);
        }
    }
}
