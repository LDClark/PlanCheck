# PlanCheck

A version 15.5 read-only plugin script that checks:
Plan DVH metrics including planSums and planSetups, with PDF reports and customizable templates.
Collisions between Body/Support/Gantry (gantry distances were from a Trilogy), and gives approximate distances with 3D, IMRT, VMAT, electron applicators, and ICVI SRS cones.
Hard-coded plan/structure/dose checks.

This project includes code from EsapiEssentials, SimplePdfReport, and DvhSummary from redcurry.

This project includes helper/geometry class code from HelixToolkit.

This project includes code from DVHMetric from Steve Thompson and Tomasz Morgas.

To run:
1. Right click solution > Restore NuGet Packages
2. Set Configuration Manager to x64 for all projects
3. Remove references to VMS.TPS.Common.Model.API and VMS.TPS.Common.Model.Types, and add the local dlls from the C: drive
4. Make sure you are running the correct version of EsapiEssentials in PlanCheck and PlanCheck.Runner projects (15.5 uses 1.9, 15.6 uses 2.0)
5. Right click PlanCheck.Runner project > Set as StartUp Project
6. In Scripts Admin, add EsapiEssentials to approved scripts list


![alt text](https://github.com/LDClark/PlanCheck/blob/master/TestCase.png)
