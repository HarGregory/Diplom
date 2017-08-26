

namespace #NameStrategy
{
	using System.Collections.Generic;
	using TSLab.Script;
	using TSLab.Script.Handlers;
	using TSLab.Script.Optimization;
	using TSLab.Script.Helpers;
	
    public class Program : IExternalScript
    {
        public bool bBuy;
        public bool bSell; 
        public bool bShort; 
        public bool bCover; 

        public bool CheckSpikeHigh(int Bar, ISecurity source, IList<double> nMax, double DiffLimit, double CloseKoef,IContext ctx)
        {
            bool bCheck = false;
            double PricePrev = source.HighPrices[Bar - 1];
            double Price = source.HighPrices[Bar];
            double PriceNext = source.HighPrices[Bar + 1];
            double diff;
            double range;
            double CloseLimit;

            if (Price > PricePrev && Price > PriceNext)
            {
                diff = (Price - PricePrev) / PricePrev * 100;
                if (diff >= DiffLimit)
                {
                    diff = (Price - PriceNext) / PriceNext * 100;
                    if (diff >= DiffLimit)
                    {
                        if (Price > nMax[Bar - 1])
                        {
                            range = source.HighPrices[Bar] - source.LowPrices[Bar];
                            CloseLimit = source.LowPrices[Bar] + range * CloseKoef;
                            if (source.ClosePrices[Bar] <= CloseLimit) bCheck = true;
                        }
                    }
                }
            }
            return bCheck;
        }

        public bool CheckSpikeLow(int Bar, ISecurity source, IList<double> nMin, double DiffLimit, double CloseKoef, IContext ctx)
        {
            bool bCheck = false;
            double PricePrev = source.LowPrices[Bar - 1];
            double Price = source.LowPrices[Bar];
            double PriceNext = source.LowPrices[Bar + 1];
            double diff;
            double range;
            double CloseLimit;

            if (Price < PricePrev && Price < PriceNext)
            {
                diff = (PricePrev - Price) / PricePrev * 100;
                if (diff >= DiffLimit)
                {
                    diff = (PriceNext - Price) / PriceNext * 100;
                    if (diff >= DiffLimit)
                    {
                        if (Price < nMin[Bar - 1])
                        {
                            range = source.HighPrices[Bar] - source.LowPrices[Bar];
                            CloseLimit = source.HighPrices[Bar] - range * CloseKoef;
                            if (source.ClosePrices[Bar] >= CloseLimit) bCheck = true;
                        }
                    }
                }
            }
            return bCheck;
        }

        public void Execute(IContext ctx, ISecurity sec)
        {
            #region Params
            int barsCount = sec.Bars.Count;

            OptimProperty SLPeriodParam = new OptimProperty(#SLPeriodParam_Standart, #SLPeriodParam_Min, #SLPeriodParam_Max, #SLPeriodParam_Step);
            OptimProperty SLKoefParam = new OptimProperty(#SLKoefParam_Standart, #SLKoefParam_Min, #SLKoefParam_Max, #SLKoefParam_Step);
            OptimProperty SLLimitParam = new OptimProperty(#SLLimitParam_Standart, #SLLimitParam_Min, #SLLimitParam_Max, #SLLimitParam_Step);

            IList<double> nShortLow = ctx.GetData("ShortLow", new[] { SLPeriodParam.ToString() },
            delegate { return Series.Lowest(sec.LowPrices, SLPeriodParam); })
            ;

            OptimProperty LHKoefParam = new OptimProperty(#LHKoefParam_Standart, #LHKoefParam_Min, #LHKoefParam_Max, #LHKoefParam_Step);
            OptimProperty LHLimitParam = new OptimProperty(#LHLimitParam_Standart, #LHLimitParam_Min, #LHLimitParam_Max, #LHLimitParam_Step);
            OptimProperty LHPeriodParam = new OptimProperty(#LHPeriodParam_Standart, #LHPeriodParam_Min, #LHPeriodParam_Max, #LHPeriodParam_Step);

            IList<double> nLongHigh = ctx.GetData("LongHigh", new[] { LHPeriodParam.ToString() },
            delegate { return Series.Highest(sec.HighPrices, LHPeriodParam); });
            #endregion

            for (int bar = LHPeriodParam; bar < barsCount; bar++)
            {
                bBuy = false;
                bSell = false;
                bShort = false;
                bCover = false;

                var bCheckhigh = CheckSpikeHigh(bar, sec, nLongHigh, LHLimitParam, LHKoefParam, ctx);
                var bChecklow = CheckSpikeLow(bar, sec, nShortLow, SLLimitParam, SLKoefParam, ctx);
                if (bCheckhigh)
                {
                    new #Strategy1Name().Execute(ctx, sec);
                }
                else if (bChecklow)
                {
                    new #Strategy2Name().Execute(ctx, sec);
                }
            }
        }
    }
	
	#Strategy1
	
	#Strategy2
}