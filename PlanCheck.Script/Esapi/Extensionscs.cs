using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

public static class Extensions
{
    public static IEnumerable<PlanningItem> GetPlanSetupsAndSums(this Course course)
    {
        var plans = new List<PlanningItem>();

        if (course.PlanSetups != null)
            plans.AddRange(course.PlanSetups);

        if (course.PlanSums != null)
            plans.AddRange(course.PlanSums);

        return plans;
    }

    public static Course GetCourse(this PlanningItem plan)
    {
        switch (plan)
        {
            case PlanSetup planSetup: return planSetup.Course;
            case PlanSum planSum: return planSum.Course;
        }

        throw new InvalidOperationException("Unknown PlanningItem type.");
    }

    public static PlanningItem GetPlanningItem(Patient patient, string courseId, string planId)
    {
        var course = GetCourse(patient, courseId);
        switch (course.GetPlanSetupsAndSums().FirstOrDefault(x => x.Id == planId))
        {
            case PlanSetup planSetup: return planSetup;
            case PlanSum planSum: return planSum;
        }
        throw new InvalidOperationException("Unknown PlanningItem type.");
    }

    private static Course GetCourse(Patient patient, string courseId) =>
        patient?.Courses?.FirstOrDefault(x => x.Id == courseId);

    public static Structure GetStructure(PlanningItem plan, string structureId) =>
        plan?.StructureSet?.Structures?.FirstOrDefault(x => x.Id == structureId);
}