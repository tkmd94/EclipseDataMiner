using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Services;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class PlanComplexityTests
    {
        [TestMethod]
        [Description("Verifies that leaf boundary coordinates generated for Varian High Definition 120 (HD120) are geometrically accurate")]
        public void MakeLeafBoundArray_HD120_ShouldGenerateCorrectCoordinates()
        {
            // Act
            bool result = PlanComplexityAnalysis.makeLeafBoundArray("Varian High Definition 120", out double[,] bounds);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(bounds);
            Assert.AreEqual(60, bounds.GetLength(0));
            Assert.AreEqual(2, bounds.GetLength(1));

            // Verify bottom-most and top-most coordinates (-110 mm to +110 mm, total length 220 mm)
            Assert.AreEqual(-110.0, bounds[0, 0], 1e-6);
            Assert.AreEqual(110.0, bounds[59, 1], 1e-6);

            // Verify continuity and width of each leaf
            for (int i = 0; i < 60; i++)
            {
                // Continuity between adjacent leaf boundaries
                if (i > 0)
                {
                    Assert.AreEqual(bounds[i - 1, 1], bounds[i, 0], 1e-6, $"Leaf {i} bottom should match Leaf {i - 1} top");
                }

                double width = bounds[i, 1] - bounds[i, 0];
                if (i >= 14 && i <= 45)
                {
                    // Central 32 leaves are 2.5 mm wide
                    Assert.AreEqual(2.5, width, 1e-6, $"Leaf {i} in HD120 center should be 2.5 mm");
                }
                else
                {
                    // Outer 28 leaves are 5.0 mm wide
                    Assert.AreEqual(5.0, width, 1e-6, $"Leaf {i} in HD120 periphery should be 5.0 mm");
                }
            }
        }

        [TestMethod]
        [Description("Verifies that leaf boundary coordinates generated for Millennium 120 are geometrically accurate")]
        public void MakeLeafBoundArray_Millennium120_ShouldGenerateCorrectCoordinates()
        {
            // Act
            bool result = PlanComplexityAnalysis.makeLeafBoundArray("Millennium 120", out double[,] bounds);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(bounds);
            Assert.AreEqual(60, bounds.GetLength(0));
            Assert.AreEqual(2, bounds.GetLength(1));

            // Verify bottom-most and top-most coordinates (-200 mm to +200 mm, total length 400 mm)
            Assert.AreEqual(-200.0, bounds[0, 0], 1e-6);
            Assert.AreEqual(200.0, bounds[59, 1], 1e-6);

            // Verify continuity and width of each leaf
            for (int i = 0; i < 60; i++)
            {
                if (i > 0)
                {
                    Assert.AreEqual(bounds[i - 1, 1], bounds[i, 0], 1e-6, $"Leaf {i} bottom should match Leaf {i - 1} top");
                }

                double width = bounds[i, 1] - bounds[i, 0];
                if (i >= 10 && i <= 49)
                {
                    // Central 40 leaves are 5.0 mm wide
                    Assert.AreEqual(5.0, width, 1e-6, $"Leaf {i} in Millennium120 center should be 5.0 mm");
                }
                else
                {
                    // Outer 20 leaves are 10.0 mm wide
                    Assert.AreEqual(10.0, width, 1e-6, $"Leaf {i} in Millennium120 periphery should be 10.0 mm");
                }
            }
        }

        [TestMethod]
        [Description("Verifies that makeLeafBoundArray safely returns false for unknown/unsupported MLC models")]
        public void MakeLeafBoundArray_WhenUnknownModel_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(PlanComplexityAnalysis.makeLeafBoundArray("UnknownMLC", out var b1));
            Assert.IsFalse(PlanComplexityAnalysis.makeLeafBoundArray("", out var b2));
            Assert.IsFalse(PlanComplexityAnalysis.makeLeafBoundArray("Halcyon DualLayer", out var b3));
        }

        [TestMethod]
        [Description("Leaf travel calculation (calcLT): Verifies 0 for first CP and sum of absolute leaf displacements for subsequent CPs")]
        public void CalcLT_ShouldCalculateCorrectTravelDistance()
        {
            // Arrange
            float[,] prevLeaf = new float[2, 60];
            float[,] currLeaf = new float[2, 60];

            int leafNo = 25;
            prevLeaf[0, leafNo] = -50.0f; // Bank A (Bank 0)
            prevLeaf[1, leafNo] = 30.0f;  // Bank B (Bank 1)

            currLeaf[0, leafNo] = -45.0f; // Bank A: +5 mm displacement (| -45 - (-50) | = 5)
            currLeaf[1, leafNo] = 38.0f;  // Bank B: +8 mm displacement (| 38 - 30 | = 8)

            // Act 1: cpCount == 0 (travel distance is 0 for first control point)
            double distCP0 = PlanComplexityAnalysis.calcLT(0, leafNo, currLeaf, prevLeaf);
            Assert.AreEqual(0.0, distCP0, 1e-6);

            // Act 2: cpCount > 0 (sum of displacement = 5 + 8 = 13 mm)
            double distCP1 = PlanComplexityAnalysis.calcLT(1, leafNo, currLeaf, prevLeaf);
            Assert.AreEqual(13.0, distCP1, 1e-6);
        }

        [TestMethod]
        [Description("Leaf travel calculation (calcLT): Verifies that 0 is returned when there is no leaf motion (0 mm)")]
        public void CalcLT_WhenNoMovement_ShouldReturnZero()
        {
            float[,] prevLeaf = new float[2, 60];
            float[,] currLeaf = new float[2, 60];

            int leafNo = 30;
            prevLeaf[0, leafNo] = -20.0f;
            prevLeaf[1, leafNo] = 20.0f;

            currLeaf[0, leafNo] = -20.0f; // No movement
            currLeaf[1, leafNo] = 20.0f;  // No movement

            double dist = PlanComplexityAnalysis.calcLT(1, leafNo, currLeaf, prevLeaf);
            Assert.AreEqual(0.0, dist, 1e-6);
        }

        [TestMethod]
        [Description("Leaf travel calculation (calcLT): Verifies boundary leaf indices (0, 59) and sum of absolute values in opposite directions")]
        public void CalcLT_BoundaryLeavesAndOppositeMovements_ShouldAccumulateAbsoluteValues()
        {
            float[,] prevLeaf = new float[2, 60];
            float[,] currLeaf = new float[2, 60];

            // Leaf 0 (bottom-most): Bank A moves negative by -10mm, Bank B moves positive by +15mm
            prevLeaf[0, 0] = -30.0f;
            prevLeaf[1, 0] = 10.0f;
            currLeaf[0, 0] = -40.0f; // |-40 - (-30)| = 10
            currLeaf[1, 0] = 25.0f;  // |25 - 10| = 15

            double distLeaf0 = PlanComplexityAnalysis.calcLT(5, 0, currLeaf, prevLeaf);
            Assert.AreEqual(25.0, distLeaf0, 1e-6);

            // Leaf 59 (top-most): Both banks move in the same direction (positive)
            prevLeaf[0, 59] = -10.0f;
            prevLeaf[1, 59] = 5.0f;
            currLeaf[0, 59] = -2.0f; // |-2 - (-10)| = 8
            currLeaf[1, 59] = 17.0f; // |17 - 5| = 12

            double distLeaf59 = PlanComplexityAnalysis.calcLT(2, 59, currLeaf, prevLeaf);
            Assert.AreEqual(20.0, distLeaf59, 1e-6);
        }

        [TestMethod]
        [Description("Aperture Area Variability (AAV): Verifies theoretical 1.0 for uniform rectangular apertures, and 0.0 for closed/edge cases (McNiven 2010 / Masi 2013)")]
        public void CalculateAAV_LiteratureValidation()
        {
            // 1. Perfectly rectangular aperture (100mm width, 10 leaves, 100mm span) -> AAV = 1.0
            double aavRect = PlanComplexityAnalysis.CalculateAAV(1000.0, 10, 50.0, -50.0);
            Assert.AreEqual(1.0, aavRect, 1e-6);

            // 2. Aperture width is half the maximum span -> AAV = 0.5
            double aavHalf = PlanComplexityAnalysis.CalculateAAV(500.0, 10, 50.0, -50.0);
            Assert.AreEqual(0.5, aavHalf, 1e-6);

            // 3. Division by zero / edge case guards (0 leaves, 0 span)
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateAAV(100.0, 0, 50.0, -50.0), 1e-6);
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateAAV(0.0, 10, 0.0, 0.0), 1e-6);
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateAAV(100.0, 10, -50.0, 50.0), 1e-6); // Negative span
        }

        [TestMethod]
        [Description("Leaf Sequence Variability (LSV): Verifies theoretical 1.0 without leaf steps, and reduction with modulation steps (McNiven 2010 / Masi 2013)")]
        public void CalculateLSV_LiteratureValidation()
        {
            // 1. All leaves at identical position (posMax == 0, no steps) -> LSV = 1.0
            double lsvFlat = PlanComplexityAnalysis.CalculateLSV(30.0, 30.0, 0.0, 10);
            Assert.AreEqual(1.0, lsvFlat, 1e-6);

            // 2. Single leaf (N <= 1) -> LSV = 1.0 (no variability)
            double lsvSingle = PlanComplexityAnalysis.CalculateLSV(40.0, 20.0, 0.0, 1);
            Assert.AreEqual(1.0, lsvSingle, 1e-6);

            // 3. With modulation steps: N = 5, posMax = 20 (40 - 20), (N - 1) * 20 = 80, leafSide = 30
            // LSV = (80 - 30) / 80 = 50 / 80 = 0.625
            double lsvModulated = PlanComplexityAnalysis.CalculateLSV(40.0, 20.0, 30.0, 5);
            Assert.AreEqual(0.625, lsvModulated, 1e-6);

            // 4. Guard against negative value when extreme step causes leafSide > (N - 1) * posMax
            double lsvClamped = PlanComplexityAnalysis.CalculateLSV(40.0, 20.0, 100.0, 5);
            Assert.AreEqual(0.0, lsvClamped, 1e-6);
        }

        [TestMethod]
        [Description("Modulation Complexity Score (MCS / MCSv): Verifies 1.0 for rectangular beams and trapezoidal integration decrease for modulated beams (Masi 2013)")]
        public void CalculateMCS_TrapezoidalIntegrationValidation()
        {
            // 1. Uniform rectangular beam: AAV = 1.0, LSV = 1.0 across all CPs -> MCS = 1.0
            double[] aavRect = new[] { 1.0, 1.0, 1.0 };
            double[] lsvRect = new[] { 1.0, 1.0, 1.0 };
            double[] mw = new[] { 0.0, 0.4, 1.0 };

            double mcsRect = PlanComplexityAnalysis.CalculateMCS(aavRect, lsvRect, mw);
            Assert.AreEqual(1.0, mcsRect, 1e-6);

            // 2. Modulated beam: AAV and LSV vary across CPs
            double[] aavMod = new[] { 0.8, 0.6, 0.4 };
            double[] lsvMod = new[] { 0.9, 0.7, 0.5 };
            // Segment 1 (0 -> 0.4): avgAAV = 0.7, avgLSV = 0.8, dWeight = 0.4 -> 0.7 * 0.8 * 0.4 = 0.224
            // Segment 2 (0.4 -> 1.0): avgAAV = 0.5, avgLSV = 0.6, dWeight = 0.6 -> 0.5 * 0.6 * 0.6 = 0.180
            // Total MCS = 0.224 + 0.180 = 0.404
            double mcsMod = PlanComplexityAnalysis.CalculateMCS(aavMod, lsvMod, mw);
            Assert.AreEqual(0.404, mcsMod, 1e-6);

            // 3. Edge case: insufficient CPs
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateMCS(new[] { 1.0 }, new[] { 1.0 }, new[] { 0.0 }), 1e-6);
        }

        [TestMethod]
        [Description("Edge Metric (EM): Verifies zero when lateral steps are absent, and calculations based on steps and segment weighting (Younge 2012)")]
        public void CalculateEdgeMetric_LiteratureValidation()
        {
            // 1. No steps (EM = 0.0)
            double[] emZero = new[] { 0.0, 0.0, 0.0 };
            double[] mw = new[] { 0.0, 0.5, 1.0 };
            Assert.AreEqual(0.0, PlanComplexityAnalysis.CalculateEdgeMetric(emZero, mw), 1e-6);

            // 2. CP-specific weighting:
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
        [Description("Arc Length: Verifies 360-degree crossing angle calculations for clockwise (CW) and counter-clockwise (CCW) arcs")]
        public void CalculateArcLength_RotationalGeometryValidation()
        {
            // Clockwise (CW): 181° to 179° is 358°
            Assert.AreEqual(358.0, PlanComplexityAnalysis.CalculateArcLength(181.0, 179.0, "CW"), 1e-6);

            // Counter-clockwise (CCW): 179° to 181° is 358°
            Assert.AreEqual(358.0, PlanComplexityAnalysis.CalculateArcLength(179.0, 181.0, "CCW"), 1e-6);

            // Partial arc (CW): 200° to 40° is (40 - 200 + 360) = 200°
            Assert.AreEqual(200.0, PlanComplexityAnalysis.CalculateArcLength(200.0, 40.0, "CW"), 1e-6);

            // Normal arc (CW): 0° to 180° is 180°
            Assert.AreEqual(180.0, PlanComplexityAnalysis.CalculateArcLength(0.0, 180.0, "CW"), 1e-6);
        }

        [TestMethod]
        [Description("AnalyzeBeam: Verifies theoretical benchmark of MCS = 1.0, EdgeMetric = 0.0, LeafTravel = 0.0 for rectangular static aperture arc")]
        public void AnalyzeBeam_RectangularFixedField_ShouldMatchTheoreticalBenchmark()
        {
            var beam = new BeamGeometry
            {
                BeamId = "Arc_Bench1",
                MlcModel = "Millennium 120",
                ArcLength = 360.0,
                IsSetupField = false
            };

            // 100mm x 100mm rectangular fixed aperture (JawY1 = -50, JawY2 = 50)
            // Millennium 120 center leaves: leaves positioned between Y = -50 and +50 mm (leaves 20-39)
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
                    // Aperture width 100 mm (Left Bank = -50, Right Bank = +50)
                    cpGeom.LeafPositions[0, l] = -50.0f;
                    cpGeom.LeafPositions[1, l] = 50.0f;
                }
                beam.ControlPoints.Add(cpGeom);
            }

            var result = PlanComplexityAnalysis.AnalyzeBeam(beam);

            Assert.IsNotNull(result);
            Assert.AreEqual("Arc_Bench1", result.BeamId);
            // In rectangular aperture, MCS = 1.0 (theoretical) due to zero leaf modulation
            Assert.AreEqual(1.0, result.ModulationComplexityScore, 1e-3);
            // Edge Metric = 0.0 due to zero lateral steps
            Assert.AreEqual(0.0, result.EdgeMetric, 1e-3);
            // Leaf Travel = 0.0 due to no leaf motion
            Assert.AreEqual(0.0, result.LeafTravel, 1e-3);
            Assert.AreEqual(360.0, result.ArcLength, 1e-3);

            // Verify ToString format
            string str = result.ToString();
            Assert.IsTrue(str.StartsWith("(Arc_Bench1:1,0,0,360"));
        }

        [TestMethod]
        [Description("AnalyzeBeam: Verifies MCS reduction and EdgeMetric/LeafTravel increases for arc with leaf modulation and movement")]
        public void AnalyzeBeam_ModulatedVmatField_ShouldReflectIncreasedComplexity()
        {
            var beam = new BeamGeometry
            {
                BeamId = "VMAT_Mod",
                MlcModel = "Millennium 120",
                ArcLength = 360.0,
                IsSetupField = false
            };

            // Staggered leaf arrangement and motion across 3 CPs
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
                    // Zig-zag leaf positions between even and odd leaves, moving across CPs
                    float shift = (l % 2 == 0) ? (cp * 5.0f) : (-cp * 5.0f);
                    cpGeom.LeafPositions[0, l] = -30.0f + shift;
                    cpGeom.LeafPositions[1, l] = 30.0f + shift;
                }
                beam.ControlPoints.Add(cpGeom);
            }

            var result = PlanComplexityAnalysis.AnalyzeBeam(beam);

            Assert.IsNotNull(result);
            Assert.AreEqual("VMAT_Mod", result.BeamId);
            // MCS decreases below 1.0 due to complex modulation
            Assert.IsTrue(result.ModulationComplexityScore < 1.0, "MCS should be less than 1.0 for modulated field");
            Assert.IsTrue(result.ModulationComplexityScore > 0.0, "MCS should be greater than 0");
            // Edge Metric is positive due to lateral leaf steps
            Assert.IsTrue(result.EdgeMetric > 0.0, "EdgeMetric should be positive");
            // Leaf Travel is positive due to leaf motion between CPs
            Assert.IsTrue(result.LeafTravel > 0.0, "LeafTravel should be positive");
        }
    }
}
