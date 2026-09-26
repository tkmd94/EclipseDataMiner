using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Services;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class PlanComplexityTests
    {
        [TestMethod]
        [Description("Varian High Definition 120 (HD120) のリーフ境界座標生成が幾何学的に正確であることを検証")]
        public void MakeLeafBoundArray_HD120_ShouldGenerateCorrectCoordinates()
        {
            // Act
            bool result = PlanComplexityAnalysis.makeLeafBoundArray("Varian High Definition 120", out double[,] bounds);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(bounds);
            Assert.AreEqual(60, bounds.GetLength(0));
            Assert.AreEqual(2, bounds.GetLength(1));

            // 最下端と最上端の座標検証 (-110 mm から +110 mm, 全長 220 mm)
            Assert.AreEqual(-110.0, bounds[0, 0], 1e-6);
            Assert.AreEqual(110.0, bounds[59, 1], 1e-6);

            // 各リーフの連続性と幅の検証
            for (int i = 0; i < 60; i++)
            {
                // 隣接リーフ境界の連続性
                if (i > 0)
                {
                    Assert.AreEqual(bounds[i - 1, 1], bounds[i, 0], 1e-6, $"Leaf {i} bottom should match Leaf {i - 1} top");
                }

                double width = bounds[i, 1] - bounds[i, 0];
                if (i >= 14 && i <= 45)
                {
                    // 中央 32 枚は 2.5 mm 幅
                    Assert.AreEqual(2.5, width, 1e-6, $"Leaf {i} in HD120 center should be 2.5 mm");
                }
                else
                {
                    // 外側 28 枚は 5.0 mm 幅
                    Assert.AreEqual(5.0, width, 1e-6, $"Leaf {i} in HD120 periphery should be 5.0 mm");
                }
            }
        }

        [TestMethod]
        [Description("Millennium 120 のリーフ境界座標生成が幾何学的に正確であることを検証")]
        public void MakeLeafBoundArray_Millennium120_ShouldGenerateCorrectCoordinates()
        {
            // Act
            bool result = PlanComplexityAnalysis.makeLeafBoundArray("Millennium 120", out double[,] bounds);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(bounds);
            Assert.AreEqual(60, bounds.GetLength(0));
            Assert.AreEqual(2, bounds.GetLength(1));

            // 最下端と最上端の座標検証 (-200 mm から +200 mm, 全長 400 mm)
            Assert.AreEqual(-200.0, bounds[0, 0], 1e-6);
            Assert.AreEqual(200.0, bounds[59, 1], 1e-6);

            // 各リーフの連続性と幅の検証
            for (int i = 0; i < 60; i++)
            {
                if (i > 0)
                {
                    Assert.AreEqual(bounds[i - 1, 1], bounds[i, 0], 1e-6, $"Leaf {i} bottom should match Leaf {i - 1} top");
                }

                double width = bounds[i, 1] - bounds[i, 0];
                if (i >= 10 && i <= 49)
                {
                    // 中央 40 枚は 5.0 mm 幅
                    Assert.AreEqual(5.0, width, 1e-6, $"Leaf {i} in Millennium120 center should be 5.0 mm");
                }
                else
                {
                    // 外側 20 枚は 10.0 mm 幅
                    Assert.AreEqual(10.0, width, 1e-6, $"Leaf {i} in Millennium120 periphery should be 10.0 mm");
                }
            }
        }

        [TestMethod]
        [Description("未知・未対応の MLC モデル名に対して安全に false を返すことを検証")]
        public void MakeLeafBoundArray_WhenUnknownModel_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(PlanComplexityAnalysis.makeLeafBoundArray("UnknownMLC", out var b1));
            Assert.IsFalse(PlanComplexityAnalysis.makeLeafBoundArray("", out var b2));
            Assert.IsFalse(PlanComplexityAnalysis.makeLeafBoundArray("Halcyon DualLayer", out var b3));
        }

        [TestMethod]
        [Description("リーフトラベル計算 (calcLT): 第1コントロールポイントで0、以降のCPで移動距離の絶対値和を検証")]
        public void CalcLT_ShouldCalculateCorrectTravelDistance()
        {
            // Arrange
            float[,] prevLeaf = new float[2, 60];
            float[,] currLeaf = new float[2, 60];

            int leafNo = 25;
            prevLeaf[0, leafNo] = -50.0f; // Bank A (Bank 0)
            prevLeaf[1, leafNo] = 30.0f;  // Bank B (Bank 1)

            currLeaf[0, leafNo] = -45.0f; // Bank A: +5 mm 移動 (| -45 - (-50) | = 5)
            currLeaf[1, leafNo] = 38.0f;  // Bank B: +8 mm 移動 (| 38 - 30 | = 8)

            // Act 1: cpCount == 0 (最初のコントロールポイントは移動距離 0)
            double distCP0 = PlanComplexityAnalysis.calcLT(0, leafNo, currLeaf, prevLeaf);
            Assert.AreEqual(0.0, distCP0, 1e-6);

            // Act 2: cpCount > 0 (移動距離の合計 = 5 + 8 = 13 mm)
            double distCP1 = PlanComplexityAnalysis.calcLT(1, leafNo, currLeaf, prevLeaf);
            Assert.AreEqual(13.0, distCP1, 1e-6);
        }

        [TestMethod]
        [Description("リーフトラベル計算 (calcLT): リーフ移動がない場合（0 mm）にゼロを返すことを検証")]
        public void CalcLT_WhenNoMovement_ShouldReturnZero()
        {
            float[,] prevLeaf = new float[2, 60];
            float[,] currLeaf = new float[2, 60];

            int leafNo = 30;
            prevLeaf[0, leafNo] = -20.0f;
            prevLeaf[1, leafNo] = 20.0f;

            currLeaf[0, leafNo] = -20.0f; // 移動なし
            currLeaf[1, leafNo] = 20.0f;  // 移動なし

            double dist = PlanComplexityAnalysis.calcLT(1, leafNo, currLeaf, prevLeaf);
            Assert.AreEqual(0.0, dist, 1e-6);
        }

        [TestMethod]
        [Description("リーフトラベル計算 (calcLT): 境界リーフインデックス（0番、59番）および逆方向移動の絶対値和を検証")]
        public void CalcLT_BoundaryLeavesAndOppositeMovements_ShouldAccumulateAbsoluteValues()
        {
            float[,] prevLeaf = new float[2, 60];
            float[,] currLeaf = new float[2, 60];

            // リーフ0番（最下端）: Bank A は負方向へ -10mm、Bank B は正方向へ +15mm 移動
            prevLeaf[0, 0] = -30.0f;
            prevLeaf[1, 0] = 10.0f;
            currLeaf[0, 0] = -40.0f; // |-40 - (-30)| = 10
            currLeaf[1, 0] = 25.0f;  // |25 - 10| = 15

            double distLeaf0 = PlanComplexityAnalysis.calcLT(5, 0, currLeaf, prevLeaf);
            Assert.AreEqual(25.0, distLeaf0, 1e-6);

            // リーフ59番（最上端）: 両バンクとも同方向（正方向）に移動
            prevLeaf[0, 59] = -10.0f;
            prevLeaf[1, 59] = 5.0f;
            currLeaf[0, 59] = -2.0f; // |-2 - (-10)| = 8
            currLeaf[1, 59] = 17.0f; // |17 - 5| = 12

            double distLeaf59 = PlanComplexityAnalysis.calcLT(2, 59, currLeaf, prevLeaf);
            Assert.AreEqual(20.0, distLeaf59, 1e-6);
        }

        [TestMethod]
        [Description("Aperture Area Variability (AAV): 均一な矩形開口で理論値 1.0、閉鎖・異常時に 0.0 を検証 (McNiven 2010 / Masi 2013)")]
        public void CalculateAAV_LiteratureValidation()
        {
            // 1. 完全な矩形開口（開口幅 100mm, 10リーフ, スパン 100mm） -> AAV = 1.0
            double aavRect = PlanComplexityAnalysis.CalculateAAV(1000.0, 10, 50.0, -50.0);
            Assert.AreEqual(1.0, aavRect, 1e-6);

            // 2. 開口幅が最大スパンの半分 -> AAV = 0.5
            double aavHalf = PlanComplexityAnalysis.CalculateAAV(500.0, 10, 50.0, -50.0);
            Assert.AreEqual(0.5, aavHalf, 1e-6);

            // 3. ゼロ除算・エッジケースガード（リーフ0枚、スパン0）
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateAAV(100.0, 0, 50.0, -50.0), 1e-6);
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateAAV(0.0, 10, 0.0, 0.0), 1e-6);
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateAAV(100.0, 10, -50.0, 50.0), 1e-6); // 負のスパン
        }

        [TestMethod]
        [Description("Leaf Sequence Variability (LSV): 段差なしで理論値 1.0、変調ステップによる低下を検証 (McNiven 2010 / Masi 2013)")]
        public void CalculateLSV_LiteratureValidation()
        {
            // 1. 全リーフが同位置（posMax == 0, 段差なし） -> LSV = 1.0
            double lsvFlat = PlanComplexityAnalysis.CalculateLSV(30.0, 30.0, 0.0, 10);
            Assert.AreEqual(1.0, lsvFlat, 1e-6);

            // 2. 単一リーフ（N <= 1） -> 変異なしとして LSV = 1.0
            double lsvSingle = PlanComplexityAnalysis.CalculateLSV(40.0, 20.0, 0.0, 1);
            Assert.AreEqual(1.0, lsvSingle, 1e-6);

            // 3. 変調ステップあり: N = 5, posMax = 20 (40 - 20), (N - 1) * 20 = 80, leafSide = 30
            // LSV = (80 - 30) / 80 = 50 / 80 = 0.625
            double lsvModulated = PlanComplexityAnalysis.CalculateLSV(40.0, 20.0, 30.0, 5);
            Assert.AreEqual(0.625, lsvModulated, 1e-6);

            // 4. 極端な段差で leafSide > (N - 1) * posMax の場合も 0.0 未満に沈まないガード
            double lsvClamped = PlanComplexityAnalysis.CalculateLSV(40.0, 20.0, 100.0, 5);
            Assert.AreEqual(0.0, lsvClamped, 1e-6);
        }

        [TestMethod]
        [Description("Modulation Complexity Score (MCS / MCSv): 矩形ビームで 1.0、変調ビームで低下する台形積分を検証 (Masi 2013)")]
        public void CalculateMCS_TrapezoidalIntegrationValidation()
        {
            // 1. 矩形均一ビーム: 全CPで AAV = 1.0, LSV = 1.0 -> MCS = 1.0
            double[] aavRect = new[] { 1.0, 1.0, 1.0 };
            double[] lsvRect = new[] { 1.0, 1.0, 1.0 };
            double[] mw = new[] { 0.0, 0.4, 1.0 };

            double mcsRect = PlanComplexityAnalysis.CalculateMCS(aavRect, lsvRect, mw);
            Assert.AreEqual(1.0, mcsRect, 1e-6);

            // 2. 変調ビーム: CP間で AAV や LSV が変動
            double[] aavMod = new[] { 0.8, 0.6, 0.4 };
            double[] lsvMod = new[] { 0.9, 0.7, 0.5 };
            // Segment 1 (0 -> 0.4): avgAAV = 0.7, avgLSV = 0.8, dWeight = 0.4 -> 0.7 * 0.8 * 0.4 = 0.224
            // Segment 2 (0.4 -> 1.0): avgAAV = 0.5, avgLSV = 0.6, dWeight = 0.6 -> 0.5 * 0.6 * 0.6 = 0.180
            // Total MCS = 0.224 + 0.180 = 0.404
            double mcsMod = PlanComplexityAnalysis.CalculateMCS(aavMod, lsvMod, mw);
            Assert.AreEqual(0.404, mcsMod, 1e-6);

            // 3. エッジケース: CP数不足
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateMCS(new[] { 1.0 }, new[] { 1.0 }, new[] { 0.0 }), 1e-6);
        }

        [TestMethod]
        [Description("Edge Metric (EM): 側面の段差がない場合にゼロ、段差と重み付けに応じた計算を検証 (Younge 2012)")]
        public void CalculateEdgeMetric_LiteratureValidation()
        {
            // 1. 段差なし（EM = 0.0）
            double[] emZero = new[] { 0.0, 0.0, 0.0 };
            double[] mw = new[] { 0.0, 0.5, 1.0 };
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateEdgeMetric(emZero, mw), 1e-6);

            // 2. CP別の重み付け:
            // CP0 weight: 0.5 / 2 = 0.25
            // CP1 weight: ((1.0 - 0.5) + (0.5 - 0.0)) / 2 = 1.0 / 2 = 0.5
            // CP2 weight: (1.0 - 0.5) / 2 = 0.25
            // Sum of weights = 0.25 + 0.5 + 0.25 = 1.0
            double[] emValues = new[] { 2.0, 4.0, 6.0 };
            // Expected: 0.25 * 2.0 + 0.5 * 4.0 + 0.25 * 6.0 = 0.5 + 2.0 + 1.5 = 4.0
            double totalEM = PlanComplexityAnalysis.CalculateEdgeMetric(emValues, mw);
            Assert.AreEqual(4.0, totalEM, 1e-6);
        }

        [TestMethod]
        [Description("Arc Length: 時計回り (CW) および反時計回り (CCW) での 360 度跨ぎ角度計算を検証")]
        public void CalculateArcLength_RotationalGeometryValidation()
        {
            // 時計回り (CW): 181° から 179° は 358°
            Assert.AreEqual(358.0, PlanComplexityAnalysis.CalculateArcLength(181.0, 179.0, "CW"), 1e-6);

            // 反時計回り (CCW): 179° から 181° は 358°
            Assert.AreEqual(358.0, PlanComplexityAnalysis.CalculateArcLength(179.0, 181.0, "CCW"), 1e-6);

            // 部分アーク (CW): 200° から 40° は (40 - 200 + 360) = 200°
            Assert.AreEqual(200.0, PlanComplexityAnalysis.CalculateArcLength(200.0, 40.0, "CW"), 1e-6);

            // 通常アーク (CW): 0° から 180° は 180°
            Assert.AreEqual(180.0, PlanComplexityAnalysis.CalculateArcLength(0.0, 180.0, "CW"), 1e-6);
        }

        [TestMethod]
        [Description("AnalyzeBeam: 矩形固定開口アークにおいて MCS = 1.0, EdgeMetric = 0.0, LeafTravel = 0.0 となる理論値を検証")]
        public void AnalyzeBeam_RectangularFixedField_ShouldMatchTheoreticalBenchmark()
        {
            var beam = new BeamGeometry
            {
                BeamId = "Arc_Bench1",
                MlcModel = "Millennium 120",
                ArcLength = 360.0,
                IsSetupField = false
            };

            // 100mm × 100mm の矩形固定開口（JawY1 = -50, JawY2 = 50）
            // Millennium 120 中央リーフ: Y = -50 から +50 mm に位置するリーフ群（リーフ20〜39）
            for (int cp = 0; cp < 3; cp++)
            {
                var cpGeom = new ControlPointGeometry
                {
                    MetersetWeight = cp * 0.5,
                    JawY1 = -50.0,
                    JawY2 = 50.0
                };

                for (int l = 0; l < 60; l++)
                {
                    // 開口幅 100 mm (Left Bank = -50, Right Bank = +50)
                    cpGeom.LeafPositions[0, l] = -50.0f;
                    cpGeom.LeafPositions[1, l] = 50.0f;
                }
                beam.ControlPoints.Add(cpGeom);
            }

            var result = PlanComplexityAnalysis.AnalyzeBeam(beam);

            Assert.IsNotNull(result);
            Assert.AreEqual("Arc_Bench1", result.BeamId);
            // 矩形開口ではリーフ変調ゼロのため MCS = 1.0 (理論値)
            Assert.AreEqual(1.0, result.ModulationComplexityScore, 1e-3);
            // 段差ゼロのため Edge Metric = 0.0
            Assert.AreEqual(0.0, result.EdgeMetric, 1e-3);
            // リーフ移動なしのため Leaf Travel = 0.0
            Assert.AreEqual(0.0, result.LeafTravel, 1e-3);
            Assert.AreEqual(360.0, result.ArcLength, 1e-3);

            // ToString 形式の検証
            string str = result.ToString();
            Assert.IsTrue(str.StartsWith("(Arc_Bench1:1,0,0,360"));
        }

        [TestMethod]
        [Description("AnalyzeBeam: リーフ変調およびリーフ移動を伴うアークで MCS 低下・EdgeMetric/LeafTravel 増加を検証")]
        public void AnalyzeBeam_ModulatedVmatField_ShouldReflectIncreasedComplexity()
        {
            var beam = new BeamGeometry
            {
                BeamId = "VMAT_Mod",
                MlcModel = "Millennium 120",
                ArcLength = 360.0,
                IsSetupField = false
            };

            // 3つのCPで互い違いのリーフ配置と移動
            for (int cp = 0; cp < 3; cp++)
            {
                var cpGeom = new ControlPointGeometry
                {
                    MetersetWeight = cp * 0.5,
                    JawY1 = -40.0,
                    JawY2 = 40.0
                };

                for (int l = 0; l < 60; l++)
                {
                    // 偶数・奇数リーフで位置をジグザグにし、CPごとに移動
                    float shift = (l % 2 == 0) ? (cp * 5.0f) : (-cp * 5.0f);
                    cpGeom.LeafPositions[0, l] = -30.0f + shift;
                    cpGeom.LeafPositions[1, l] = 30.0f + shift;
                }
                beam.ControlPoints.Add(cpGeom);
            }

            var result = PlanComplexityAnalysis.AnalyzeBeam(beam);

            Assert.IsNotNull(result);
            Assert.AreEqual("VMAT_Mod", result.BeamId);
            // 複雑な変調があるため MCS は 1.0 未満に低下
            Assert.IsTrue(result.ModulationComplexityScore < 1.0, "MCS should be less than 1.0 for modulated field");
            Assert.IsTrue(result.ModulationComplexityScore > 0.0, "MCS should be greater than 0");
            // 側面に多数の段差が存在するため Edge Metric は正値
            Assert.IsTrue(result.EdgeMetric > 0.0, "EdgeMetric should be positive");
            // CP間でリーフが移動したため Leaf Travel は正値
            Assert.IsTrue(result.LeafTravel > 0.0, "LeafTravel should be positive");
        }
    }
}
