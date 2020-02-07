using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace EclipseDataMiner
{
    class GetOptimizationSetup
    {
        public static string GetObjectivesParameters(PlanSetup plan)
        {
            string msg = "";
            msg = "optimizationSetup;";

            var optimizationSetup = plan.OptimizationSetup;
            var objectives = optimizationSetup.Objectives;
            var parameters = optimizationSetup.Parameters;
            var useJawTracking = optimizationSetup.UseJawTracking;

            foreach (var parameter in parameters.OfType<OptimizationNormalTissueParameter>())
            {
                msg += parameter.GetType().Name + "," +
                    "DistanceFromTargetBoarderInMM:" + parameter.DistanceFromTargetBorderInMM.ToString() + "," +
                    "EndDosePercentage:" + parameter.EndDosePercentage.ToString() + "," +
                    "FallOff:" + parameter.FallOff.ToString() + "," +
                    "IsAutomatic:" + parameter.IsAutomatic.ToString() + "," +
                    "Priority:" + parameter.Priority.ToString() + "," +
                    "StartDosePercentage:" + parameter.StartDosePercentage.ToString() + ";";
            }

            foreach (var objective in objectives.OfType<OptimizationPointObjective>())
            {

                msg += "Type:" + objective.GetType().Name + "," +
                    "Operator:" + objective.Operator.ToString() + "," +
                    "Dose:" + objective.Dose.ToString() + "," +
                    "Volume:" + objective.Volume.ToString() + "," +
                    "Structure:" + objective.Structure.Id + "," +
                    "Priority:" + objective.Priority.ToString() + ";";
            }
            foreach (var objective in objectives.OfType<OptimizationEUDObjective>())
            {

                msg += "Type:" + objective.GetType().Name + "," +
                    "Operator:" + objective.Operator.ToString() + "," +
                    "Dose:" + objective.Dose.ToString() + "," +
                    "parameter-A:" + objective.ParameterA.ToString() + "," +
                    "Structure:" + objective.Structure.Id + "," +
                    "Priority:" + objective.Priority.ToString() + ";";
            }
            foreach (var objective in objectives.OfType<OptimizationMeanDoseObjective>())
            {

                msg += "Type:" + objective.GetType().Name + "," +
                    "Operator:" + objective.Operator.ToString() + "," +
                    "Dose:" + objective.Dose.ToString() + "," +
                    "Structure:" + objective.Structure.Id + "," +
                    "Priority:" + objective.Priority.ToString() + ";";
            }

            return msg;
        }
    }
}
