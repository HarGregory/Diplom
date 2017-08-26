
	//================================================================================
	public class Envelope 
	{
		// используем переменные-флаги для сигналов
		public bool bBuy; // флаг сигнала открытия длинной позиции
		public bool bSell; // флаг сигнала закрытия длинной позиции
		public bool bShort; // флаг сигнала открытия короткой позиции
		public bool bCover; // флаг сигнала закрытия короткой позиции
		public IPosition LongPos, ShortPos;
		 
		//================================================================================
		// функция вычисления верхней границы конверта (последовательность значений)
		// nSMA - серия значений простой скользящей средней
		// Shift - смещение границы от скользящей средней
		public IList<double> GenHighRange(ISecurity source, IList<double> nSMA, double Shift)
		{
			double vHighRange = 0;
			// серия значений HighRange
			IList<double> nHighRange = new List<double>(source.Bars.Count);

			for (int bar = 0; bar < source.Bars.Count; bar++)
			{
				vHighRange = nSMA[bar] * (1 + Shift);
				//--------------------------------------------------------------------------------
				// добавление нового значения в последовательность
				nHighRange.Add(vHighRange);
			}
			return nHighRange;
		}
		
		//================================================================================
		// функция вычисления нижней границы конверта (последовательность значений)
		// nSMA - серия значений простой скользящей средней
		// Shift - смещение границы от скользящей средней
		public IList<double> GenLowRange(ISecurity source, IList<double> nSMA, double Shift)
		{
			double vLowRange = 0;
			// серия значений LowRange
			IList<double> nLowRange = new List<double>(source.Bars.Count);

			for (int bar = 0; bar < source.Bars.Count; bar++)
			{
				vLowRange = nSMA[bar] * (1 - Shift);
				//--------------------------------------------------------------------------------
				// добавление нового значения в последовательность
				nLowRange.Add(vLowRange);
			}
			return nLowRange;
		}
		
		//================================================================================
		// Параметры оптимизации
		// параметра для длинных позиций
		public OptimProperty LongPeriodParam = new OptimProperty(22, 2, 50, 2);
		public OptimProperty LongShiftParam = new OptimProperty(0.01, 0.01, 0.1, 0.005);
		// параметра для коротких позиций
		public OptimProperty ShortPeriodParam = new OptimProperty(24, 2, 50, 2);
		public OptimProperty ShortShiftParam = new OptimProperty(0.01, 0.01, 0.1, 0.005);

		//================================================================================
		public virtual void Execute(IContext ctx, ISecurity source)
		{
			int StartBar = 0;

			#region Variables
			// для длинных позиций
			int LongPeriod; // период расчета простой скользящей средней SMA
			double LongShift; // смещение границ конверта от скользящей средней
			// для коротких длинных позиций
			int ShortPeriod; // период расчета простой скользящей средней SMA
			double ShortShift; // смещение границ конверта от скользящей средней
			
			#endregion
			//--------------------------------------------------------------------------------
			#region Obtain parameters
			LongPeriod = LongPeriodParam;
			LongShift = LongShiftParam;
			ShortPeriod = ShortPeriodParam;
			ShortShift = ShortShiftParam;

			StartBar = LongPeriod + 1;
			if (ShortPeriod > LongPeriod) StartBar = ShortPeriod + 1;

			// Вычисляем верхнюю и нижнюю границы конверта.
			// Используем GetData для кеширования данных и ускорения оптимизации.
			// для длинных позиций
			// серия для простой скользящей средней SMA
			IList<double> nLongSMA = ctx.GetData("LongSMA", new[] {LongPeriod.ToString()},
			delegate { return Series.SMA(source.ClosePrices, LongPeriod); });
			// серия значений верхней границы
			IList<double> nLongHighRange = ctx.GetData("LongHighRange", new[] {LongPeriod.ToString()+LongShift.ToString()},
			delegate { return GenHighRange(source, nLongSMA, LongShift); });
			// серия значений нижней границы
			IList<double> nLongLowRange = ctx.GetData("LongLowRange", new[] {LongPeriod.ToString()+LongShift.ToString()},
			delegate { return GenLowRange(source, nLongSMA, LongShift); });
			// для коротких позиций
			// серия для простой скользящей средней SMA
			IList<double> nShortSMA = ctx.GetData("ShortSMA", new[] {ShortPeriod.ToString()},
			delegate { return Series.SMA(source.ClosePrices, ShortPeriod); });
			// серия значений верхней границы
			IList<double> nShortHighRange = ctx.GetData("ShortHighRange", new[] {ShortPeriod.ToString()+ShortShift.ToString()},
			delegate { return GenHighRange(source, nShortSMA, ShortShift); });
			// серия значений нижней границы
			IList<double> nShortLowRange = ctx.GetData("ShortLowRange", new[] {ShortPeriod.ToString()+ShortShift.ToString()},
			delegate { return GenLowRange(source, nShortSMA, ShortShift); });
			
			#endregion
			//================================================================================
			#region основной цикл - проход по барам
			int barsCount = source.Bars.Count;
			for (int bar = StartBar; bar < barsCount; bar++)
			{
				//--------------------------------------------------------------------------------
				#region generate signals
				// сброс значений сигналов
				bBuy = false;
				bSell = false;
				bShort = false;
				bCover = false;
				
				// установка сигналов по условиям
				// для длинных позиций
				if (source.ClosePrices[bar-1] > nLongHighRange[bar-1])
				{
					if (source.ClosePrices[bar] > source.ClosePrices[bar-1]) bBuy = true;
				}
				if (source.ClosePrices[bar] < nLongLowRange[bar]) bSell = true;
				// для коротких позиций
				if (source.ClosePrices[bar-1] < nShortLowRange[bar-1])
				{
					if (source.ClosePrices[bar] < source.ClosePrices[bar-1]) bShort = true;
				}
				if (source.ClosePrices[bar] > nShortHighRange[bar]) bCover = true;
				#endregion	
				//================================================================================
				#region execute signals
				//--------------------------------------------------------------------------------
				// выполнение сигналов для длинной позиции
				IPosition LongPos = source.Positions.GetLastActiveForSignal("LN");
				if (LongPos == null)
				{
					// Если нет активной длинной позиции
					if (bBuy)
					{
						// Если есть сигнал Buy, 
						// выдаем ордер на открыте новой длинной позиции.
						source.Positions.BuyAtMarket(bar+1, 1, "LN");
					}
				}
				else
				{
					// Если есть активная длинная позиция 
					if (bSell)
					{
						// Если есть сигнал Sell, 
						// выдаем ордер на закрыте длинной позиции.
						LongPos.CloseAtMarket(bar+1, "LX");
					}
				}
				//--------------------------------------------------------------------------------
				// выполнение сигналов для короткой позиции
				IPosition ShortPos = source.Positions.GetLastActiveForSignal("SN");
				if (ShortPos == null)
				{
					// Если нет активной короткой позиции
					if (bShort)
					{
						// Если есть сигнал Short
						// выдаем ордер на открыте новой короткой позиции.
						source.Positions.SellAtMarket(bar+1, 1, "SN");
			
					}
				}
				else
				{
					// Если есть активная короткая позиция, 
					if (bCover)
					{
						// Если есть сигнал Cover
						// выдаем ордер на закрыте короткой позиции.
						ShortPos.CloseAtMarket(bar+1, "SX");
					}
				}
				#endregion
			}
			#endregion
			//================================================================================
			#region прорисовка графиков
			// Берем основную панель (Pane)
			IPane mainPane = ctx.First;
 
			// для длинных позиций
			// Отрисовка простой скользящей средней
			mainPane.AddList("LongSMA", nLongSMA, ListStyles.LINE, 0x00a0a0, LineStyles.DOT, PaneSides.RIGHT);
			// Отрисовка верхней и нижней границы конверта
			mainPane.AddList("LongHighRange", nLongHighRange, ListStyles.LINE, 0x0000a0, LineStyles.DOT, PaneSides.RIGHT);
			mainPane.AddList("LongLowRange", nLongLowRange, ListStyles.LINE, 0xf07070, LineStyles.DOT, PaneSides.RIGHT);

			// для коротких позиций
			// Отрисовка простой скользящей средней
			mainPane.AddList("ShortSMA", nShortSMA, ListStyles.LINE, 0xa000a0, LineStyles.DOT, PaneSides.RIGHT);
			// Отрисовка верхней и нижней границы конверта
			mainPane.AddList("ShortHighRange", nShortHighRange, ListStyles.LINE, 0x7070f0, LineStyles.DOT, PaneSides.RIGHT);
			mainPane.AddList("ShortLowRange", nShortLowRange, ListStyles.LINE, 0xa00000, LineStyles.DOT, PaneSides.RIGHT);

			#endregion
			//--------------------------------------------------------------------------------
		}
	}