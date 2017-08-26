	//================================================================================
	public class NRMA 
	{
		public double StopBuyPrice, StopSellPrice, StopShortPrice, StopCoverPrice;
		public IPosition LongPos, ShortPos;
		 
		//================================================================================
		// функция вычисления индикатора NRMA (последовательность значений)
		// kShift - коэффициент смещения
		// kSharp - степень для усиления выраженности индикатора (2-3)
		public IList<double> GenNRMA(ISecurity source, double kShift, double kSharp)
		{
			#region Variables
			int Dir; // нарпавление индикатора NRTR (+1 вверх, -1 вниз)
			int MinPeriod; // минимальное значение периода для скользящей средней
			double MaxPrice, MinPrice, UpPrice, DownPrice;
			double vNRTR, vRatio, vNRMA;
			double vOSC, vOSC1, vOSC2; // последовательные значения для вычисления среднего

			IList<double> nNRMA = new List<double>(source.Bars.Count);
 			#endregion
			//--------------------------------------------------------------------------------
			#region Init vars
			Dir = 0;
			MinPeriod = 2;
			
			vOSC = 0;
			vOSC1 = 0;
			vOSC2 = 0;
			vNRMA = 0;
			#endregion
			//--------------------------------------------------------------------------------
			#region значения для первой свечи
			MaxPrice = source.HighPrices[0];	
			MinPrice = source.LowPrices[0];	
			UpPrice = MinPrice * (1 + kShift / 100);
			DownPrice = MaxPrice * (1 - kShift / 100);
			#endregion

			for (int bar = 0; bar < source.Bars.Count-1; bar++)
			{
				//--------------------------------------------------------------------------------
				#region calculate values
				int NewDir = Dir;
				double NewUpPrice = source.LowPrices[bar] * (1 + kShift / 100);
				double NewDownPrice = source.HighPrices[bar] * (1 - kShift / 100);
				
				// разворот последовательности
				// при движении вверх
				if (Dir > -1)
				{
					if (source.LowPrices[bar] < DownPrice)
					{
						NewDir = -1;	
						UpPrice = NewUpPrice;
					}	
				}
				// при движении вниз
				if (Dir < 1)
				{
					if (source.HighPrices[bar] > UpPrice)
					{
						NewDir = 1;	
						DownPrice = NewDownPrice;
					}	
				}
				Dir = NewDir;
				
				if ((Dir > -1) && (NewDownPrice > DownPrice)) DownPrice = NewDownPrice;
				if ((Dir < 1) && (NewUpPrice < UpPrice)) UpPrice = NewUpPrice;
			
				// значение индикатора NRTR
				// (принцип по аналогии с параболиком)
				vNRTR = DownPrice;
				if (Dir < 1) vNRTR = UpPrice;

				// значение vRatio - усреднение осцилятора (на 3 бара) и возведение в степень kSharp
				vOSC2 = vOSC1;
				vOSC1 = vOSC;
				vOSC = (100 * Math.Abs(source.ClosePrices[bar] - vNRTR) / source.ClosePrices[bar]) / kShift;
				if (bar == 0)
				{
					vOSC1 = vOSC;
					vOSC2 = vOSC;
					vNRMA = source.ClosePrices[bar];
				}
				vRatio = Math.Pow((vOSC + vOSC1 + vOSC2) / 3, kSharp);
			
				// значение NRMA
				double Factor = 2.0 / (1 + MinPeriod);
				vNRMA = vNRMA + vRatio * Factor * (source.ClosePrices[bar] - vNRMA);
				
				#endregion
				//--------------------------------------------------------------------------------
				// добавление нового значения в последовательность
				nNRMA.Add(vNRMA);
			}
			return nNRMA;
		}
		
		//================================================================================
		// Параметры оптимизации - для примера задан только 1 
		// также могут быть заданы другие параметры (kShift, kSharp  и т.д.)
		// ParamShift - параметр оптимизации для коэффицента смещения на вход
		public OptimProperty ParamShift = new OptimProperty(3.4, 0.2, 20, 0.2);

		//================================================================================
		public virtual void Execute(IContext ctx, ISecurity source)
		{
			int StartBar = 0;

			#region Variables
			int MDir; // напраление адаптивной скользящей средней (+1 вверх, -1 вниз)
			int StdPeriod; // период для определения стандартного отклонения
			double HighRange, LowRange; // границы диапазона для определения сигналов
			double kShift; // коэффициент смещения для расчета NRMA
			double kMShift; // коэффициент смещения для определения границ диапазона
			double kStd; // коэффициент для стандартного (среднеквадратичного) отклонения
			double kSharp; // степень для усиления выраженности индикатора NRTR
			double vNRMA; // значение NRMA для текущего бара
			double vPrevNRMA; // значение NRMA для предыдущего бара
			#endregion
			//--------------------------------------------------------------------------------
			#region Init vars
			MDir = 0;
			kShift = 10;
			kSharp = 2;
			kMShift = 1;
			StdPeriod = 14;
			kStd = 0.7;
			
			// массив значений для вычисления среднеквадратичного отклонения
			double[] aMA = new double[StdPeriod];
			
			HighRange = source.HighPrices[StartBar];
			LowRange = source.LowPrices[StartBar];
			#endregion
			//--------------------------------------------------------------------------------
			// Obtain parameters
			kMShift = ParamShift;

			// серия значений индикатора NRMA
			// кэширование с учетом параметров kShift и kSharp
			IList<double> nNRMA = ctx.GetData("NRMA", new[] {kShift.ToString()+"_"+kSharp.ToString()},
			delegate { return GenNRMA(source, kShift, kSharp); });
			
			// серии значений границ диапазона
			IList<double> nHighRange = new List<double>(source.Bars.Count);
			IList<double> nLowRange = new List<double>(source.Bars.Count);
			
			//================================================================================
			#region основной цикл - проход по барам
			int barsCount = source.Bars.Count;
			vNRMA = nNRMA[0];
			for (int bar = 0; bar < barsCount; bar++)
			{
				//--------------------------------------------------------------------------------
				#region calculate values
				// значение NRMA
				vPrevNRMA = vNRMA;
				vNRMA = nNRMA[bar];

				// определение среднеквадратичнонго отклонения
				for (int i=1; i < StdPeriod; i++) aMA[i-1] = aMA[i];
				aMA[StdPeriod-1] = vNRMA;
			
				double sum = 0;
				for (int i=0; i < StdPeriod; i++) sum = sum + aMA[i];
				double avg = sum / StdPeriod;
				sum = 0;
				for (int i=0; i < StdPeriod; i++) sum = sum + Math.Pow((aMA[i]-avg), 2);
				double std = Math.Pow(sum, 0.5);
				// смещение границ диапазона от скользящей средней
				double MShift = kMShift * std * kStd;
			
				// изменение направления индикатора NRMA
				// при движении вверх
				if (MDir > -1)
				{
					if (vNRMA < vPrevNRMA) MDir = -1;	
					if (MDir > -1) LowRange = vNRMA * (1 - MShift / 100);
				}
				// при движении вниз
				if (MDir < 1)
				{
					if (vNRMA > vPrevNRMA) MDir = 1;	
					if (MDir < 1) HighRange = vNRMA * (1 + MShift / 100);
				}
				#endregion
				//--------------------------------------------------------------------------------
				#region data series
				// добавление новых значений в последовательности
				if (bar == 0)
				{
					// смещение значений на один бар для соответствия стопов на графике
					nHighRange.Add(HighRange);
					nLowRange.Add(LowRange);
				}
				nHighRange.Add(HighRange);
				nLowRange.Add(LowRange);
				#endregion
				//--------------------------------------------------------------------------------
				#region generate signals
				// сброс значений сигналов
				StopBuyPrice = 0;
				StopSellPrice = 0;
				StopShortPrice = 0;
				StopCoverPrice = 0;
				
				// установка сигналов по условиям
				// если направление вверх
				if (MDir > 0)
				{
					StopBuyPrice = HighRange;
					StopCoverPrice = HighRange;
				}
				// если направление вниз
				if (MDir < 0) 
				{
					StopSellPrice = LowRange;
					StopShortPrice = LowRange;
				}
				#endregion
				//================================================================================
				#region execute signals
				//--------------------------------------------------------------------------------
				// выполнение сигналов для длинной позиции
				IPosition LongPos = source.Positions.GetLastActiveForSignal("LN");
				if (LongPos == null)
				{
					// Если нет активной длинной позиции
					if (StopBuyPrice > 0)
					{
						// Если есть сигнал StopBuy, 
						// выдаем стоп-ордер на открыте новой длинной позиции.
						source.Positions.BuyIfGreater(bar+1, 1, StopBuyPrice, "LN");
					}
				}
				else
				{
					// Если есть активная длинная позиция 
					if (StopSellPrice > 0)
					{
						// Если есть сигнал StopSell, 
						// выдаем стоп-ордер на закрыте длинной позиции.
						LongPos.CloseAtStop(bar+1, StopSellPrice, "LX");
					}
				}
				//--------------------------------------------------------------------------------
				// выполнение сигналов для короткой позиции
				IPosition ShortPos = source.Positions.GetLastActiveForSignal("SN");
				if (ShortPos == null)
				{
					// Если нет активной короткой позиции
					if (StopShortPrice > 0)
					{
						// Если есть сигнал StopShort
						// выдаем стоп-ордер на открыте новой короткой позиции.
						source.Positions.SellIfLess(bar+1, 1, StopShortPrice, "SN");
			
					}
				}
				else
				{
					// Если есть активная короткая позиция, 
					if (StopCoverPrice > 0)
					{
						// Если есть сигнал StopCover
						// выдаем стоп-ордер на закрыте короткой позиции.
						ShortPos.CloseAtStop(bar+1, StopCoverPrice, "SX");
					}
				}
				#endregion
			}
			#endregion
			//================================================================================
			#region прорисовка графиков
			// Берем основную панель (Pane)
			IPane mainPane = ctx.First;
  
			// Отрисовка
			mainPane.AddList("NRMA", nNRMA, ListStyles.LINE, 0xa000a0, LineStyles.SOLID, PaneSides.RIGHT);
			mainPane.AddList("HighRange", nHighRange, ListStyles.LINE, 0x0000a0, LineStyles.DOT, PaneSides.RIGHT);
			mainPane.AddList("LowRange", nLowRange, ListStyles.LINE, 0xa00000, LineStyles.DOT, PaneSides.RIGHT);
			#endregion
			//--------------------------------------------------------------------------------
		}
	}