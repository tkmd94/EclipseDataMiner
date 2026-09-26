using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// ビームごとの照射野複雑度（MCS, Edge Metric, Leaf Travel, Arc Length）解析結果
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
    /// ESAPI 非依存のコントロールポイント幾何データ
    /// </summary>
    public class ControlPointGeometry
    {
        public double MetersetWeight { get; set; }
        public double JawY1 { get; set; }
        public double JawY2 { get; set; }
        /// <summary>
        /// リーフ座標配列 [2, 60]
        /// [0, leaf]: Left Bank (Bank B / Bank 0)
        /// [1, leaf]: Right Bank (Bank A / Bank 1)
        /// </summary>
        public float[,] LeafPositions { get; set; } = new float[2, 60];
    }

    /// <summary>
    /// ESAPI 非依存のビーム幾何データ
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
    /// 治療計画の照射野複雑度解析エンジン
    /// 参考文献:
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
        /// ESAPI PlanSetup に対する解析エントリポイント
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
        /// 後方互換用メソッド名（旧スペル）
        /// </summary>
        public static string Proccess(PlanSetup planSetup) => Process(planSetup);

        /// <summary>
        /// ESAPI 非依存のビーム幾何データから複雑度指標群を網羅的に計算
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

                    // Jaw 外のリーフ判定
                    if (cp.JawY2 <= leafEdgeD || cp.JawY1 >= leafEdgeU)
                    {
                        continue;
                    }

                    // 開口高さ leafEnd の幾何計算
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

                    // リーフ開口幅 (Right Bank - Left Bank)
                    double leafWidth = leaf[1, leaf_loop] - leaf[0, leaf_loop];
                    if (leafWidth < 0) leafWidth = 0;

                    double leafArea = leafEnd * leafWidth;

                    // 隣接照射野内リーフとのステップ差分
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

                    // Min / Max 座標の更新
                    if (leaf[1, leaf_loop] < minPos_Rb) minPos_Rb = leaf[1, leaf_loop];
                    if (leaf[1, leaf_loop] > maxPos_Rb) maxPos_Rb = leaf[1, leaf_loop];
                    if (leaf[0, leaf_loop] < minPos_Lb) minPos_Lb = leaf[0, leaf_loop];
                    if (leaf[0, leaf_loop] > maxPos_Lb) maxPos_Lb = leaf[0, leaf_loop];

                    // リーフトラベル積算
                    leafTravel += calcLT(cpIndex, leaf_loop, leaf, prevLeaf);

                    sumLeafSidePerCP += leafSide;
                    sumLeafEndPerCP += 2.0 * leafEnd;
                    sumAreaPerCP += leafArea;
                }

                // コントロールポイント単位の AAV および LSV 計算
                aav_CP[cpIndex] = CalculateAAV(openLeafWidth, countLeafInField, maxPos_Rb, minPos_Lb);
                double lsv_Rb = CalculateLSV(maxPos_Rb, minPos_Rb, leafSide_Rb, countLeafInField);
                double lsv_Lb = CalculateLSV(maxPos_Lb, minPos_Lb, leafSide_Lb, countLeafInField);
                lsv_CP[cpIndex] = lsv_Rb * lsv_Lb;

                // コントロールポイント単位の Edge Metric
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

            // ビーム全体の統合指標計算
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
        /// コントロールポイントごとの Aperture Area Variability (AAV) を計算
        /// 文献: McNiven et al. (2010) Eq. 2, Masi et al. (2013)
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
        /// コントロールポイントごとの Leaf Sequence Variability (LSV) を計算 (単一バンク)
        /// 文献: McNiven et al. (2010) Eq. 1, Masi et al. (2013)
        /// LSV = ((N - 1) * posMax - sumLeafSide) / ((N - 1) * posMax)
        /// リーフ段差がない場合（posMax == 0）または N <= 1 の場合は 1.0 (変動なし)
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
        /// ビーム全体の Modulation Complexity Score (MCS / MCSv) を台形公式により積分
        /// 文献: Masi et al. (2013) Eq. 1
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
        /// ビーム全体の Edge Metric (EM) を各コントロールポイントの重み付け和により計算
        /// 文献: Younge et al. (2012)
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
        /// ガントリ回転アーク角度の幾何計算（度）
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
        /// リーフ移動距離（Leaf Travel: LT）計算
        /// cpCount == 0（初期コントロールポイント）では 0、以降の CP で両バンクの絶対値移動量を積算
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
        /// MLC モデルに応じたリーフ境界 Y 座標配列 [60, 2] の生成
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
