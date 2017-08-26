
	//================================================================================
	public class Supertrend
	{
		// используем переменные-флаги для сигналов
		public bool bBuy; // флаг сигнала открытия длинной позиции
		public bool bSell;
		public bool bShort;
		public bool bCover;
		public IPosition LongPos, ShortPos;
		 
		//================================================================================
		// функция вычисления ATR (последовательность значений)
		// Period -  период скользящей средней для расчета ATR
		public IList<double> GenATR(ISecurity source, int Period)
		{
			double TrueRange; // значение "истинного диапазона"
			double vATR; // значение ATR для текущего бара

			// серия значений ATR
			IList<double> nATR = new List<double>(source.Bars.Count);

			TrueRange = source.HighPrices[0] - source.LowPrices[0];
			vATR = TrueRange;

			for (int bar = 0; bar < source.Bars.Count; bar++)
			{

				TrueRange = source.HighPrices[bar] - source.LowPrices[bar];
				if (bar > 0) 
				{
					if (source.LowPrices[bar] > source.ClosePrices[bar-1])
						TrueRange	= TrueRange + (source.LowPrices[bar] - source.ClosePrices[bar-1]);
					if (source.HighPrices[bar] < source.ClosePrices[bar-1])
						TrueRange	= TrueRange + (source.ClosePrices[bar-1] - source.HighPrices[bar]);
				}
				vATR = vATR + (TrueRange - vATR) / Period;
				//--------------------------------------------------------------------------------
				// добавление нового значения в последовательность
				nATR.Add(vATR);
			}
			return nATR;
		}
		
		//================================================================================
		// Параметры оптимизации
		public OptimProperty PeriodParam = new OptimProperty(20, 2, 20, 2);
		public OptimProperty MultiplierParam = new OptimProperty(9, 1, 10, 0.5);

		//================================================================================
		public virtual void Execute(IContext ctx, ISecurity source)
		{
			#region Variables
			int Period; // период расчета ATR
			double vATR; // значение ATR для текущего бара
			int Dir; // направление тренда (+1 вверх, -1 вниз)
			int PrevDir; // предыдущее направление тренда
			double Up; // значение верхней границы для текущего бара
			double Down; // значение нижней границы для текущего бара
			double Multiplier; // множитель
			
			double vTrend;
			double PrevUp, PrevDown;
			double Price; // цена закрытия текущего бара
			double AvgPrice; // средняя цена дл ятекущего бара
			
			// серия значений верхней границы
			IList<double> nHighRange = new List<double>(source.Bars.Count);
			// серия значений нижней границы
			IList<double> nLowRange = new List<double>(source.Bars.Count);
			// серия значений для фильтра тренда
			IList<double> nDir = new List<double>(source.Bars.Count);
			// серия значений индикатора тренда
			IList<double> nTrend = new List<double>(source.Bars.Count);
			#endregion
			//--------------------------------------------------------------------------------
			#region Init vars
			Dir = 0;
			Up = 0;
			Down = 0;
			 
			#endregion
			//--------------------------------------------------------------------------------
			#region Obtain parameters
			Period = PeriodParam;
			Multiplier = MultiplierParam;

			// серия значений ATR
			// кэширование с учетом параметра Period
			IList<double> nATR = ctx.GetData("ATR", new[] {Period.ToString()},
			delegate { return GenATR(source, Period); });

			#endregion
			//================================================================================
			#region основной цикл - проход по барам
			int barsCount = source.Bars.Count;
			vATR = source.ClosePrices[0];
			for (int bar = 0; bar < barsCount; bar++)
			{
				//--------------------------------------------------------------------------------
				#region calculate values
				Price = source.ClosePrices[bar];
				vATR = nATR[bar];
				vTrend = 0;

				PrevUp = Up;
				PrevDown = Down;
				PrevDir = Dir;

				AvgPrice = (source.HighPrices[bar] + source.LowPrices[bar]) / 2;
				Up = AvgPrice + Multiplier * vATR;
				Down = AvgPrice - Multiplier * vATR;
				
				if (Price > PrevUp) Dir = 1;
				if (Price < PrevDown) Dir = -1;
				
				if (Dir > 0 && Down < PrevDown) Down = PrevDown;
				if (Dir < 0 && Up > PrevUp) Up = PrevUp;

				if (Dir > 0 && PrevDir < 0) Down = AvgPrice - Multiplier * vATR;
				if (Dir < 0 && PrevDir > 0) Up = AvgPrice + Multiplier * vATR;
					
				if (Dir == 1) vTrend = Down;
				if (Dir == -1) vTrend = Up;
				

				#endregion
				//--------------------------------------------------------------------------------
				#region data series
				nHighRange.Add(Up);
				nLowRange.Add(Down);
				nATR.Add(vATR);
				nDir.Add(Dir);
				nTrend.Add(vTrend);
				
				#endregion
				//--------------------------------------------------------------------------------
				#region generate signals
				// сброс значений сигналов
				bBuy = false;
				bSell = false;
				bShort = false;
				bCover = false;
				
				// установка сигналов по условиям
				if (Dir > 0 && PrevDir <= 0)
				{
					bBuy = true;
					bCover = true;
				}
				if (Dir < 0 && PrevDir >= 0) 
				{
					bShort = true;
					bSell = true;
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
 
			// Отрисовка верхней и нижней границы условных прямоугольников 
			//mainPane.AddList("HighRange", nHighRange, ListStyles.LINE, 0x0000a0, LineStyles.DOT, PaneSides.RIGHT);
			//mainPane.AddList("LowRange", nLowRange, ListStyles.LINE, 0xa00000, LineStyles.DOT, PaneSides.RIGHT);

			mainPane.AddList("Trend Indicator", nTrend, ListStyles.LINE, 0xa000a0, LineStyles.DOT, PaneSides.RIGHT);
			
			// Создаем дополнительную панель для ATR.
			IPane ATRPane = ctx.CreatePane("ATR", 10, false, false);

			// Отрисовка графика ATR
			ATRPane.AddList(string.Format("ATR"), nATR, ListStyles.LINE,
			0x5050a0, LineStyles.SOLID, PaneSides.RIGHT);
			
			// Создаем дополнительную панель для филтра тренда.
			IPane FilterPane = ctx.CreatePane("Filtr", 10, false, false);

			// Отрисовка графика фильтра тренда
			FilterPane.AddList(string.Format("TREND"), nDir, ListStyles.HISTOHRAM_FILL,
			0x00ff00, LineStyles.SOLID, PaneSides.RIGHT);

			#endregion
			//--------------------------------------------------------------------------------

			}
		}