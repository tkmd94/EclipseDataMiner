using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace EclipseDataMiner
{
    public static class PlanComplexityAnalysis
    {
        public static string Proccess(Patient patient, PlanSetup planSetup)
        {
            //Reference
            //Modulation Complexity Score:
            //Masi, L. , Doro, R. , Favuzza, V. , Cipressi, S. and Livi, L. (2013), Impact of plan parameters on the dosimetric accuracy of volumetric modulated arc therapy. Med. Phys., 40: 071718. doi:10.1118/1.4810969
            //Edge Metric:
            //Younge, K. C., Matuszak, M. M., Moran, J. M., McShan, D. L., Fraass, B. A. and Roberts, D. A. (2012), Penalization of aperture complexity in inversely planned volumetric modulated arc therapy. Med. Phys., 39: 7160-7170. doi:10.1118/1.4762566

            const double C1_EDGEMETRIC = 0.0;
            const double C2_EDGEMETRIC = 1.0;
            string msg = "";
            string msg_summary = "";
            string msg_CP = "";

            foreach (var beam in planSetup.Beams)
            {
                double edgeMetric = 0;
                double leafTravel = 0;

                int nCP = beam.ControlPoints.Count();

                if (!beam.IsSetupField && //exclude setup field
                beam.MLC != null && //exclude non-MLC field
                nCP > 2  //exclude non-modulation field
                )
                {
                    msg_summary += "(" + beam.Id.ToString() + ":";
                    msg_CP += "(" + beam.Id.ToString() + ":";

                    // Generate leaf boundary position data.
                    double[,] leafBoundArray;
                    bool fMakeLeafBountArray = makeLeafBoundArray(beam.MLC.Model, out leafBoundArray);
                    if (fMakeLeafBountArray == false)
                    {
                        return "ERROR";
                    }

                    double[] metersetWeightCP = new double[nCP];
                    for (int idx = 0; idx < nCP; idx++)
                    {
                        metersetWeightCP[idx] = beam.ControlPoints.ElementAt(idx).MetersetWeight;
                    }

                    //define variables for MCS
                    double[] lsv_CP = new double[nCP];
                    double[] aav_CP = new double[nCP];

                    float[,] prevLeaf = new float[2, 60];
                    int cpCount = 0;
                    foreach (var cp in beam.ControlPoints)
                    {

                        //define variables for EM
                        double sumLeafSidePerCP = 0.0;
                        double sumLeafEndPerCP = 0.0;
                        double sumAreaPerCP = 0.0;
                        double edgeMetricPerCP = 0;



                        //define variables for MCS
                        int countLeafInField = 0;
                        double posMax_Lb = 0;
                        double posMax_Rb = 0;
                        double leafSide_Rb = 0;
                        double leafSide_Lb = 0;
                        double openLeafWidth = 0;

                        double minPos_Rb = 10000;
                        double maxPos_Rb = -10000;
                        double minPos_Lb = 10000;
                        double maxPos_Lb = -10000;

                        var jaws = cp.JawPositions;
                        var leaf = cp.LeafPositions;

                        for (int leaf_loop = 0; leaf_loop < 60; leaf_loop++)
                        {
                            double leafSide = 0;
                            double leafEnd = 0;
                            double leafArea = 0;
                            double leafEdgeD = leafBoundArray[leaf_loop, 0];
                            double leafEdgeU = leafBoundArray[leaf_loop, 1];

                            if (jaws.Y2 <= leafEdgeD)//leaf position is under the jaws.Y2
                            {
                                // no calculation
                            }
                            else if (jaws.Y1 >= leafEdgeU)//leaf position is under the jaws.Y1
                            {
                                // no calculation
                            }
                            else if (jaws.Y2 > leafEdgeD && jaws.Y2 <= leafEdgeU)//patial overlap jaws.Y2 
                            {
                                leafSide = leaf[1, leaf_loop] - leaf[0, leaf_loop];
                                leafEnd = jaws.Y2 - leafEdgeD;
                                leafArea = leafEnd * (leaf[1, leaf_loop] - leaf[0, leaf_loop]);

                                //MessageBox.Show("Y1:" + jaws.Y1.ToString() + "\n"
                                //    + ",Y2:" + jaws.Y2.ToString() + "\n"
                                //    + ",leafB(L):" + leaf[0, leaf_loop].ToString() + "\n"
                                //    + ",leafA(R):" + leaf[1, leaf_loop].ToString() + "\n"
                                //    + ",leafEdgeU:" + leafEdgeU.ToString() + "\n"
                                //    + ",leafEdgeD:" + leafEdgeD.ToString() + "\n"
                                //    + "leaf:" + leaf_loop.ToString() + "\n"
                                //    + "leafSide:" + leafSide.ToString() + "\n"
                                //    + ",leafEnd:" + leafEnd.ToString() + "\n"
                                //    + ",leafArea:" + leafArea.ToString());


                                // calculate leaf travel
                                leafTravel += calcLT(cpCount, leaf_loop, leaf, prevLeaf);

                                // calculate Min/Max leaf position in each CP
                                if (leaf[1, leaf_loop] < minPos_Rb) minPos_Rb = leaf[1, leaf_loop];
                                if (leaf[1, leaf_loop] > maxPos_Rb) maxPos_Rb = leaf[1, leaf_loop];
                                if (leaf[0, leaf_loop] < minPos_Lb) minPos_Lb = leaf[0, leaf_loop];
                                if (leaf[0, leaf_loop] > maxPos_Lb) maxPos_Lb = leaf[0, leaf_loop];
                                openLeafWidth += leaf[1, leaf_loop] - leaf[0, leaf_loop];


                            }
                            else if (jaws.Y1 >= leafEdgeD && jaws.Y1 < leafEdgeU)//patial overlap jaws.Y1
                            {
                                leafSide = Math.Abs(leaf[1, leaf_loop] - leaf[1, leaf_loop - 1]) + Math.Abs(leaf[0, leaf_loop] - leaf[0, leaf_loop - 1]) //upper side
                                         + Math.Abs(leaf[1, leaf_loop] - leaf[0, leaf_loop]);//lower side
                                leafEnd = leafEdgeU - jaws.Y1;
                                leafArea = leafEnd * (leaf[1, leaf_loop] - leaf[0, leaf_loop]);

                                // calculate leaf travel
                                leafTravel += calcLT(cpCount, leaf_loop, leaf, prevLeaf);

                                // calculate Min/Max leaf position in each CP
                                if (leaf[1, leaf_loop] < minPos_Rb) minPos_Rb = leaf[1, leaf_loop];
                                if (leaf[1, leaf_loop] > maxPos_Rb) maxPos_Rb = leaf[1, leaf_loop];
                                if (leaf[0, leaf_loop] < minPos_Lb) minPos_Lb = leaf[0, leaf_loop];
                                if (leaf[0, leaf_loop] > maxPos_Lb) maxPos_Lb = leaf[0, leaf_loop];
                                leafSide_Rb += Math.Abs(leaf[1, leaf_loop] - leaf[1, leaf_loop - 1]);
                                leafSide_Lb += Math.Abs(leaf[0, leaf_loop] - leaf[0, leaf_loop - 1]);
                                countLeafInField++;
                                openLeafWidth += leaf[1, leaf_loop] - leaf[0, leaf_loop];

                            }
                            else if (jaws.Y2 >= leafEdgeU && jaws.Y1 <= leafEdgeD) //Y1&Y2 is not overlapping current leaf
                            {
                                leafSide = Math.Abs(leaf[1, leaf_loop] - leaf[1, leaf_loop - 1]) + Math.Abs(leaf[0, leaf_loop] - leaf[0, leaf_loop - 1]);
                                leafEnd = leafEdgeU - leafEdgeD;
                                leafArea = leafEnd * (leaf[1, leaf_loop] - leaf[0, leaf_loop]);

                                // calculate leaf travel
                                leafTravel += calcLT(cpCount, leaf_loop, leaf, prevLeaf);

                                // calculate Min/Max leaf position in each CP
                                if (leaf[1, leaf_loop] < minPos_Rb) minPos_Rb = leaf[1, leaf_loop];
                                if (leaf[1, leaf_loop] > maxPos_Rb) maxPos_Rb = leaf[1, leaf_loop];
                                if (leaf[0, leaf_loop] < minPos_Lb) minPos_Lb = leaf[0, leaf_loop];
                                if (leaf[0, leaf_loop] > maxPos_Lb) maxPos_Lb = leaf[0, leaf_loop];
                                leafSide_Rb += Math.Abs(leaf[1, leaf_loop] - leaf[1, leaf_loop - 1]);
                                leafSide_Lb += Math.Abs(leaf[0, leaf_loop] - leaf[0, leaf_loop - 1]);
                                countLeafInField++;
                                openLeafWidth += leaf[1, leaf_loop] - leaf[0, leaf_loop];

                            }
                            else
                            {
                                return "ERROR";
                            }

                            //EM
                            sumLeafSidePerCP += leafSide;
                            sumLeafEndPerCP += 2 * leafEnd;
                            sumAreaPerCP += leafArea;
                        }


                        double weight = 0;
                        if (cpCount == 0)
                        {
                            weight = metersetWeightCP[cpCount + 1] / 2.0;
                        }
                        else if (cpCount == (nCP - 1))
                        {
                            weight = (metersetWeightCP[cpCount] - metersetWeightCP[cpCount - 1]) / 2.0;
                        }
                        else
                        {
                            weight = (metersetWeightCP[cpCount + 1] - metersetWeightCP[cpCount]) / 2.0 + (metersetWeightCP[cpCount] - metersetWeightCP[cpCount - 1]) / 2.0;
                        }

                        //EM
                        edgeMetricPerCP = weight * (C1_EDGEMETRIC * sumLeafEndPerCP + C2_EDGEMETRIC * sumLeafSidePerCP) / sumAreaPerCP;
                        edgeMetric += edgeMetricPerCP;

                        //MCS
                        posMax_Lb = maxPos_Lb - minPos_Lb;
                        posMax_Rb = maxPos_Rb - minPos_Rb;

                        lsv_CP[cpCount] = (countLeafInField * posMax_Rb - leafSide_Rb) / (countLeafInField * posMax_Rb)
                                        * (countLeafInField * posMax_Lb - leafSide_Lb) / (countLeafInField * posMax_Lb);

                        aav_CP[cpCount] = openLeafWidth / (countLeafInField + 1) / (maxPos_Rb - minPos_Lb);


                        msg_CP += (cpCount + 1).ToString() + "," +
                               edgeMetricPerCP.ToString() +
                               //weight.ToString() +
                               //sumLeafEndPerCP.ToString() +
                               //sumLeafSidePerCP.ToString() +
                               //sumAreaPerCP.ToString() +
                               "\n";

                        //hold current leaf position for LT
                        prevLeaf = leaf;

                        cpCount++;
                    }

                    //MCS
                    double mcs = 0;
                    for (var cp_loop = 1; cp_loop < beam.ControlPoints.Count(); cp_loop++)
                    {
                        mcs += ((aav_CP[cp_loop] + aav_CP[cp_loop - 1]) / 2.0
                             * (lsv_CP[cp_loop] + lsv_CP[cp_loop - 1]) / 2.0
                             * (metersetWeightCP[cp_loop] - metersetWeightCP[cp_loop - 1])
                             );
                    }
                    msg_CP += "TOTAL," + edgeMetric.ToString() + "\n";
                    msg_summary += Math.Round(mcs, 2).ToString() + "," +
                        Math.Round(edgeMetric, 2).ToString() + "," +
                        Math.Round(leafTravel, 1).ToString() + "," +
                        Math.Round(beam.ArcLength, 1).ToString() +
                                   ")";

                }
            }
            msg += msg_summary + msg_CP;
            return msg_summary;
        }
        /// <summary>
        /// Generate leaf boundary coordinates
        /// </summary>
        /// <param name="type"></param>
        /// <param name="leafBoundArray"></param>
        /// <returns></returns>
        public static bool makeLeafBoundArray(string type, out double[,] leafBoundArray)
        {
            leafBoundArray = new double[60, 2];
            if (type == "Varian High Definition 120")
            {
                byte i;
                for (i = 0; i < 60; i++)
                {
                    if ((i >= 14) && (i < 46))
                    {
                        leafBoundArray[i, 0] = leafBoundArray[i - 1, 1];
                        leafBoundArray[i, 1] = leafBoundArray[i, 0] + 2.5;
                    }
                    else if (i == 0)
                    {
                        leafBoundArray[i, 0] = 0.0 - 110.0;
                        leafBoundArray[i, 1] = 5.0 - 110.0;
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
                byte i;
                for (i = 0; i < 60; i++)
                {
                    if ((i >= 10) && (i < 49))
                    {
                        leafBoundArray[i, 0] = leafBoundArray[i - 1, 1];
                        leafBoundArray[i, 1] = leafBoundArray[i, 0] + 5.0;
                    }
                    else if (i == 0)
                    {
                        leafBoundArray[i, 0] = 0.0 - 200.0;
                        leafBoundArray[i, 1] = 10.0 - 200.0;
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
        }  //end of makeLeafBoundArray

        /// <summary>
        /// calculate distance of leaf travel
        /// </summary>
        /// <param name="cpCount"></param>
        /// <param name="leafNo"></param>
        /// <param name="leaf"></param>
        /// <param name="prevLeaf"></param>
        /// <returns></returns>
        public static double calcLT(int cpCount, int leafNo, float[,] leaf, float[,] prevLeaf)
        {
            double distance = 0;

            // exlude 1st control point 
            if (cpCount > 0)
            {
                distance = Math.Abs(leaf[1, leafNo] - prevLeaf[1, leafNo]) + Math.Abs(leaf[0, leafNo] - prevLeaf[0, leafNo]);
            }
            return distance;
        }
    }
}
