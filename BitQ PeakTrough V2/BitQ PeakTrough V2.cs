using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using System.Collections;
namespace BitQIndicator
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
        private const double MIN_COMPARE_VALUE = 1E-08;
        private int lastPeakIndex = 0;
        private int lastTroughIndex = 0;
        private double lastPeakValue = 0;
        private double lastTroughValue = LARGE_NUM;
        private int mPeakIndex = 0;
        private int mTroughIndex = 0;
        private double mPeakValue = 0;
        private double mTroughValue = LARGE_NUM;
        private bool isFindingPeak = true;
        public IndicatorDataSeries dataSeries;
        private double pipValue;
        private AverageTrueRange atrIndicator;
        public static ArrayList peakData = new ArrayList();
        public static ArrayList troughtData = new ArrayList();
        private Utils.Base utils = new Utils.Base();
        private IndicatorDataSeries lineChartDataSeries;
        private bool isLastDown = true;

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

            //findPeakTrough(index, AutoThreshold ? thresholdBaseonATR : Threshold);
            findPeakTroughV2(index, thresholdBaseonATR, true);
            //Result[index] = index;
            //Print("peaks length = ", peakData.Count);
            //Print("troughs length = ", troughtData.Count);

        }

        public void findPeakTroughV2(int index, double threshold = 0, bool shoulDraw = true)
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
                        Chart.DrawIcon("peak_" + (index - 1), ChartIconType.DownArrow, index - 1, lineChartDataSeries[index - 1], Color.White);
                        var data = new Utils.Base.Point(index - 1, lineChartDataSeries[index - 1], Bars.OpenTimes[index - 1]);
                        peakData.Add(data);
                    }
                    else
                    {
                        Chart.DrawIcon("peak_" + (index - 2), ChartIconType.DownArrow, index - 2, lineChartDataSeries[index - 2], Color.White);
                        var data = new Utils.Base.Point(index - 2, lineChartDataSeries[index - 2], Bars.OpenTimes[index - 2]);
                        peakData.Add(data);
                    }
                }
                if (lineChartDataSeries[index - 2] > lineChartDataSeries[index - 1] && lineChartDataSeries[index - 1] < lineChartDataSeries[index])
                {
                    if (!isGreenCandle(index - 1) || true)
                    {
                        Chart.DrawIcon("trough_" + (index - 1), ChartIconType.UpArrow, index - 1, lineChartDataSeries[index - 1], Color.Blue);
                        var data = new Utils.Base.Point(index - 1, lineChartDataSeries[index - 1], Bars.OpenTimes[index - 1]);
                        troughtData.Add(data);
                    }
                    //else
                    //{
                    //    Chart.DrawIcon("trough_" + (index - 2), ChartIconType.DownArrow, index - 2, lineChartDataSeries[index - 2], Color.Blue);
                    //}
                }
            }

        }

        public bool isGreenCandle(int index)
        {
            var openPrice = Bars.OpenPrices[index];
            var closePrice = Bars.ClosePrices[index];

            return openPrice < closePrice;
        }


        public void findPeakTrough(int index, double threshold = 0, bool shouldDraw = true)
        {
            var openPrice = Bars.OpenPrices[index];
            var closePrice = Bars.ClosePrices[index];
            var high = Math.Max(openPrice, closePrice);
            var low = Math.Min(openPrice, closePrice);
            var time = Bars.OpenTimes[index];
            var shouldOveridePeak = true;
            var shouldOverideTrough = true;
            // create point:
            //Print("index = " + index + "; OP=" + openPrice + " ; CP=" + closePrice + " ; open > close : " + (openPrice> closePrice));
            //if(openPrice - closePrice < MIN_COMPARE_VALUE)
            //{
            //     green candle.
            //    Chart.DrawIcon("Linechart_Icon_" + index.ToString(), ChartIconType.Circle, index, closePrice, Color.White);
            //} else
            //{
            //     red candle.
            //    Chart.DrawIcon("Linechart_Icon_" + index.ToString(), ChartIconType.Circle, index, closePrice, Color.White);
            //}
            //if (!(index >= 1150 && index <= 1180)) return;
            // LOGIC:
            /*
             * STEP 1: Finding peak:
             *  - If there are a value that create a peak and it's green candle. It MAY be peak (1)
             *  - When found a trough (same but reverse logic with peak), that peak (1) can be confirm.
             *
             */
            if (isFindingPeak)
            {
                //Print("isFindingPeak:", isFindingPeak + ",index:"+ index);
                // is higher value from it and it is green candle
                if (isValueLowerCurrent(mPeakValue, index))
                {
                    if (openPrice < closePrice)
                    {
                        mPeakIndex = index;
                        mPeakValue = high;
                    }
                }
                else
                {
                    //Print("isFindingPeak:stop:", index);
                    lastPeakIndex = mPeakIndex;
                    lastPeakValue = mPeakValue;
                    // compare to threshold; --> found a peak;
                    var currChanging = Math.Floor(Math.Abs(mPeakValue - lastTroughValue) * pipValue);
                    if (currChanging > threshold)
                    {

                        // old trought should been pick;
                        if (shouldDraw)
                        {
                            Chart.DrawIcon("icone" + index.ToString(), ChartIconType.UpTriangle, lastTroughIndex, lastTroughValue - IconSpace / pipValue, Color.Blue);
                            //Print("isFindingPeak:drawTrough:", index+ " ,"+ lastTroughIndex +" ," +lastPeakIndex);
                        }
                        dataSeries[lastTroughIndex] = -lastTroughValue;
                        var data = new Utils.Base.Point(lastTroughIndex, lastTroughValue, time);
                        troughtData.Add(data);

                        // reset value, changing to find Trough
                        isFindingPeak = !isFindingPeak;
                        mPeakValue = 0;
                        lastTroughValue = LARGE_NUM;
                        shouldOverideTrough = false;
                    }
                    else
                    {
                        // reset value
                        mPeakValue = 0;
                    }

                }
                if (isValueHigherCurrent(lastTroughValue, index) && shouldOverideTrough)
                {
                    // If it was red candle
                    if (openPrice > closePrice)
                    {
                        //Print("isFindingPeak:stop:findNewTrough", index);
                        // replace older trought with new one suitable;
                        lastTroughValue = low;
                        lastTroughIndex = index;
                        // reset old mPeakValue value;
                        mPeakValue = low;
                    }

                }
            }
            else
            {
                //Print("isFindingTrough", isFindingPeak + ",index:" + index);
                // find a trough
                if (isValueHigherCurrent(mTroughValue, index))
                {
                    // If it was a red candle.
                    if (openPrice > closePrice)
                    {
                        mTroughValue = low;
                        mTroughIndex = index;
                    }
                }
                else
                {
                    //Print("isFindingTrough:stop:", index);
                    lastTroughIndex = mTroughIndex;
                    lastTroughValue = mTroughValue;
                    // compare to threshold; --> found a trough;
                    var currChanging = Math.Floor(Math.Abs(mTroughValue - lastPeakValue) * pipValue);
                    if (currChanging > threshold)
                    {
                        if (shouldDraw)
                        {
                            Chart.DrawIcon("icone" + index.ToString(), ChartIconType.DownTriangle, lastPeakIndex, lastPeakValue + IconSpace / pipValue, Color.Purple);
                            //Print("isFindingTrough:drawPeak:", index + " ," + lastPeakIndex + " ," + lastTroughIndex);
                        }
                        dataSeries[lastPeakIndex] = lastPeakValue;
                        var data = new Utils.Base.Point(lastPeakIndex, lastPeakValue, time);
                        peakData.Add(data);
                        // reset value, changing to find Peak
                        isFindingPeak = !isFindingPeak;
                        mTroughValue = LARGE_NUM;
                        lastPeakValue = 0;
                        shouldOveridePeak = false;
                    }
                    else
                    {
                        //resetvalue
                        mTroughValue = LARGE_NUM;
                    }
                }
                if (isValueLowerCurrent(lastPeakValue, index) && shouldOveridePeak)
                {
                    // if it was a green candle
                    if (openPrice < closePrice)
                    {
                        //Print("isFindingTrought:stop:findNewPeak", index);
                        lastPeakValue = high;
                        lastPeakIndex = index;
                        // reset
                        mTroughValue = high;
                    }

                }
            }
        }

        public bool isValueLowerCurrent(double value, int index)
        {
            var openPrice = Bars.OpenPrices[index];
            var closePrice = Bars.ClosePrices[index];
            var max = Math.Max(openPrice, closePrice);
            if (value - max < MIN_COMPARE_VALUE)
                return true;
            return false;
        }

        public bool isValueHigherCurrent(double value, int index)
        {
            var openPrice = Bars.OpenPrices[index];
            var closePrice = Bars.ClosePrices[index];
            var min = Math.Min(openPrice, closePrice);
            if (value - min > MIN_COMPARE_VALUE)
                return true;
            return false;
        }

        public ArrayList getPeakData()
        {
            return peakData;
        }

        public ArrayList getTroughData()
        {
            return troughtData;
        }

        public void reset()
        {
            lastPeakIndex = 0;
            lastTroughIndex = 0;
            lastPeakValue = 0;
            lastTroughValue = LARGE_NUM;
            mPeakIndex = 0;
            mTroughIndex = 0;
            mPeakValue = 0;
            mTroughValue = LARGE_NUM;
            isFindingPeak = true;
            dataSeries = CreateDataSeries();
            peakData = new ArrayList();
            troughtData = new ArrayList();
        }
    }
}
