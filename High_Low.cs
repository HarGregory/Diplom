public class High_Low 
	{
		// Параметры оптимизации задаются при помощи типа OptimProperty.
		// Параметры оптимизации для длинных позиций 
		public OptimProperty High1Period = new OptimProperty(20, 10, 100, 5);
		public OptimProperty Low1Period = new OptimProperty(10, 10, 100, 5);

		// Параметры оптимизации для коротких позици
		public OptimProperty Low2Period = new OptimProperty(20, 10, 100, 5);
		public OptimProperty High2Period = new OptimProperty(10, 10, 100, 5);

		// Параметры закрытия по скользящему стопу
		public OptimProperty LongStop = new OptimProperty(50, 1, 200, 1);
		public OptimProperty ShortStop = new OptimProperty(50, 1, 200, 1);

		public virtual void Execute(IContext ctx, ISecurity source)
		{
			#region Вычисляем максимумы и минимумы.
			// Используем GetData для кеширования данных и ускорения оптимизация.
			// При неиспользовании кэша увеличивается объем обрабатываемых данных, что ведет к сильному замедлению оптимизации.
			// Следует учесть, что необходимо перечислить абсолютно все изменяемые переменные используемые в вычислениях.
			// Не соблюдение этого правила приведет к некорректной работе и результатам оптимизации.
			IList<double> high1 = ctx.GetData("Highest", new[] {High1Period.ToString()},
			delegate { return Series.Highest(source.HighPrices, High1Period); });
			IList<double> low1 = ctx.GetData("Lowest", new[] {Low1Period.ToString()},
			delegate { return Series.Lowest(source.LowPrices, Low1Period); });
			IList<double> high2 = ctx.GetData("Highest", new[] {High2Period.ToString()},
			delegate { return Series.Highest(source.HighPrices, High2Period); });
			IList<double> low2 = ctx.GetData("Lowest", new[] {Low2Period.ToString()},
			delegate { return Series.Lowest(source.LowPrices, Low2Period); });
 
			#endregion
			// =================================================
			#region Variables
			double MaxPrice; // максимальная цена с момента открытия длинной позиции
			double MinPrice; // минимальная цена с момента открытия короткой позиции
			double LongStopPrice; // цена скользящего стопа длинной позиции
			double ShortStopPrice; // цена скользящего стопа короткой позиции
			
			// серия значений стоп-цены длинной позиции
			IList<double> nLongStopPrice = new List<double>(source.Bars.Count);
			// серия значений стоп-цены короткой позиции
			IList<double> nShortStopPrice = new List<double>(source.Bars.Count);

			#endregion
			//--------------------------------------------------------------------------------
			#region Init vars
			// начальные значения переменным задавать не обязательно
			// при открытии позиции переменным задаются реальные значения
			MaxPrice = source.HighPrices[0];	
			MinPrice = source.LowPrices[0];
			LongStopPrice = MinPrice;
			ShortStopPrice = MaxPrice;
			#endregion
			// =================================================
			#region прорисовка графиков
			// Берем основную панель (Pane).
			IPane mainPane = ctx.First;
		 
			// Отрисовка графиков.
			mainPane.AddList(string.Format("High1({0}) [{1}]", High1Period, source.Symbol), high1, ListStyles.LINE,
			0x00ff00, LineStyles.SOLID, PaneSides.RIGHT);
			mainPane.AddList(string.Format("Low1({0}) [{1}]", Low1Period, source.Symbol), low1, ListStyles.LINE,
			0xff0000, LineStyles.SOLID, PaneSides.RIGHT);
			mainPane.AddList(string.Format("Low2({0}) [{1}]", Low2Period, source.Symbol), low2, ListStyles.LINE,
			0xff0000, LineStyles.DASH, PaneSides.RIGHT);
			mainPane.AddList(string.Format("High2({0}) [{1}]", High2Period, source.Symbol), high2, ListStyles.LINE,
			0x00ff00, LineStyles.DASH, PaneSides.RIGHT);
 			#endregion
			// =================================================
			#region Основной цикл обработки (торговля).
			
			int barsCount = source.Bars.Count;
			for (int bar = 0; (bar < barsCount); bar++)
			{
				// выполнение сигналов для длинной позиции
				IPosition LongPos = source.Positions.GetLastActiveForSignal("LN");
				if (LongPos == null)
				{
					// Если нет активной длинной позиции, 
					// выдаем условный ордер на открыте новой длинной позиции.
					source.Positions.BuyIfGreater(bar + 1, 1, high1[bar], "LN");
					MaxPrice = source.HighPrices[bar];
				}
				else
				{
					// Если есть активная длинная позиция, 
					// вычисляем стоп-цену по максимуму
					LongStopPrice = MaxPrice * (1 - LongStop * 1.0 / 1000);
					// или по нижнему краю ценового канала
					if (low1[bar] > LongStopPrice) LongStopPrice = low1[bar];
					// выдаем условный ордер на закрыте длинной позиции.
					LongPos.CloseAtStop(bar + 1, LongStopPrice, "LX");
					// сдвигаем максимум после отработки стоп-ордера
					if (source.HighPrices[bar] > MaxPrice) MaxPrice = source.HighPrices[bar];
				}
				//--------------------------------------------------------------------------------
				// выполнение сигналов для короткой позиции
				IPosition ShortPos = source.Positions.GetLastActiveForSignal("SN");
				if (ShortPos == null)
				{
					// Если нет активной короткой позиции, 
					// выдаем условный ордер на открыте новой короткой позиции.
					source.Positions.SellIfLess(bar + 1, 1, low2[bar], "SN");
					MinPrice = source.LowPrices[bar];
				}
				else
				{
					// Если есть активная короткая позиция,
					// вычисляем стоп-цену по минимуму
					ShortStopPrice = MinPrice * (1 + ShortStop * 1.0 / 1000);
					// или по верхнему краю ценового канала
					if (high2[bar] < ShortStopPrice) ShortStopPrice = high2[bar];
					// выдаем условный ордер на закрыте короткой позиции.
					ShortPos.CloseAtStop(bar + 1, ShortStopPrice, "SX");
					// сдвигаем минимум после отработки стоп-ордера
					if (source.LowPrices[bar] < MinPrice) MinPrice = source.LowPrices[bar];
				}
				
				// добавляем значения стопов для текущего бара в соответствующие серии
				nLongStopPrice.Add(LongStopPrice);
				nShortStopPrice.Add(ShortStopPrice);
			}
			#endregion
			// =================================================
			#region прорисовка стопов
			mainPane.AddList("LongStop", nLongStopPrice, ListStyles.LINE, 0xff00a0, LineStyles.SOLID, PaneSides.RIGHT);
			mainPane.AddList("ShortStop", nShortStopPrice, ListStyles.LINE, 0xa000ff, LineStyles.SOLID, PaneSides.RIGHT);
			#endregion
		}
	}