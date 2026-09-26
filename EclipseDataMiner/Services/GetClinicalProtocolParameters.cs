using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace EclipseDataMiner
{
    class GetClinicalProtocolParameters
    {
        public static string GetParameters(Patient patient, PlanSetup plan)
        {
            string msg = "";

            if (plan.ProtocolID.Count() == 0)
            {
                return msg;
            }
            msg = "ClinicalProtocol:" + plan.ProtocolID.ToString() + ";";
            Course course = plan.Course;

            //get clinical protocol information.
            var prescriptions = new List<ProtocolPhasePrescription>();
            var measures = new List<ProtocolPhaseMeasure>();
            plan.GetProtocolPrescriptionsAndMeasures(ref prescriptions, ref measures);
            foreach (ProtocolPhasePrescription prescription in prescriptions)
            {
                string modifier = "";
                // replace with eclipse UI notation. 
                if (prescription.PrescModifier.ToString() == "PrescriptionModifierAtLeast")
                {
                    modifier = " At least " + prescription.PrescParameter.ToString() + " % recievs more than ";
                }
                else if (prescription.PrescModifier.ToString() == "PrescriptionModifierAtMost")
                {
                    modifier = " At most " + prescription.PrescParameter.ToString() + " % recievs more than ";
                }
                else if (prescription.PrescModifier.ToString() == "PrescriptionModifierMinDose")
                {
                    modifier = " Minimum dose is ";
                }
                else if (prescription.PrescModifier.ToString() == "PrescriptionModifierMaxDose")
                {
                    modifier = " Maximum dose is ";
                }
                else if (prescription.PrescModifier.ToString() == "PrescriptionModifierMeanDose")
                {
                    modifier = " Mean dose is ";
                }
                else
                {
                    modifier = prescription.PrescModifier.ToString();
                }
                msg += "StructureId:" + prescription.StructureId.ToString() + "," +
                    "PrescModifier:" + modifier.ToString() + "," +
                    "TargetFractionDose:" + prescription.TargetFractionDose.ToString() + "," +
                    "TargetTotalDose:" + prescription.TargetTotalDose.ToString() + "," +
                    "ActualTotalDose:" + prescription.ActualTotalDose.ToString() + "," +
                    "TargetIsMet:" + prescription.TargetIsMet.ToString() + ";";
            }
            //set quality indices on each row. 
            foreach (ProtocolPhaseMeasure measure in measures)
            {
                string modifier = "";
                // replace with eclipse UI notation. 
                if (measure.Modifier.ToString() == "MeasureModifierAtMost")
                {
                    modifier = " is less than ";
                }
                else if (measure.Modifier.ToString() == "MeasureModifierAtLeast")
                {
                    modifier = " is more than ";
                }
                else if (measure.Modifier.ToString() == "MeasureModifierTarget")
                {
                    modifier = " is ";
                }
                else
                {
                    modifier = measure.Modifier.ToString();
                }
                msg += "StructureId:" + measure.StructureId.ToString() + "," +
                    "Modifier:" + measure.TypeText.ToString() + modifier.ToString() + "," +
                    "TargetValue:" + measure.TargetValue.ToString() + "," +
                    "ActualValue:" + measure.ActualValue.ToString() + "," +
                    "TargetIsMet:" + measure.TargetIsMet.ToString() + ";";
            }
            return msg;
        }
    }
}
