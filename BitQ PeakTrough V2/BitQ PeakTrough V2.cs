using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using System.Collections;
using DataType;
namespace BitQPeakTrough
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BitQPeakTrough : Indicator
    {
        [Parameter("AutoThreshold", DefaultValue = true)]
        public bool AutoThreshold { get; set; }

        [Parameter("Icon Space", DefaultValue = 50, Step = 10)]
        public int IconSpace { get; set; }
        [Parameter("Threshold", DefaultValue = -10, Step = 10)]
        public int Threshold { get; set; }

        [Output("Main", PlotType = PlotType.Points)]
        public IndicatorDataSeries Result { get; set; }

        private const double LARGE_NUM = 99999999999L;
        public IndicatorDataSeries dataSeries;
        private double pipValue;
        private AverageTrueRange atrIndicator;
        public static ArrayList peakData = new ArrayList();
        public static ArrayList troughtData = new ArrayList();
        public static ArrayList peakTroughData = new ArrayList();
        // memory peak trough value
        private static BitQ_Point mPeakPoint = new BitQ_Point(0, 0, new DateTime());
        private static BitQ_Point mTroughPoint = new BitQ_Point(0, LARGE_NUM, new DateTime());
        private IndicatorDataSeries lineChartDataSeries;
        // check current work is find peak or trough'
        private bool isFindPeak = true;

        public void init(bool autoThreshold, int iconSpace)
        {
            AutoThreshold = autoThreshold;
            IconSpace = iconSpace;
        }

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            atrIndicator = Indicators.AverageTrueRange(10, MovingAverageType.Exponential);
            dataSeries = CreateDataSeries();
            lineChartDataSeries = CreateDataSeries();
            pipValue = Math.Floor(1 / Symbol.PipSize);
            Print("pipValue: ", pipValue);
        }

        public override void Calculate(int index)
        {
            double thresholdBaseonATR = atrIndicator.Result[index] * pipValue;

            findPeakTroughV2(index, AutoThreshold ? thresholdBaseonATR : Threshold, true);
            //Print("peaks length = ", peakData.Count);
            //Print("troughs length = ", troughtData.Count);

        }

        public void findPeakTroughV2(int index, double threshold = 0, bool shouldDraw = true)
        {
            var openPrice = Bars.OpenPrices[index];
            var closePrice = Bars.ClosePrices[index];
            var time = Bars.OpenTimes[index];
            // don't case to open price.
            lineChartDataSeries[index] = closePrice;
            if (true)
            {
                // find peak without trought
                if (lineChartDataSeries[index - 2] < lineChartDataSeries[index - 1] && lineChartDataSeries[index - 1] > lineChartDataSeries[index])
                {
                    if (isGreenCandle(index - 1))
                    {
                        //Chart.DrawIcon("peak_" + (index - 1), ChartIconType.DownArrow, index - 1, lineChartDataSeries[index - 1], Color.White);
                        BitQ_Point data = new BitQ_Point(index - 1, lineChartDataSeries[index - 1], Bars.OpenTimes[index - 1]);
                        confirmPeakAndTrough(true, data, threshold, shouldDraw);
                    }
                }
                if (lineChartDataSeries[index - 2] > lineChartDataSeries[index - 1] && lineChartDataSeries[index - 1] < lineChartDataSeries[index])
                {
                    if (!isGreenCandle(index - 1) || true)
                    {
                        //Chart.DrawIcon("trough_" + (index - 1), ChartIconType.DownArrow, index - 1, lineChartDataSeries[index - 1], Color.Blue);
                        BitQ_Point data = new BitQ_Point(index - 1, lineChartDataSeries[index - 1], Bars.OpenTimes[index - 1]);
                        confirmPeakAndTrough(false, data, threshold, shouldDraw);
                    }
                }
            }

        }

        public void confirmPeakAndTrough(bool isPeak, BitQ_Point pointData, double threshold, bool shouldDraw)
        {
            if (isFindPeak)
            {
                /**
                 * Currently finding true peak. 
                 * If got a trough, check that trought with mPeakPoint then confirm that
                 * mPeakPoint or not.
                 * If got a peak, check that peak with mPeakPoint, if yValue of that 
                 * greater than mPeakPoint --> replace mPeakPoint with that point 
                 */
                if (isPeak)
                {
                    // got a peak.
                    if (pointData.yValue > mPeakPoint.yValue)
                    {
                        mPeakPoint = pointData;
                    }
                }
                else
                {
                    // got a trough.
                    if ((mPeakPoint.yValue - pointData.yValue) * pipValue > threshold && mPeakPoint.yValue != 0)
                    {
                        // satified condition, confirm mPeakPoint and change to find true trought
                        peakData.Add(mPeakPoint);
                        peakTroughData.Add(mPeakPoint);
                        isFindPeak = false;

                        // draw peak;
                        if (shouldDraw)
                        {
                            Chart.DrawIcon("peak_" + mPeakPoint.barIndex, ChartIconType.DownArrow, mPeakPoint.barIndex, mPeakPoint.yValue, Color.White);
                        }

                        // set pointData to mTroughPoint;
                        mTroughPoint = pointData;

                        //reset cache peak point
                        mPeakPoint = new BitQ_Point(0, 0, new DateTime());

                    }
                }
            }
            else
            {
                /**
                 * Currently finding true trough. 
                 * If got a peak, check that trought with mTroughPoint then confirm that
                 * mTroughPoint or not.
                 * If got a trough, check that trough with mTroughPoint, if yValue of that 
                 * smaller than mTroughPoint --> replace mTroughPoint with that point 
                 */
                if (!isPeak)
                {
                    // got a trough
                    if (pointData.yValue < mTroughPoint.yValue)
                    {
                        mTroughPoint = pointData;
                    }
                }
                else
                {
                    // got a peak
                    if ((pointData.yValue - mTroughPoint.yValue) * pipValue > threshold && mTroughPoint.yValue != LARGE_NUM)
                    {
                        // satified condition, confirm mTroughPoint and change to find true peak
                        troughtData.Add(mTroughPoint);
                        peakTroughData.Add(mTroughPoint);
                        isFindPeak = true;

                        // draw trough;
                        if (shouldDraw)
                        {
                            Chart.DrawIcon("trough_" + mTroughPoint.barIndex, ChartIconType.UpArrow, mTroughPoint.barIndex, mTroughPoint.yValue, Color.Blue);
                        }

                        // set pointData to mPeakPoint;
                        mPeakPoint = pointData;

                        //reset cache peak point
                        mTroughPoint = new BitQ_Point(0, LARGE_NUM, new DateTime());

                    }
                }
            }
        }

        public bool isGreenCandle(int index)
        {
            var openPrice = Bars.OpenPrices[index];
            var closePrice = Bars.ClosePrices[index];

            return openPrice < closePrice;
        }

        public ArrayList getPeakData()
        {
            return peakData;
        }

        public ArrayList getTroughData()
        {
            return troughtData;
        }

        public ArrayList getPeakTroughData()
        {
            return peakTroughData;
        }

        public void reset()
        {
            dataSeries = CreateDataSeries();
            peakData = new ArrayList();
            troughtData = new ArrayList();
            peakTroughData = new ArrayList();
        }
    }
}
