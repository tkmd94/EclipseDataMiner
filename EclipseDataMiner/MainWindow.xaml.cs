using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace EclipseDataMiner
{

    public partial class MainWindow : Window
    {

        VMS.TPS.Common.Model.API.Application app = VMS.TPS.Common.Model.API.Application.CreateApplication();

        public string[] filter_PatId;
        public string[] filter_CourseId;
        public string[] fitler_PlanID;
        public PlanSetupApprovalStatus[] approvalStatus;
        public string[] filter_TargetId;
        public double filter_DosePerFraction;
        public Byte filter_NoOfFraction;
        public double filter_TotalDose;
        public string logtext;
        public string currentFolderPath;
        public string outputFolderPath;
        public string outputFilename;
        public StreamWriter reportFile;
        public bool[] checkStatus;
        public ObservableCollection<DQP> DQPList;
        CancellationTokenSource cancelTokensource;
        public MainWindow()
        {
            InitializeComponent();

            // Set initial DQP list
            DQPList = new ObservableCollection<DQP>();
            DQPList.Add(new DQP
            {
                structureName = "*",
                DQPtype = DQPtype.Dose,
                DQPvalue = 95.0,
                InputUnit = IOUnit.Relative,
                OutputUnit = IOUnit.Absolute
            });
            this.dataGrid.ItemsSource = DQPList;

            // Preset checkbox status
            unApprovedCheckBox.IsChecked = true;
            planApprovedCheckBox.IsChecked = true;
            treatAprovedCheckBox.IsChecked = true;

            planApproverCheckBox.IsChecked = false;
            planApprovevalDateCheckBox.IsChecked = false;
            planMuCheckBox.IsChecked = false;
            planEnergyCheckBox.IsChecked = false;
            CMCheckBox.IsChecked = false;
            calculationLogCheckBox.IsChecked = false;
            nModeCheckBox.IsChecked = false;
            cProtocolCheckBox.IsChecked = false;
            dvhDataCheckBox.IsChecked = false;

            planComplexityCheckBox.IsChecked = false;

            approvalStatus = new PlanSetupApprovalStatus[3];
            if (unApprovedCheckBox.IsChecked.Value == true)
            {
                approvalStatus[0] = PlanSetupApprovalStatus.UnApproved;
            }
            else
            {
                approvalStatus[0] = PlanSetupApprovalStatus.Unknown;
            }
            if (planApprovedCheckBox.IsChecked.Value == true)
            {
                approvalStatus[1] = PlanSetupApprovalStatus.PlanningApproved;
            }
            else
            {
                approvalStatus[1] = PlanSetupApprovalStatus.Unknown;
            }
            if (treatAprovedCheckBox.IsChecked.Value == true)
            {
                approvalStatus[2] = PlanSetupApprovalStatus.TreatmentApproved;
            }
            else
            {
                approvalStatus[2] = PlanSetupApprovalStatus.Unknown;
            }

            checkStatus = new bool[10];
            checkStatus[0] = planApproverCheckBox.IsChecked.Value;
            checkStatus[1] = planApprovevalDateCheckBox.IsChecked.Value;
            checkStatus[2] = planMuCheckBox.IsChecked.Value;
            checkStatus[3] = planEnergyCheckBox.IsChecked.Value;
            checkStatus[4] = CMCheckBox.IsChecked.Value;
            checkStatus[5] = calculationLogCheckBox.IsChecked.Value;
            checkStatus[6] = nModeCheckBox.IsChecked.Value;
            checkStatus[7] = cProtocolCheckBox.IsChecked.Value;
            checkStatus[8] = dvhDataCheckBox.IsChecked.Value;

            checkStatus[9] = planComplexityCheckBox.IsChecked.Value;

            outputFolderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\";
            //outputFolderPath = System.Environment.GetEnvironmentVariable("TEMP") + "\\";
            currentFolderPath = outputFolderPath;
            DateTime dt = DateTime.Now;
            string datetext = dt.ToString("yyyyMMddHHmmss");
            outputFilename = "DataMiningOutput."+ datetext + ".txt";
            pathTextBlock.Text = outputFolderPath + outputFilename;
            ShowLogMsg("Default Output path:" + outputFolderPath + outputFilename);

        }

        /// <summary>
        /// cancelButton_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (cancelTokensource != null)
            {
                cancelTokensource.Cancel();
            }
        }

        /// <summary>
        /// DataminingProcess
        /// </summary>
        /// <param name="progress"></param>
        private bool DataminingProcess(IProgress<int> progress, ObservableCollection<DQP> DQPList, CancellationToken cancelToken)
        {
            // iterate over all patients in the database
            int np = app.PatientSummaries.Count();
            this.Dispatcher.Invoke((Action)delegate ()
            {
                ShowLogMsg("Number of Patient in DB:" + np.ToString() + "\n");
            });

            int count = 0;
            foreach (PatientSummary patsum in app.PatientSummaries)
            {
                //check cancel token
                if (cancelToken.IsCancellationRequested == true)
                {
                    return false;
                }

                if (filter_PatId == null)
                {
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        Patient pat = app.OpenPatient(patsum);
                        CheckCourseID(pat, DQPList, reportFile);
                        app.ClosePatient();
                    });
                }
                else
                {
                    foreach (var patId in filter_PatId)
                    {
                        if (patsum.Id.Contains(patId))
                        {
                            this.Dispatcher.Invoke((Action)delegate ()
                            {
                                Patient pat = app.OpenPatient(patsum);
                                CheckCourseID(pat, DQPList, reportFile);
                                app.ClosePatient();
                            });
                        }
                    }
                }
                Thread.Sleep(10);
                int percentage = (int)((double)(count + 1) / (double)np * 100.0);
                progress.Report(percentage);
                count++;
            }
            return true;
        }

        /// <summary>
        /// check search filter
        /// </summary>
        private void FilterCheck()
        {
            //Patient ID filter
            if (patientIdTextBox.Text != "")
            {
                filter_PatId = patientIdTextBox.Text.Split(',');
            }
            else
            {
                filter_PatId = null;
            }

            //Course ID filter
            if (courseIdTextBox.Text != "")
            {
                filter_CourseId = courseIdTextBox.Text.Split(',');
            }
            else
            {
                filter_CourseId = null;
            }

            //Plan ID filter
            if (planIdTextBox.Text != "")
            {
                fitler_PlanID = planIdTextBox.Text.Split(',');
            }
            else
            {
                fitler_PlanID = null;
            }

            //Target Volume ID filter
            if (TargetIdTextBox.Text != "")
            {
                filter_TargetId = TargetIdTextBox.Text.Split(',');
            }
            else
            {
                filter_TargetId = null;
            }

            //Dose per fraction filter
            if (dosePerFractionTextBox.Text != "")
            {
                filter_DosePerFraction = double.Parse(dosePerFractionTextBox.Text);
            }
            else
            {
                filter_DosePerFraction = 0;
            }

            //Number of fraction filter
            if (noOfFractionTextBox.Text != "")
            {
                filter_NoOfFraction = Byte.Parse(noOfFractionTextBox.Text);
            }
            else
            {
                filter_NoOfFraction = 0;
            }

            //Total dose filter
            if (totalDoseTextBox.Text != "")
            {
                filter_TotalDose = double.Parse(totalDoseTextBox.Text);
            }
            else
            {
                filter_TotalDose = 0;
            }
        }
        /// <summary>
        /// runButton_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void runButton_Click(object sender, RoutedEventArgs e)
        {
            //check search filter
            FilterCheck();

            ShowLogMsg("Output path:" + outputFolderPath + outputFilename);

            using (reportFile = new StreamWriter(outputFolderPath + outputFilename, false))
            {
                //Write header line
                string outputHeaderText = "Patient ID" + "\t" +
                             "Course ID" + "\t" +
                             "Date of birth" + "\t" +
                             "Plan ID" + "\t" +
                             "Target volume" + "\t" +
                             "DosePerFraction[Gy/Fr]" + "\t" +
                             "NunberOfFraction" + "\t" +
                             "PrescribedDose[Gy]" + "\t" +
                             "NumberOfBeam" + "\t" +
                             "ApprovalStatus" + "\t";

                if (checkStatus[0] == true)
                {
                    outputHeaderText += "PlanningApprover" + "\t";
                }
                if (checkStatus[1] == true)
                {
                    outputHeaderText += "PlanningApprovalDate" + "\t";
                }
                if (checkStatus[2] == true)
                {
                    outputHeaderText += "MU" + "\t";
                }
                if (checkStatus[3] == true)
                {
                    outputHeaderText += "Energy" + "\t";
                }
                if (checkStatus[4] == true)
                {
                    outputHeaderText += "CalculationModel" + "\t";
                }

                if (checkStatus[5] == true)
                {
                    outputHeaderText += "Calculation Time[sec]" + "\t";
                }
                if (checkStatus[6] == true)
                {
                    outputHeaderText += "Plan normalization mode" + "\t";
                }
                if (checkStatus[7] == true)
                {
                    outputHeaderText += "Clinical Protocol" + "\t";
                }
                if (checkStatus[9] == true)
                {
                    outputHeaderText += "PlanComplexity(Beam ID:MCS,EM[mm-1],LeafTravel[mm],ArcLength[deg])" + "\t";
                }




                var nrow = DQPList.Count();
                if (nrow == 0)
                {
                }
                else
                {
                    IEnumerable<string> list = DQPList.Select(x => x.structureName).Distinct();
                    foreach (var c in list)
                    {
                        outputHeaderText += c + "-Volume[cc]" + "\t";
                        outputHeaderText += c + "-Max dose[Gy]" + "\t";
                        outputHeaderText += c + "-Mean dose[Gy]" + "\t";
                        outputHeaderText += c + "-Min dose[Gy]" + "\t";
                    }

                    foreach (var row in DQPList)
                    {
                        if (row.structureName == null)
                        {
                        }
                        else
                        {
                            if (row.DQPtype == DQPtype.Dose)
                            {
                                outputHeaderText += row.structureName + "-D" + row.DQPvalue.ToString();
                            }
                            else if (row.DQPtype == DQPtype.Volume)
                            {
                                outputHeaderText += row.structureName + "-V" + row.DQPvalue.ToString();
                            }
                            else if (row.DQPtype == DQPtype.DoseComplement)
                            {
                                outputHeaderText += row.structureName + "-DC" + row.DQPvalue.ToString();
                            }
                            else if (row.DQPtype == DQPtype.ComplementVolume)
                            {
                                outputHeaderText += row.structureName + "-CV" + row.DQPvalue.ToString();
                            }

                            if (row.InputUnit == IOUnit.Absolute)
                            {
                                if (row.DQPtype == DQPtype.Dose)
                                {
                                    outputHeaderText += "cc";
                                }
                                else if (row.DQPtype == DQPtype.Volume)
                                {
                                    outputHeaderText += "Gy";
                                }
                                else if (row.DQPtype == DQPtype.DoseComplement)
                                {
                                    outputHeaderText += "cc";
                                }
                                else if (row.DQPtype == DQPtype.ComplementVolume)
                                {
                                    outputHeaderText += "Gy";
                                }
                            }
                            else
                            {
                                outputHeaderText += "%";
                            }

                            if (row.OutputUnit == IOUnit.Absolute)
                            {
                                if (row.DQPtype == DQPtype.Dose)
                                {
                                    outputHeaderText += "[Gy]";
                                }
                                else if (row.DQPtype == DQPtype.Volume)
                                {
                                    outputHeaderText += "[cc]";
                                }
                                else if (row.DQPtype == DQPtype.DoseComplement)
                                {
                                    outputHeaderText += "[Gy]";
                                }
                                else if (row.DQPtype == DQPtype.ComplementVolume)
                                {
                                    outputHeaderText += "[cc]";
                                }
                            }
                            else
                            {
                                outputHeaderText += "[%]";
                            }
                            outputHeaderText += "\t";
                        }
                    }

                }
                reportFile.WriteLine(outputHeaderText);

                runButton.IsEnabled = false;
                cancelButton.IsEnabled = true;
                ProgressTextBlock.Text = "Datamining start. ";
                progressBar.Value = 0;

                Progress<int> progress = new Progress<int>(onProgressChanged);
                // Create token for cancellation
                cancelTokensource = new CancellationTokenSource();
                var cToken = cancelTokensource.Token;

                bool result = await Task.Run(() => DataminingProcess(progress, DQPList, cToken));

                if (result == false)
                {
                    ShowLogMsg("Cancelled.");
                    ProgressTextBlock.Text = "Cancelled";
                }
                else
                {
                    ShowLogMsg("Completed.");
                    ProgressTextBlock.Text = "Completed";
                }


                runButton.IsEnabled = true;
                cancelButton.IsEnabled = false;
                reportFile.Flush();
            }
            ShowLogMsg("Done.");
            System.Diagnostics.Debug.WriteLine("Done.\n");
        }

        /// <summary>
        /// onProgressChanged
        /// </summary>
        /// <param name="percentage"></param>
        private void onProgressChanged(int percentage)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
             {
                 progressBar.Value = percentage;
                 ProgressTextBlock.Text = "Processing... " + percentage.ToString() + " %";
             }));

        }

        /// <summary>
        /// CheckCourseID
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="reportFile"></param>
        private void CheckCourseID(Patient openPat, ObservableCollection<DQP> DQPList, StreamWriter reportFile)
        {
            if (filter_CourseId == null)
            {
                foreach (Course course in openPat.Courses)
                {
                    // System.Diagnostics.Debug.WriteLine("Course filter is not use.\n");
                    CheckPlan(openPat, course, DQPList, reportFile);
                }
            }
            else
            {
                foreach (var fc in filter_CourseId)
                {
                    foreach (Course course in openPat.Courses)
                    {
                        if (course.Id.Contains(fc))
                        {
                            // System.Diagnostics.Debug.WriteLine("Course filter is matched:" + course.Id + "/" + fc + "\n");
                            CheckPlan(openPat, course, DQPList, reportFile);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// CheckPlan
        /// </summary>
        /// <param name="course"></param>
        /// <param name="reportFile"></param>
        private void CheckPlan(Patient openPat, Course course, ObservableCollection<DQP> DQPList, StreamWriter reportFile)
        {

            var approvedPlans = from PlanSetup ps in course.PlanSetups
                                where ((ps.ApprovalStatus == approvalStatus[0]) ||
                                (ps.ApprovalStatus == approvalStatus[1]) ||
                                (ps.ApprovalStatus == approvalStatus[2]))
                                select new
                                {
                                    Plan = ps
                                };

            if (!approvedPlans.Any())
                return;
            foreach (var p in approvedPlans)
            {
                if (fitler_PlanID == null)
                {
                    CheckTargetID(openPat, p.Plan, DQPList, reportFile);
                }
                else
                {
                    foreach (var ft in fitler_PlanID)
                    {
                        if (p.Plan.Id.Contains(ft))
                        {
                            CheckTargetID(openPat, p.Plan, DQPList, reportFile);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// CheckTargetID
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="reportFile"></param>
        private void CheckTargetID(Patient openPat, PlanSetup plan, ObservableCollection<DQP> DQPList, StreamWriter reportFile)
        {
            if (filter_TargetId == null)
            {
                //System.Diagnostics.Debug.WriteLine("Target ID filter is not use.\n");
                checkPrescribedDose(openPat, plan, DQPList, reportFile);
            }
            else
            {
                foreach (var ft in filter_TargetId)
                {
                    if (plan.TargetVolumeID.Contains(ft))
                    {
                        //System.Diagnostics.Debug.WriteLine("Course filter is matched:" + plan.TargetVolumeID + "/" + ft + "\n");
                        checkPrescribedDose(openPat, plan, DQPList, reportFile);
                    }
                }
            }
        }

        /// <summary>
        /// checkPrescribedDose
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="reportFile"></param>
        private void checkPrescribedDose(Patient openPat, PlanSetup plan, ObservableCollection<DQP> DQPList, StreamWriter reportFile)
        {
            if ((filter_DosePerFraction == 0) || (plan.DosePerFraction.Dose == filter_DosePerFraction))
            {
                if ((filter_NoOfFraction == 0) || (plan.NumberOfFractions == filter_NoOfFraction))
                {
                    if ((filter_TotalDose == 0) || (plan.TotalDose.Dose == filter_TotalDose))
                    {
                        ReportOnePlan(openPat, plan, DQPList, reportFile);
                    }
                }
            }
        }

        /// <summary>
        /// Report one-plan
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="ps"></param>
        /// <param name="reportFile"></param>
        private void ReportOnePlan(Patient openPat, PlanSetup ps, ObservableCollection<DQP> DQPList, StreamWriter reportFile)
        {

            ps.DoseValuePresentation = DoseValuePresentation.Absolute;

            string MU = "";
            string Energy = "";
            string calcLogs = "";

            int nBeam = 0;
            int countFieldX = 0;
            int countFieldE = 0;
            foreach (var b in ps.Beams)
            {
                if (!b.IsSetupField)
                {
                    MU += b.Meterset.Value.ToString() + "/";
                    Energy += b.TreatmentUnit.ToString() + ":" + b.EnergyModeDisplayName.ToString() + ":" + b.Technique.ToString() + ":" + b.MLCPlanType.ToString() + "/";
                    if (b.EnergyModeDisplayName.IndexOf("X") >= 0)
                        countFieldX++;
                    if (b.EnergyModeDisplayName.IndexOf("E") >= 0)
                        countFieldE++;

                    nBeam++;
                    calcLogs += ("#B" + nBeam.ToString() + "#;");
                    foreach (var log in b.CalculationLogs)
                    {
                        for (int i = 0; i < log.MessageLines.Count(); i++)
                        {
                            // if (log.MessageLines.ElementAt(i).IndexOf("Calculation took") >= 0)
                            calcLogs += "LOG:" + i.ToString() + log.MessageLines.ElementAt(i) + ";";
                        }
                    }
                }
            }

            string CalculationModel = "";
            if (countFieldX > 0)
            {
                CalculationModel += "(" + ps.PhotonCalculationModel.ToString();
                foreach (KeyValuePair<string, string> kvp in ps.PhotonCalculationOptions)
                {
                    CalculationModel += "/" + kvp.Key + ":" + kvp.Value;
                }
                CalculationModel += ")";
            }
            if (countFieldE > 0)
            {
                CalculationModel += "(" + ps.ElectronCalculationModel.ToString();
                foreach (KeyValuePair<string, string> kvp in ps.ElectronCalculationOptions)
                {
                    CalculationModel += "/" + kvp.Key + ":" + kvp.Value;
                }
                CalculationModel += ")";
            }

            //write plan info. 
            string outputText = openPat.Id + "\t" +
                         ps.Course.Id + "\t" +
                         openPat.DateOfBirth + "\t" +
                         ps.Id + "\t" +
                         ps.TargetVolumeID + "\t" +
                         ps.DosePerFraction.Dose.ToString() + "\t" +
                         ps.NumberOfFractions.ToString() + "\t" +
                         ps.TotalDose.Dose + "\t" +
                         nBeam.ToString() + "\t" +
                         ps.ApprovalStatus.ToString() + "\t";

            if (checkStatus[0] == true)
            {
                outputText += ps.PlanningApprover + "\t";
            }
            if (checkStatus[1] == true)
            {
                outputText += ps.PlanningApprovalDate + "\t";
            }
            if (checkStatus[2] == true)
            {
                outputText += MU.ToString() + "\t";
            }
            if (checkStatus[3] == true)
            {
                outputText += Energy.ToString() + "\t";
            }
            if (checkStatus[4] == true)
            {
                outputText += CalculationModel + "\t";
            }
            if (checkStatus[5] == true)
            {
                outputText += calcLogs + "\t";
            }
            if (checkStatus[6] == true)
            {
                outputText += ps.PlanNormalizationMethod + "\t";
            }
            if (checkStatus[7] == true)
            {
                outputText += GetClinicalProtocolParameters.GetParameters(openPat, ps) + "\t";
            }
            if (checkStatus[9] == true)
            {
                outputText += PlanComplexityAnalysis.Proccess(openPat, ps) + "\t";
            }

            outputText = ReportDVHstatistics(openPat, ps, DQPList, outputText, outputFolderPath, outputFilename, checkStatus[8]);
            reportFile.WriteLine(outputText);
        }

        /// <summary>
        /// Report DVH statistics
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="ps"></param>
        /// <param name="reportFile"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static string ReportDVHstatistics(Patient patient, PlanSetup ps, ObservableCollection<DQP> DQPList, string msg, string outputFolderPath, string outputFilename, bool checkstatus)
        {

            ///////////////////////////////////DVH statistics ////////////////////////////////////////////////////////
            DVHData dvhStat = null;
            Structure targetStructure = null;

            var nrow = DQPList.Count();
            if (nrow == 0)
            {
            }
            else
            {
                IEnumerable<string> list = DQPList.Select(x => x.structureName).Distinct();
                foreach (var c in list)
                {
                    try
                    {
                        targetStructure = ps.StructureSet.Structures.Where(s => s.Id == c).FirstOrDefault();
                    }
                    catch
                    { }
                    if (targetStructure != null && ps.Dose != null)
                    {

                        dvhStat = ps.GetDVHCumulativeData(targetStructure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);

                        if (dvhStat != null)
                        {
                            msg += targetStructure.Volume.ToString() + "\t" +
                                dvhStat.MaxDose.Dose.ToString() + "\t" +
                                dvhStat.MeanDose.Dose.ToString() + "\t" +
                                dvhStat.MinDose.Dose.ToString() + "\t";
                        }
                        else
                        {
                            msg += "NA\tNA\tNA\tNA\t";
                        }
                    }
                    else
                    {
                        msg += "NA\tNA\tNA\tNA\t";
                    }
                }
                foreach (var row in DQPList)
                {
                    if (row.structureName == null)
                    {
                    }
                    else
                    {

                        // DVH statistics //////////////////////////////////
                        try
                        {
                            targetStructure = ps.StructureSet.Structures.Where(s => s.Id == row.structureName).FirstOrDefault();
                        }
                        catch
                        { }


                        if (targetStructure != null && ps.Dose != null)
                        {

                            dvhStat = ps.GetDVHCumulativeData(targetStructure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);

                            if (dvhStat != null)
                            {
                                if (checkstatus)
                                {
                                    string folderpath = outputFolderPath + Path.GetFileNameWithoutExtension(outputFilename);
                                    if (!Directory.Exists(folderpath))
                                    {
                                        Directory.CreateDirectory(folderpath);
                                    }

                                    DVHData dvh = ps.GetDVHCumulativeData(targetStructure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
                                    string filename = string.Format(@"{0}\{1}_{2}_{3}_{4}-dvh.csv",
                                        folderpath, patient.Id, ps.Course.Id, ps.Id, targetStructure.Id);
                                    DumpDVH(filename, dvh);
                                }

                                if (row.DQPtype == DQPtype.Dose)
                                {
                                    DoseValue doseValue = ps.GetDoseAtVolume(targetStructure, row.DQPvalue,
                                        row.InputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3,
                                        row.OutputUnit == IOUnit.Relative ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute);
                                    msg += doseValue.ToString() + "\t";
                                }
                                else if (row.DQPtype == DQPtype.Volume)
                                {
                                    double voluemeValue = ps.GetVolumeAtDose(targetStructure,
                                        row.InputUnit == IOUnit.Relative ?
                                        new DoseValue(ps.TotalDose.Dose * (row.DQPvalue * 0.01), dvhStat.MaxDose.Unit) :
                                        new DoseValue(row.DQPvalue, dvhStat.MaxDose.Unit),
                                         row.OutputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3);
                                    msg += voluemeValue.ToString() + "\t";
                                }
                                else if (row.DQPtype == DQPtype.DoseComplement)
                                {
                                    double subVolume = 0.0;
                                    if (row.InputUnit == IOUnit.Relative)
                                    {
                                        subVolume = targetStructure.Volume - targetStructure.Volume * (row.DQPvalue * 0.01);
                                        subVolume = (subVolume / targetStructure.Volume * 100);
                                    }
                                    else
                                    {
                                        subVolume = targetStructure.Volume - row.DQPvalue;
                                    }
                                    DoseValue doseValue = ps.GetDoseAtVolume(targetStructure, subVolume,
                                        row.InputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3,
                                        row.OutputUnit == IOUnit.Relative ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute);
                                    msg += doseValue.ToString() + "\t";

                                }
                                else if (row.DQPtype == DQPtype.ComplementVolume)
                                {
                                    double voluemeValue = ps.GetVolumeAtDose(targetStructure,
                                       row.InputUnit == IOUnit.Relative ?
                                       new DoseValue(ps.TotalDose.Dose * (row.DQPvalue * 0.01), dvhStat.MaxDose.Unit) :
                                       new DoseValue(row.DQPvalue, dvhStat.MaxDose.Unit),
                                       VolumePresentation.AbsoluteCm3);
                                    double CV = targetStructure.Volume - voluemeValue;
                                    if (row.OutputUnit == IOUnit.Relative)
                                    {
                                        CV = (CV / targetStructure.Volume * 100);
                                    }
                                    msg += CV.ToString() + "\t";
                                }
                                else
                                {
                                    msg += "DQP type not found\t";
                                }
                            }
                            else
                            {
                                msg += "NA\t";
                            }
                        }
                        else
                        {
                            msg += "NA\t";
                        }
                    }
                }
            }
            return msg;
        }

        /// <summary>
        /// DumpDVH
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dvh"></param>
        static void DumpDVH(string filename, DVHData dvh)
        {
            System.IO.StreamWriter dvhFile = new System.IO.StreamWriter(filename);
            // write a header
            dvhFile.WriteLine("Dose,Volume");
            // write all dvh points for the structure.
            foreach (DVHPoint pt in dvh.CurveData)
            {
                string line = string.Format("{0},{1}", pt.DoseValue.Dose, pt.Volume);
                dvhFile.WriteLine(line);
            }
            dvhFile.Close();
        }

        /// <summary>
        /// ShowLogMsg
        /// </summary>
        /// <param name="dataFile"></param>
        private void ShowLogMsg(string text)
        {
            logTextBox.AppendText(text + "\n");
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToEnd();
        }

        /// <summary>
        /// window closing event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            app.ClosePatient();
            app.Dispose();
        }

        /// <summary>
        /// Set output filename and folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            //create instance of SaveFileDialog class
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.FileName = outputFilename;
            sfd.InitialDirectory = outputFolderPath;
            sfd.Filter = "CSV files(*.csv)|*.csv|All files(*.*)|*.*";
            sfd.FilterIndex = 1;
            sfd.Title = "Save as";
            sfd.RestoreDirectory = true;
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;

            Nullable<bool> result = sfd.ShowDialog();
            if (result == true)
            {
                outputFilename = sfd.SafeFileName;
                outputFolderPath = System.IO.Path.GetDirectoryName(sfd.FileName) + @"\";
                currentFolderPath = outputFolderPath;
                pathTextBlock.Text = outputFolderPath + outputFilename;
                ShowLogMsg("change Output path:" + outputFolderPath + outputFilename);

            }
        }

        /// <summary>
        /// Open output folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(outputFolderPath);
        }

        /// <summary>
        /// PlanInfo_Checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlanInfo_Checked(object sender, RoutedEventArgs e)
        {
            if (checkStatus != null)
            {
                CheckBox checkBox = (CheckBox)sender;
                if (checkBox.Name == planApproverCheckBox.Name)
                {
                    checkStatus[0] = true;
                }
                else if (checkBox.Name == planApprovevalDateCheckBox.Name)
                {
                    checkStatus[1] = true;
                }
                else if (checkBox.Name == planMuCheckBox.Name)
                {
                    checkStatus[2] = true;
                }
                else if (checkBox.Name == planEnergyCheckBox.Name)
                {
                    checkStatus[3] = true;
                }
                else if (checkBox.Name == CMCheckBox.Name)
                {
                    checkStatus[4] = true;
                }
                else if (checkBox.Name == calculationLogCheckBox.Name)
                {
                    checkStatus[5] = true;
                }
                else if (checkBox.Name == nModeCheckBox.Name)
                {
                    checkStatus[6] = true;
                }
                else if (checkBox.Name == cProtocolCheckBox.Name)
                {
                    checkStatus[7] = true;
                }
                else if (checkBox.Name == dvhDataCheckBox.Name)
                {
                    checkStatus[8] = true;
                }
                else if (checkBox.Name == planComplexityCheckBox.Name)
                {
                    checkStatus[9] = true;
                }
                else if (checkBox.Name == unApprovedCheckBox.Name)
                {
                    approvalStatus[0] = PlanSetupApprovalStatus.UnApproved;
                }
                else if (checkBox.Name == planApprovedCheckBox.Name)
                {
                    approvalStatus[1] = PlanSetupApprovalStatus.PlanningApproved;
                }
                else if (checkBox.Name == treatAprovedCheckBox.Name)
                {
                    approvalStatus[2] = PlanSetupApprovalStatus.TreatmentApproved;
                }
            }
        }

        /// <summary>
        /// PlanInfo_Unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlanInfo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (checkStatus != null)
            {
                CheckBox checkBox = (CheckBox)sender;
                if (checkBox.Name == planApproverCheckBox.Name)
                {
                    checkStatus[0] = false;
                }
                else if (checkBox.Name == planApprovevalDateCheckBox.Name)
                {
                    checkStatus[1] = false;
                }
                else if (checkBox.Name == planMuCheckBox.Name)
                {
                    checkStatus[2] = false;
                }
                else if (checkBox.Name == planEnergyCheckBox.Name)
                {
                    checkStatus[3] = false;
                }
                else if (checkBox.Name == CMCheckBox.Name)
                {
                    checkStatus[4] = false;
                }
                else if (checkBox.Name == calculationLogCheckBox.Name)
                {
                    checkStatus[5] = false;
                }
                else if (checkBox.Name == nModeCheckBox.Name)
                {
                    checkStatus[6] = false;
                }
                else if (checkBox.Name == cProtocolCheckBox.Name)
                {
                    checkStatus[7] = false;
                }
                else if (checkBox.Name == dvhDataCheckBox.Name)
                {
                    checkStatus[8] = false;
                }
                else if (checkBox.Name == planComplexityCheckBox.Name)
                {
                    checkStatus[9] = false;
                }
                else if (checkBox.Name == unApprovedCheckBox.Name)
                {
                    approvalStatus[0] = PlanSetupApprovalStatus.Unknown;
                }
                else if (checkBox.Name == planApprovedCheckBox.Name)
                {
                    approvalStatus[1] = PlanSetupApprovalStatus.Unknown;
                }
                else if (checkBox.Name == treatAprovedCheckBox.Name)
                {
                    approvalStatus[2] = PlanSetupApprovalStatus.Unknown;
                }

            }
        }

        /// <summary>
        ///  Load DQP file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadParaButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = currentFolderPath;
            ofd.Filter = "CSV files(*.csv)|*.csv|All files(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "Open";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.RestoreDirectory = true;

            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                using (var sr = new StreamReader(ofd.FileName))
                {
                    DQPList = new ObservableCollection<DQP>();
                    int count = 0;
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (count > 0)
                        {
                            var values = line.Split(',');
                            if (values.Count() == 5)
                            {
                                DQPtype DQPtypeIndex = DQPtype.Dose;
                                if (values[1] == "Dose")
                                {
                                    DQPtypeIndex = DQPtype.Dose;
                                }
                                else if (values[1] == "Volume")
                                {
                                    DQPtypeIndex = DQPtype.Volume;
                                }
                                else if (values[1] == "DoseComplement")
                                {
                                    DQPtypeIndex = DQPtype.DoseComplement;
                                }
                                else if (values[1] == "ComplementVolume")
                                {
                                    DQPtypeIndex = DQPtype.ComplementVolume;
                                }
                                IOUnit InputUnit = IOUnit.Absolute;
                                if (values[3] == "Absolute")
                                {
                                    InputUnit = IOUnit.Absolute;
                                }
                                else if (values[3] == "Relative")
                                {
                                    InputUnit = IOUnit.Relative;
                                }
                                IOUnit OutputUnit = IOUnit.Absolute;
                                if (values[4] == "Absolute")
                                {
                                    OutputUnit = IOUnit.Absolute;
                                }
                                else if (values[4] == "Relative")
                                {
                                    OutputUnit = IOUnit.Relative;
                                }

                                DQPList.Add(new DQP
                                {
                                    structureName = values[0],
                                    DQPtype = DQPtypeIndex,
                                    DQPvalue = double.Parse(values[2]),
                                    InputUnit = InputUnit,
                                    OutputUnit = OutputUnit
                                });
                                this.dataGrid.ItemsSource = DQPList;
                            }
                        }
                        count++;
                    }
                }
                currentFolderPath = System.IO.Path.GetDirectoryName(ofd.FileName);
                ShowLogMsg("Load DQP file:" + ofd.FileName);
            }
        }
        /// <summary>
        /// Save DQP file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveParaButton_Click(object sender, RoutedEventArgs e)
        {
            var nrow = DQPList.Count();
            if (nrow == 0)
            {
                ShowLogMsg("No Dose Quality Parameters.");
                MessageBox.Show("No Dose Quality Parameters.");
            }
            else
            {
                //Create instance of SaveFileDialog class
                SaveFileDialog sfd = new SaveFileDialog();

                //set default filename
                sfd.FileName = "DQPlist.csv";
                sfd.InitialDirectory = currentFolderPath;
                sfd.Filter = "CSV files(*.csv)|*.csv|All files(*.*)|*.*";
                sfd.FilterIndex = 1;
                sfd.Title = "Save as";
                sfd.RestoreDirectory = true;
                sfd.OverwritePrompt = true;
                sfd.CheckPathExists = true;

                Nullable<bool> result = sfd.ShowDialog();
                if (result == true)
                {
                    string filepath = "";
                    using (StreamWriter saveFile = new StreamWriter(sfd.FileName, false))
                    {
                        filepath = sfd.FileName;
                        string headerText = "structureName" + ",";
                        headerText += "DQPtype" + ",";
                        headerText += "DQPvalue" + ",";
                        headerText += "InputUnit" + ",";
                        headerText += "OutputUnit";
                        saveFile.WriteLine(headerText);
                        foreach (var row in DQPList)
                        {
                            string outputText = "";
                            if (row.structureName == null)
                            {
                                outputText += ",";
                            }
                            else
                            {
                                outputText += row.structureName.ToString() + ",";
                            }

                            outputText += row.DQPtype.ToString() + ",";
                            outputText += row.DQPvalue.ToString() + ",";
                            outputText += row.InputUnit.ToString() + ",";
                            outputText += row.OutputUnit.ToString();
                            saveFile.WriteLine(outputText);
                        }
                        saveFile.Flush();
                        currentFolderPath = System.IO.Path.GetDirectoryName(sfd.FileName);
                    }
                    MessageBox.Show("Save Dose Quality Parameters.\n" + "Path:" + filepath);
                    ShowLogMsg("Save DQP file:" + sfd.FileName);
                }
            }
        }
    }
}
