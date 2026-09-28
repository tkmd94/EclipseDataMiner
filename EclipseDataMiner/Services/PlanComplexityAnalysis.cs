using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// Beam complexity analysis results (MCS, Edge Metric, Leaf Travel, Arc Length) per beam.
    /// </summary>
    public class BeamComplexityResult
    {
        public string BeamId { get; set; } = string.Empty;
        public double ModulationComplexityScore { get; set; }
        public double EdgeMetric { get; set; }
        public double LeafTravel { get; set; }
        public double ArcLength { get; set; }

        public override string ToString()
        {
            return $"({BeamId}:{Math.Round(ModulationComplexityScore, 2)},{Math.Round(EdgeMetric, 2)},{Math.Round(LeafTravel, 1)},{Math.Round(ArcLength, 1)})";
        }
    }

    /// <summary>
    /// ESAPI-independent control point geometry data.
    /// </summary>
    public class ControlPointGeometry
    {
        public double MetersetWeight { get; set; }
        public double JawY1 { get; set; }
        public double JawY2 { get; set; }
        /// <summary>
        /// Leaf position coordinate array [2, 60]
        /// [0, leaf]: Left Bank (Bank B / Bank 0)
        /// [1, leaf]: Right Bank (Bank A / Bank 1)
        /// </summary>
        public float[,] LeafPositions { get; set; } = new float[2, 60];
    }

    /// <summary>
    /// ESAPI-independent beam geometry data.
    /// </summary>
    public class BeamGeometry
    {
        public string BeamId { get; set; } = string.Empty;
        public string MlcModel { get; set; } = "Millennium 120";
        public double ArcLength { get; set; }
        public bool IsSetupField { get; set; }
        public List<ControlPointGeometry> ControlPoints { get; set; } = new List<ControlPointGeometry>();
    }

    /// <summary>
    /// Treatment plan beam delivery complexity analysis engine.
    /// References:
    /// - Modulation Complexity Score (MCS / MCSv):
    ///   - McNiven et al., Med. Phys. 37 (2), 590-601 (2010)
    ///   - Masi et al., Med. Phys. 40 (7), 071718 (2013)
    /// - Edge Metric (EM):
    ///   - Younge et al., Med. Phys. 39 (11), 7160-7170 (2012)
    /// </summary>
    public static class PlanComplexityAnalysis
    {
        public const double C1_EDGEMETRIC = 0.0;
        public const double C2_EDGEMETRIC = 1.0;

        /// <summary>
        /// Analysis entry point for ESAPI PlanSetup.
        /// </summary>
        public static string Process(PlanSetup planSetup)
        {
            if (planSetup == null || planSetup.Beams == null)
            {
                return string.Empty;
            }

            var summaryList = new List<string>();

            foreach (var beam in planSetup.Beams)
            {
                if (beam.IsSetupField || beam.MLC == null || beam.ControlPoints.Count() <= 2)
                {
                    continue;
                }

                var beamGeom = new BeamGeometry
                {
                    BeamId = beam.Id ?? string.Empty,
                    MlcModel = beam.MLC.Model ?? string.Empty,
                    ArcLength = beam.ArcLength,
                    IsSetupField = beam.IsSetupField
                };

                foreach (var cp in beam.ControlPoints)
                {
                    var cpGeom = new ControlPointGeometry
                    {
                        MetersetWeight = cp.MetersetWeight,
                        JawY1 = cp.JawPositions.Y1,
                        JawY2 = cp.JawPositions.Y2,
                        LeafPositions = cp.LeafPositions
                    };
                    beamGeom.ControlPoints.Add(cpGeom);
                }

                var res = AnalyzeBeam(beamGeom);
                if (res != null)
                {
                    summaryList.Add(res.ToString());
                }
            }

            return string.Concat(summaryList);
        }

        /// <summary>
        /// Backward compatibility alias (legacy spelling).
        /// </summary>
        public static string Proccess(PlanSetup planSetup) => Process(planSetup);

        /// <summary>
        /// Computes complexity metrics from ESAPI-independent beam geometry data.
        /// </summary>
        public static BeamComplexityResult AnalyzeBeam(BeamGeometry beam)
        {
            if (beam == null || beam.IsSetupField || beam.ControlPoints == null || beam.ControlPoints.Count <= 2)
            {
                return null;
            }

            if (!makeLeafBoundArray(beam.MlcModel, out var leafBoundArray))
            {
                return null;
            }

            int nCP = beam.ControlPoints.Count;
            double[] metersetWeightCP = new double[nCP];
            for (int i = 0; i < nCP; i++)
            {
                metersetWeightCP[i] = beam.ControlPoints[i].MetersetWeight;
            }

            double[] lsv_CP = new double[nCP];
            double[] aav_CP = new double[nCP];
            double[] edgeMetricPerCP = new double[nCP];

            double leafTravel = 0.0;
            float[,] prevLeaf = new float[2, 60];

            for (int cpIndex = 0; cpIndex < nCP; cpIndex++)
            {
                var cp = beam.ControlPoints[cpIndex];
                var leaf = cp.LeafPositions ?? new float[2, 60];

                double sumLeafSidePerCP = 0.0;
                double sumLeafEndPerCP = 0.0;
                double sumAreaPerCP = 0.0;

                int countLeafInField = 0;
                int prevInFieldLeaf = -1;

                double leafSide_Rb = 0.0;
                double leafSide_Lb = 0.0;
                double openLeafWidth = 0.0;

                double minPos_Rb = double.MaxValue;
                double maxPos_Rb = double.MinValue;
                double minPos_Lb = double.MaxValue;
                double maxPos_Lb = double.MinValue;

                for (int leaf_loop = 0; leaf_loop < 60; leaf_loop++)
                {
                    double leafEdgeD = leafBoundArray[leaf_loop, 0];
                    double leafEdgeU = leafBoundArray[leaf_loop, 1];

                    // Check if leaf is outside jaws
                    if (cp.JawY2 <= leafEdgeD || cp.JawY1 >= leafEdgeU)
                    {
                        continue;
                    }

                    // Geometric calculation of exposed leaf height leafEnd
                    double leafEnd;
                    if (cp.JawY2 < leafEdgeU && cp.JawY1 > leafEdgeD)
                    {
                        leafEnd = cp.JawY2 - cp.JawY1;
                    }
                    else if (cp.JawY2 < leafEdgeU)
                    {
                        leafEnd = cp.JawY2 - leafEdgeD;
                    }
                    else if (cp.JawY1 > leafEdgeD)
                    {
                        leafEnd = leafEdgeU - cp.JawY1;
                    }
                    else
                    {
                        leafEnd = leafEdgeU - leafEdgeD;
                    }

                    if (leafEnd < 0) leafEnd = 0;

                    // Exposed leaf opening width (Right Bank - Left Bank)
                    double leafWidth = leaf[1, leaf_loop] - leaf[0, leaf_loop];
                    if (leafWidth < 0) leafWidth = 0;

                    double leafArea = leafEnd * leafWidth;

                    // Step difference between adjacent in-field leaves
                    double leafSide = 0.0;
                    if (prevInFieldLeaf >= 0)
                    {
                        double diffR = Math.Abs(leaf[1, leaf_loop] - leaf[1, prevInFieldLeaf]);
                        double diffL = Math.Abs(leaf[0, leaf_loop] - leaf[0, prevInFieldLeaf]);
                        leafSide = diffR + diffL;
                        leafSide_Rb += diffR;
                        leafSide_Lb += diffL;
                    }

                    prevInFieldLeaf = leaf_loop;
                    countLeafInField++;
                    openLeafWidth += leafWidth;

                    // Update Min / Max positions
                    if (leaf[1, leaf_loop] < minPos_Rb) minPos_Rb = leaf[1, leaf_loop];
                    if (leaf[1, leaf_loop] > maxPos_Rb) maxPos_Rb = leaf[1, leaf_loop];
                    if (leaf[0, leaf_loop] < minPos_Lb) minPos_Lb = leaf[0, leaf_loop];
                    if (leaf[0, leaf_loop] > maxPos_Lb) maxPos_Lb = leaf[0, leaf_loop];

                    // Accumulate leaf travel
                    leafTravel += calcLT(cpIndex, leaf_loop, leaf, prevLeaf);

                    sumLeafSidePerCP += leafSide;
                    sumLeafEndPerCP += 2.0 * leafEnd;
                    sumAreaPerCP += leafArea;
                }

                // Calculate AAV and LSV per control point
                aav_CP[cpIndex] = CalculateAAV(openLeafWidth, countLeafInField, maxPos_Rb, minPos_Lb);
                double lsv_Rb = CalculateLSV(maxPos_Rb, minPos_Rb, leafSide_Rb, countLeafInField);
                double lsv_Lb = CalculateLSV(maxPos_Lb, minPos_Lb, leafSide_Lb, countLeafInField);
                lsv_CP[cpIndex] = lsv_Rb * lsv_Lb;

                // Edge Metric per control point
                if (sumAreaPerCP > 1e-6)
                {
                    edgeMetricPerCP[cpIndex] = (C1_EDGEMETRIC * sumLeafEndPerCP + C2_EDGEMETRIC * sumLeafSidePerCP) / sumAreaPerCP;
                }
                else
                {
                    edgeMetricPerCP[cpIndex] = 0.0;
                }

                prevLeaf = leaf;
            }

            // Calculate aggregate metrics across the whole beam
            double mcs = CalculateMCS(aav_CP, lsv_CP, metersetWeightCP);
            double edgeMetric = CalculateEdgeMetric(edgeMetricPerCP, metersetWeightCP);

            return new BeamComplexityResult
            {
                BeamId = beam.BeamId,
                ModulationComplexityScore = mcs,
                EdgeMetric = edgeMetric,
                LeafTravel = leafTravel,
                ArcLength = beam.ArcLength
            };
        }

        /// <summary>
        /// Calculates Aperture Area Variability (AAV) per control point.
        /// Reference: McNiven et al. (2010) Eq. 2, Masi et al. (2013)
        /// AAV = sum(openLeafWidth) / (N * (maxPos_Rb - minPos_Lb))
        /// </summary>
        public static double CalculateAAV(double openLeafWidth, int countLeafInField, double maxPosRb, double minPosLb)
        {
            if (countLeafInField <= 0) return 0.0;

            double maxSpan = maxPosRb - minPosLb;
            if (maxSpan <= 1e-6) return 0.0;

            double aav = openLeafWidth / (countLeafInField * maxSpan);
            return Math.Max(0.0, Math.Min(1.0, aav));
        }

        /// <summary>
        /// Calculates Leaf Sequence Variability (LSV) per control point (single bank).
        /// Reference: McNiven et al. (2010) Eq. 1, Masi et al. (2013)
        /// LSV = ((N - 1) * posMax - sumLeafSide) / ((N - 1) * posMax)
        /// Returns 1.0 (no variability) if no leaf steps (posMax == 0) or N <= 1.
        /// </summary>
        public static double CalculateLSV(double maxPos, double minPos, double leafSide, int countLeafInField)
        {
            if (countLeafInField <= 1) return 1.0;

            double posMax = maxPos - minPos;
            if (posMax <= 1e-6) return 1.0;

            double denom = (countLeafInField - 1) * posMax;
            if (denom <= 1e-6) return 1.0;

            double lsv = (denom - leafSide) / denom;
            return Math.Max(0.0, Math.Min(1.0, lsv));
        }

        /// <summary>
        /// Integrates Modulation Complexity Score (MCS / MCSv) across the entire beam using the trapezoidal rule.
        /// Reference: Masi et al. (2013) Eq. 1
        /// MCS = sum_{cp=1}^{n-1} [ ((AAV_{cp} + AAV_{cp-1})/2) * ((LSV_{cp} + LSV_{cp-1})/2) * (MW_{cp} - MW_{cp-1}) ]
        /// </summary>
        public static double CalculateMCS(double[] aav_CP, double[] lsv_CP, double[] metersetWeightCP)
        {
            if (aav_CP == null || lsv_CP == null || metersetWeightCP == null) return 0.0;
            int nCP = Math.Min(aav_CP.Length, Math.Min(lsv_CP.Length, metersetWeightCP.Length));
            if (nCP < 2) return 0.0;

            double mcs = 0.0;
            for (int i = 1; i < nCP; i++)
            {
                double dWeight = metersetWeightCP[i] - metersetWeightCP[i - 1];
                if (dWeight > 0)
                {
                    double avgAav = (aav_CP[i] + aav_CP[i - 1]) / 2.0;
                    double avgLsv = (lsv_CP[i] + lsv_CP[i - 1]) / 2.0;
                    mcs += avgAav * avgLsv * dWeight;
                }
            }

            return Math.Max(0.0, Math.Min(1.0, mcs));
        }

        /// <summary>
        /// Computes Edge Metric (EM) across the beam via weighted sum of control points.
        /// Reference: Younge et al. (2012)
        /// </summary>
        public static double CalculateEdgeMetric(double[] edgeMetricPerCP, double[] metersetWeightCP)
        {
            if (edgeMetricPerCP == null || metersetWeightCP == null) return 0.0;
            int nCP = Math.Min(edgeMetricPerCP.Length, metersetWeightCP.Length);
            if (nCP == 0) return 0.0;
            if (nCP == 1) return edgeMetricPerCP[0];

            double totalEdgeMetric = 0.0;
            for (int cp = 0; cp < nCP; cp++)
            {
                double weight;
                if (cp == 0)
                {
                    weight = metersetWeightCP[1] / 2.0;
                }
                else if (cp == nCP - 1)
                {
                    weight = (metersetWeightCP[cp] - metersetWeightCP[cp - 1]) / 2.0;
                }
                else
                {
                    weight = ((metersetWeightCP[cp + 1] - metersetWeightCP[cp]) + (metersetWeightCP[cp] - metersetWeightCP[cp - 1])) / 2.0;
                }

                totalEdgeMetric += weight * edgeMetricPerCP[cp];
            }

            return totalEdgeMetric;
        }

        /// <summary>
        /// Calculates gantry rotation arc angle in degrees.
        /// </summary>
        public static double CalculateArcLength(double gantryStart, double gantryStop, string direction)
        {
            if (string.Equals(direction, "CW", StringComparison.OrdinalIgnoreCase))
            {
                double diff = gantryStop - gantryStart;
                while (diff <= 0) diff += 360.0;
                return diff;
            }
            else if (string.Equals(direction, "CCW", StringComparison.OrdinalIgnoreCase))
            {
                double diff = gantryStart - gantryStop;
                while (diff <= 0) diff += 360.0;
                return diff;
            }
            else
            {
                return Math.Abs(gantryStop - gantryStart);
            }
        }

        /// <summary>
        /// Calculates leaf travel distance (Leaf Travel: LT).
        /// Returns 0 for cpCount == 0 (initial CP), and accumulates absolute displacements for both banks thereafter.
        /// </summary>
        public static double calcLT(int cpCount, int leafNo, float[,] leaf, float[,] prevLeaf)
        {
            if (cpCount <= 0 || leaf == null || prevLeaf == null)
            {
                return 0.0;
            }

            return Math.Abs(leaf[1, leafNo] - prevLeaf[1, leafNo]) + Math.Abs(leaf[0, leafNo] - prevLeaf[0, leafNo]);
        }

        /// <summary>
        /// Generates leaf boundary Y coordinate array [60, 2] corresponding to the MLC model.
        /// </summary>
        public static bool makeLeafBoundArray(string type, out double[,] leafBoundArray)
        {
            leafBoundArray = new double[60, 2];
            if (type == "Varian High Definition 120")
            {
                for (int i = 0; i < 60; i++)
                {
                    if (i >= 14 && i < 46)
                    {
                        leafBoundArray[i, 0] = leafBoundArray[i - 1, 1];
                        leafBoundArray[i, 1] = leafBoundArray[i, 0] + 2.5;
                    }
                    else if (i == 0)
                    {
                        leafBoundArray[i, 0] = -110.0;
                        leafBoundArray[i, 1] = -105.0;
                    }
                    else
                    {
                        leafBoundArray[i, 0] = leafBoundArray[i - 1, 1];
                        leafBoundArray[i, 1] = leafBoundArray[i, 0] + 5.0;
                    }
                }
                return true;
            }
            else if (type == "Millennium 120")
            {
                for (int i = 0; i < 60; i++)
                {
                    if (i >= 10 && i < 50)
                    {
                        leafBoundArray[i, 0] = leafBoundArray[i - 1, 1];
                        leafBoundArray[i, 1] = leafBoundArray[i, 0] + 5.0;
                    }
                    else if (i == 0)
                    {
                        leafBoundArray[i, 0] = -200.0;
                        leafBoundArray[i, 1] = -190.0;
                    }
                    else
                    {
                        leafBoundArray[i, 0] = leafBoundArray[i - 1, 1];
                        leafBoundArray[i, 1] = leafBoundArray[i, 0] + 10.0;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
