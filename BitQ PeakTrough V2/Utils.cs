using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
namespace DataType
{
    public struct BitQ_Point
    {
        public double yValue;
        public int barIndex;
        public DateTime dateTime;

        public BitQ_Point(int _barIndex, double _yValue, DateTime _dateTime)
        {
            yValue = _yValue;
            barIndex = _barIndex;
            dateTime = _dateTime;
        }
    }

    public struct TrendLineData
    {
        public String id;
        public BitQ_Point startPoint;
        public BitQ_Point endPoint;
        public double b;
        // line y = ${b}x + w;
        public double w;
        public bool isUsed;
        public TrendLineData(String _id, BitQ_Point _startPoint, BitQ_Point _endPoint, double _b, double _w, bool _isUsed = false)
        {
            id = _id;
            startPoint = _startPoint;
            endPoint = _endPoint;
            b = _b;
            w = _w;
            isUsed = _isUsed;
        }
    }

    public enum RSI_TYPE
    {
        BEARISH = 0,
        BULLISH = 1,
    }

    public enum RECENT_CHANGE
    {
        MIXED = 0,
        DECREASE = 1,
        INCREASE = 2,
    }

    public enum TREND_TYPE
    {
        UNCONFIRMED = 0,
        UPTREND = 1,
        DOWNTREND = 2,
    }

    public struct RsiData
    {
        public int type;
        public BitQ_Point startPoint;
        public BitQ_Point endPoint;
        public int streight;
        public int recentChange;
        public int lastTrend;
        public RsiData(BitQ_Point _startPoint, BitQ_Point _endPoint, RSI_TYPE _type, int _streight = 0, RECENT_CHANGE _recentChange = RECENT_CHANGE.MIXED, TREND_TYPE _lastTrend = TREND_TYPE.UNCONFIRMED)
        {
            startPoint = _startPoint;
            endPoint = _endPoint;
            type = (int)_type;
            streight = _streight;
            recentChange = (int)_recentChange;
            lastTrend = (int)_lastTrend;
        }

    }
}


namespace Function
{
    public class Helper
    {
        public static double distanceFromPointToLine(double x, double y, double slope, double b)
        {
            double distance = Math.Abs(slope * x - y + b) / Math.Sqrt(Math.Pow(slope, 2) + Math.Pow(-1, 2));
            return distance;
        }

        public static void findLine(DataType.BitQ_Point point1, DataType.BitQ_Point point2, out double b, out double w)
        {
            b = (point2.yValue - point1.yValue) / (point2.barIndex - point1.barIndex);
            w = point1.yValue - b * point1.barIndex;
        }

        public static int ToUnixTimestamp(DateTime d)
        {
            var epoch = d - new DateTime(1970, 1, 1, 0, 0, 0);
            return (int)epoch.TotalSeconds;
        }

        public static void LinearRegression(double[] xVals, double[] yVals, out double rSquared, out double yIntercept, out double slope)
        {
            if (xVals.Length != yVals.Length)
            {
                throw new Exception("Input values should be with the same length.");
            }

            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double sumCodeviates = 0;

            for (var i = 0; i < xVals.Length; i++)
            {
                var x = xVals[i];
                var y = yVals[i];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }

            var count = xVals.Length;
            var ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            var ssY = sumOfYSq - ((sumOfY * sumOfY) / count);

            var rNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            var rDenom = (count * sumOfXSq - (sumOfX * sumOfX)) * (count * sumOfYSq - (sumOfY * sumOfY));
            var sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            var meanX = sumOfX / count;
            var meanY = sumOfY / count;
            var dblR = rNumerator / Math.Sqrt(rDenom);

            rSquared = dblR * dblR;
            yIntercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }

        public static int FindLineThreshold(double price)
        {
            if (Math.Floor(price / 1000) > 0)
                return 1;
            if (Math.Floor(price / 100) > 0)
                return 100;
            return 10000;
        }

        public static bool shouldModifyTrailingStop(Position position,
            double currAskPrice, double currBidPrice,
            double pipSize,
            double baseSL, double ratioRR,
            out double? SL, out double? TP)
        {
            bool shoulModify = false;
            SL = position.StopLoss;
            TP = 1 / ratioRR * baseSL;
            if (position.TradeType == TradeType.Buy)
            {
                var profit = currAskPrice - position.EntryPrice;
                var currSL = (double)position.StopLoss;
                var newSL = currAskPrice - baseSL * pipSize;
                if (newSL > currSL)
                {
                    SL = newSL;
                    shoulModify = true;
                }
            }
            return shoulModify;
        }

        public static bool hasHigherPointAtRange(double price, int startRange, int endRange, Bars bars)
        {
            bool hasHigherPoint = false;
            for (int i = startRange; i <= endRange; i++)
            {
                var highest = Math.Max(bars.OpenPrices[i], bars.ClosePrices[i]);
                if (highest > price)
                {
                    hasHigherPoint = true;
                    break;
                }
            }

            return hasHigherPoint;
        }

        public static bool hasLowerPointAtRange(double price, int startRange, int endRange, Bars bars)
        {
            bool hasLowerPoint = false;
            for (int i = startRange; i <= endRange; i++)
            {
                var lowest = Math.Min(bars.OpenPrices[i], bars.ClosePrices[i]);
                if (lowest < price)
                {
                    hasLowerPoint = true;
                    break;
                }
            }

            return hasLowerPoint;
        }

        public static bool shouldModifyTrailingStopT1(Position position,
            double currAskPrice, double currBidPrice,
            double pipSize,
            double baseSL, double ratioRR,
            out double? SL, out double? TP)
        {
            bool shoulModify = false;
            SL = position.StopLoss;
            TP = 1 / ratioRR * baseSL;
            if (position.TradeType == TradeType.Buy)
            {
                var profit = currAskPrice - position.EntryPrice;
                var currSL = (double)position.StopLoss;
                var newSL = currAskPrice - baseSL * pipSize;
                if (newSL > currSL)
                {
                    SL = newSL;
                    shoulModify = true;
                }
                if (profit > baseSL * 1 / ratioRR)
                {
                    newSL += baseSL / 2;
                    SL = newSL;
                }
            }
            return shoulModify;
        }
    }
}
