namespace Japanese
{

	//================================================================================
	public class Japanese : IExternalScript
	{
		// используем переменные-флаги для сигналов
		public bool bBuy; // флаг сигнала открытия длинной позиции
		public bool bSell; // флаг сигнала закрытия длинной позиции
		public bool bShort; // флаг сигнала открытия короткой позиции
		public bool bCover; // флаг сигнала закрытия короткой позиции
		public IPosition LongPos, ShortPos;
		public int LongEntryBar, ShortEntryBar;
		
		public bool LongCond1, LongCond2, LongCond3, LongCond4, LongCond5;
		public bool ShortCond1, ShortCond2, ShortCond3, ShortCond4, ShortCond5;
		
		public IList<double> nATR; // серия значений ATR
			
		// сокращенные переменные для рабочих значений 
		public double O, H, L, C, O1, H1, L1, C1, O2, H2, L2, C2, O3, H3, L3, C3;
		public double O4, H4, L4, C4, O5, H5, L5, C5, O6, H6, L6, C6, O7, H7, L7, C7;
		public double O8, H8, L8, C8, O9, H9, L9, C9, O10, H10, L10, C10;
		public double O11, H11, L11, C11, O12, H12, L12, C12, O13, H13, L13, C13;
		public double O14, H14, L14, C14, O15, H15, L15, C15, O16, H16, L16, C16;
		public double O17, H17, L17, C17, O18, H18, L18, C18, O19, H19, L19, C19;
		public double O21, H21, L21, C21, O110, H110, L110, C110;
		
		public double OHL, CHL, OHL1, CHL1, OHL2, CHL2, OHL3, CHL3;
		public double OHL4, CHL4, OHL5, CHL5, OHL6, CHL6, OHL11, CHL11;
		public double A, A1, A2;
		
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
		// функция распознавания бычьего шаблона ThreeOutsideUp
		public bool BullishThreeOutsideUp(ISecurity source, int bar)
		{
			bool bMatch = false;

			//--------------------------------------------------------------------------------
			// определение сокращений
			O7 = source.OpenPrices[bar] - source.LowPrices[bar];
			H7 = source.HighPrices[bar] - source.LowPrices[bar];
			L7 = source.LowPrices[bar];
			C7 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O15 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H15 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L15 = source.LowPrices[bar-1];
			C15 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			O21 = source.OpenPrices[bar-2] - source.LowPrices[bar-2];
			H21 = source.HighPrices[bar-2] - source.LowPrices[bar-2];
			L21 = source.LowPrices[bar-2];
			C21 = source.ClosePrices[bar-2] - source.LowPrices[bar-2];
			A1 = nATR[bar-1] * 0.5;
			
			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (O21 > C21) //1st day black line --- Start Bullish Engulfing pattern
			// (O15 < C15) //2nd day white line
			// (O21 + L21 < C15 + L15) //1st day Open < 2nd day Close
			// (C21 + L21 > O15 + L15) //1st day Close > 2nd day Open
			// (H15 > ATR(Bar-1,10) * 0.5) //2nd day long line --- End Bullish Engulfing pattern
			// (C7 > O7) //3rd day white line
			// (C7 + L7 > C15 + L15); //3rd day closes above 2nd day Close

			bMatch = (O21>C21)&&(O15<C15)&&(O21+L21<C15+L15)&&(C21+L21>O15+L15)&&(H15>A1)&&(C7>O7)&&(C7+L7>C15+L15);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания бычьего шаблона PiercingLine
		public bool BullishPiercingLine(ISecurity source, int bar)
		{
			bool bMatch = false;

			//--------------------------------------------------------------------------------
			// определение сокращений
			O8 = source.OpenPrices[bar] - source.LowPrices[bar];
			H8 = source.HighPrices[bar] - source.LowPrices[bar];
			L8 = source.LowPrices[bar];
			C8 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O16 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H16 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L16 = source.LowPrices[bar-1];
			C16 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A = nATR[bar] * 0.5;
			
			OHL4 = 0; 
			CHL4 = 0;
			if (H8 != 0) { OHL4 = O8/H8; CHL4 = C8/H8; }
			OHL11 = 0; 
			CHL11 = 0;
			if (H16 != 0) { OHL11 = O16/H16; CHL11 = C16/H16; }

			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (OHL11 > 0.70) and (CHL11 < 0.30) //1st day long black
			// (OHL4 < 0.30) and (CHL4 > 0.70) //2nd day long white
			// (O8 + L8 < L16) //1st day Open > 2nd day Low
			// (C8 + L8 > (O16 + C16) / 2 + C16 + L16) //2nd day Close > 1st day midpoint
			// (Min(H8, H16) > ATR(Bar, 10) * 0.5); //2 long lines

			bMatch = (OHL11>0.70)&&(CHL11<0.30)&&(OHL4<0.30)&&(CHL4>0.70)&&(O8+L8<L16)&&(C8+L8>(O16+C16)/2+C16+L16)&&(Math.Min(H8,H16)>A);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания бычьего шаблона EngulfingLines
		public bool BullishEngulfingLines(ISecurity source, int bar)
		{
			bool bMatch = false;

			//--------------------------------------------------------------------------------
			// определение сокращений
			O9 = source.OpenPrices[bar] - source.LowPrices[bar];
			H9 = source.HighPrices[bar] - source.LowPrices[bar];
			L9 = source.LowPrices[bar];
			C9 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O17 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H17 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L17 = source.LowPrices[bar-1];
			C17 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A1 = nATR[bar-1] * 0.5;
			
			OHL4 = 0; 
			CHL4 = 0;
			if (H8 != 0) { OHL4 = O8/H8; CHL4 = C8/H8; }
			OHL11 = 0; 
			CHL11 = 0;
			if (H16 != 0) { OHL11 = O16/H16; CHL11 = C16/H16; }

			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (O17 > C17) //1st day black line
			// (O9 < C9) //2nd day white line
			// (O17 + L17 < C9 + L9) //1st day Open < 2nd day Close
			// (C17 + L17 > O9 + L9) //1st day Close > 2nd day Open
			// (H9 > ATR(Bar-1,10) * 0.5); //2nd day long line

			bMatch = (O17>C17)&&(O9<C9)&&(O17+L17<C9+L9)&&(C17+L17>O9+L9)&&(H9>A1);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания бычьего шаблона Harami
		public bool BullishHarami(ISecurity source, int bar)
		{
			bool bMatch = false;

			//--------------------------------------------------------------------------------
			// определение сокращений
			O10 = source.OpenPrices[bar] - source.LowPrices[bar];
			H10 = source.HighPrices[bar] - source.LowPrices[bar];
			L10 = source.LowPrices[bar];
			C10 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O18 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H18 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L18 = source.LowPrices[bar-1];
			C18 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A1 = nATR[bar-1] * 0.5;
			
			OHL5 = 0; 
			CHL5 = 0;
			if (H10 != 0) { OHL5 = O10/H10; CHL5 = C10/H10; }

			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (C18 < O18) //1st day black line
			// (C10 > O10) //2nd day white line
			// (CHL5 - OHL5 > 0.1) //2nd day no Doji
			// (H10 < ATR(Bar-1, 10) * 0.5) //2nd day short line
			// (H18 > ATR(Bar-1, 10) * 0.5) //1st day long line
			// (O18 + L18 > C10 + L10) //1st day Open > 2nd day Close
			// (C18 + L18 < O10 + L10); //1st day Close < 2nd day Open

			bMatch = (C18<O18)&&(C10>O10)&&(CHL5-OHL5>0.1)&&(H10<A1)&&(H18>A1)&&(O18+L18>C10+L10)&&(C18+L18<O10+L10);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания бычьего шаблона BeltHold
		public bool BullishBeltHold(ISecurity source, int bar)
		{
			bool bMatch = false;

			//--------------------------------------------------------------------------------
			// определение сокращений
			O19 = source.OpenPrices[bar] - source.LowPrices[bar];
			H19 = source.HighPrices[bar] - source.LowPrices[bar];
			L19 = source.LowPrices[bar];
			C19 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O110 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H110 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L110 = source.LowPrices[bar-1];
			C110 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A1 = nATR[bar-1] * 0.5;
			
			OHL6 = 0; 
			CHL6 = 0;
			if (H19 != 0) { OHL6 = O19/H19; CHL6 = C19/H19; }

			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (CHL6 > 0.7) and (OHL6 < 0.05) //white day where Open = Low
			// (H19 > ATR(Bar-1, 10) * 0.5) //long line
			// (L110 > C19 + L19); //significant gap down (based on Close!) below yesterday's
        
			bMatch = (CHL6>0.7)&&(OHL6<0.05)&&(H19>A1)&&(L110>C19+L19);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания медвежьего шаблона ThreeOutsideDown
		public bool BearishThreeOutsideDown(ISecurity source, int bar)
		{
			bool bMatch = false;
			
			//--------------------------------------------------------------------------------
			// определение сокращений
			O = source.OpenPrices[bar] - source.LowPrices[bar];
			H = source.HighPrices[bar] - source.LowPrices[bar];
			L = source.LowPrices[bar];
			C = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O1 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H1 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L1 = source.LowPrices[bar-1];
			C1 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			O2 = source.OpenPrices[bar-2] - source.LowPrices[bar-2];
			H2 = source.HighPrices[bar-2] - source.LowPrices[bar-2];
			L2 = source.LowPrices[bar-2];
			C2 = source.ClosePrices[bar-2] - source.LowPrices[bar-2];
			A1 = nATR[bar-1] * 0.5;
			
			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (O1 < C1) //1st day white line --- Start Bearish Engulfing pattern
			// (O > C) //2nd day black line
			// (C1 + L1 < O + L) //1st day Close < 2nd day Open
			// (O1 + L1 > C + L) //1st day Open > 2nd day Close
			// (H > ATR(Bar-1, 10) * 0.5) //2nd day long line --- End Bearish Engulfing pattern
			// (C < O) //3rd day black line
			// (C + L < C1 + L1); //3rd day closes below 2nd day Close

			bMatch = (O1<C1)&&(O>C)&&(C1+L1<O+L)&&(O1+L1>C+L)&&(H>A1)&&(C<O)&&(C+L<C1+L1);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания медвежьего шаблона DarkCloudCover
		public bool BearishDarkCloudCover(ISecurity source, int bar)
		{
			bool bMatch = false;
			
			//--------------------------------------------------------------------------------
			// определение сокращений
			O3 = source.OpenPrices[bar] - source.LowPrices[bar];
			H3 = source.HighPrices[bar] - source.LowPrices[bar];
			L3 = source.LowPrices[bar];
			C3 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O11 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H11 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L11 = source.LowPrices[bar-1];
			C11 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A = nATR[bar] * 0.5;

			OHL = 0; 
			CHL = 0;
			if (H3 != 0) { OHL = O3/H3; CHL = C3/H3; }
			OHL1 = 0; 
			CHL1 = 0;
			if (H11 != 0) { OHL1 = O11/H11; CHL1 = C11/H11; }
			
			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (OHL1 < 0.30) and (OHL1 < 0.70) //1st day: long white bar
			// (OHL > 0.70) and (CHL < 0.30) //2nd day: long black bar
			// (O3 + L3 > H11 + L11) //2nd day Open > 1st day High
			// (C3 + L3 < (O11 + C11) / 2 + O11 + L11) // 2nd day Close < 1st day midpoint
			// (Min(H3, H11) > ATR(Bar, 10) * 0.5); //Long line
			
			bMatch = (OHL1<0.30)&&(OHL1<0.70)&&(OHL>0.70)&&(CHL<0.30)&&(O3+L3>H11+L11);
			bMatch = bMatch && (C3+L3<(O11+C11)/2+O11+L11)&&(Math.Min(H3,H11)>A);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания медвежьего шаблона EngulfingLines
		public bool BearishEngulfingLines(ISecurity source, int bar)
		{
			bool bMatch = false;
			
			//--------------------------------------------------------------------------------
			// определение сокращений
			O4 = source.OpenPrices[bar] - source.LowPrices[bar];
			H4 = source.HighPrices[bar] - source.LowPrices[bar];
			L4 = source.LowPrices[bar];
			C4 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O12 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H12 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L12 = source.LowPrices[bar-1];
			C12 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A1 = nATR[bar-1] * 0.5;
			
			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (O12 < C12) //1st day white line
			// (O4 > C4) //2nd day black line
			// (C12 + L12 < O4 + L4) //1st day Close < 2nd day Open
			// (O12 + L12 > C4 + L4) //1st day Open > 2nd day Close
			// (H4 > ATR(Bar-1,10) * 0.5); //2nd day long line

			bMatch = (O12<C12)&&(O4>C4)&&(C12+L12<O4+L4)&&(O12+L12>C4+L4)&&(H4>A1);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания медвежьего шаблона Harami
		public bool BearishHarami(ISecurity source, int bar)
		{
			bool bMatch = false;
			
			//--------------------------------------------------------------------------------
			// определение сокращений
			O5 = source.OpenPrices[bar] - source.LowPrices[bar];
			H5 = source.HighPrices[bar] - source.LowPrices[bar];
			L5 = source.LowPrices[bar];
			C5 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O13 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H13 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L13 = source.LowPrices[bar-1];
			C13 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A1 = nATR[bar-1] * 0.5;
			
			OHL2 = 0; 
			CHL2 = 0;
			if (H5 != 0) { OHL2 = O5/H5; CHL2 = C5/H5; }

			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (C13 > O13) //1st day white line
			// (C5 < O5) //2nd day black line
			// (OHL2 - CHL2 > 0.1) //2nd day no Doji
			// (H13 > ATR(Bar-1, 10) * 0.5) //1st day long line
			// (H5 < ATR(Bar-1, 10) * 0.5) //2nd day short line
			// (C13 + L13 > O5 + L5) //1st day Close > 2nd day Open
			// (O13 + L13 < C5 + L5); //1st day Open < 2nd day Close

			bMatch = (C13>O13)&&(C5<O5)&&(OHL2-CHL2>0.1)&&(H13>A1)&&(H5<A1)&&(C13+L13>O5+L5)&&(O13+L13<C5+L5);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// функция распознавания медвежьего шаблона BeltHold
		public bool BearishBeltHold(ISecurity source, int bar)
		{
			bool bMatch = false;
			
			//--------------------------------------------------------------------------------
			// определение сокращений
			O6 = source.OpenPrices[bar] - source.LowPrices[bar];
			H6 = source.HighPrices[bar] - source.LowPrices[bar];
			L6 = source.LowPrices[bar];
			C6 = source.ClosePrices[bar] - source.LowPrices[bar-1];
			O14 = source.OpenPrices[bar-1] - source.LowPrices[bar-1];
			H14 = source.HighPrices[bar-1] - source.LowPrices[bar-1];
			L14 = source.LowPrices[bar-1];
			C14 = source.ClosePrices[bar-1] - source.LowPrices[bar-1];
			A2 = nATR[bar-2] * 0.5;
			
			OHL3 = 0; 
			CHL3 = 0;
			if (H6 != 0) { OHL3 = O6/H6; CHL3 = C6/H6; }

			//--------------------------------------------------------------------------------
			// распознавание шаблона
			// (OHL3 > 0.95) and (CHL3 < 0.30) //black day where Open = High
			// (H6 > ATR(Bar-2, 10) * 0.5) //long line
			// (H14 + L14 < C6 + L6); //significant gap up above yesterday's high

			bMatch = (OHL3>0.95)&&(CHL3<0.30)&&(H6>A2)&&(H14+L14<C6+L6);
			
			//--------------------------------------------------------------------------------
			return bMatch;
		}
		
		//================================================================================
		// Параметры оптимизации
		public OptimProperty HoldParam = new OptimProperty(13, 1, 20, 1);
		public OptimProperty PatternParam = new OptimProperty(4, 1, 5, 1);
		public OptimProperty PeriodParam = new OptimProperty(95, 5, 100, 5);

		//================================================================================
		public virtual void Execute(IContext ctx, ISecurity source)
		{
			int StartBar = 0;
			LongPos = null;
			ShortPos = null;
			LongEntryBar = 0;
			ShortEntryBar = 0;
			
			#region Variables
			int HoldBars; // количество баров для нахождения в позиции
			int PatternNum; // номер шаблона для использования
			int Period; // период расчета ATR
			
			#endregion
			//--------------------------------------------------------------------------------
			#region Obtain parameters
			HoldBars = HoldParam;
			PatternNum = PatternParam;
			Period = PeriodParam;
			
			StartBar = HoldBars + 1;

			// серия значений ATR
			// кэширование с учетом параметра Period
			nATR = ctx.GetData("ATR", new[] {Period.ToString()},
			delegate { return GenATR(source, Period); });

			#endregion
			//================================================================================
			#region основной цикл - проход по барам
			int barsCount = source.Bars.Count;
			for (int bar = StartBar; bar < barsCount; bar++)
			{
				//--------------------------------------------------------------------------------
				#region calculate values
				LongCond1 = false;				
				LongCond2 = false;				
				LongCond3 = false;				
				LongCond4 = false;				
				LongCond5 = false;				

				ShortCond1 = false;
				ShortCond2 = false;				
				ShortCond3 = false;				
				ShortCond4 = false;				
				ShortCond5 = false;				

				if (LongPos == null && ShortPos == null)
				{
					switch (PatternNum) 
					{
						case 1: 
							LongCond1 = BullishThreeOutsideUp(source, bar); 
							ShortCond1 = BearishThreeOutsideDown(source, bar); 
							break;
						case 2: 
							LongCond1 = BullishPiercingLine(source, bar); 
							ShortCond1 = BearishDarkCloudCover(source, bar); 
							break;
						case 3: 
							LongCond1 = BullishEngulfingLines(source, bar); 
							ShortCond1 = BearishEngulfingLines(source, bar); 
							break;
						case 4: 
							LongCond1 = BullishHarami(source, bar); 
							ShortCond1 = BearishHarami(source, bar); 
							break;
						case 5: 
							LongCond1 = BullishBeltHold(source, bar); 
							ShortCond1 = BearishBeltHold(source, bar); 
							break;
					}
				}
				
				#endregion
				//--------------------------------------------------------------------------------
				#region generate signals
				// сброс значений сигналов
				bBuy = false;
				bSell = false;
				bShort = false;
				bCover = false;
				
				// установка сигналов по условиям
				// для длинных позиций
				if (LongCond1 || LongCond2 || LongCond3 || LongCond4 || LongCond5)
				{
					bBuy = true;
				}
				if (LongPos != null)
				{
					if (bar - LongEntryBar >= HoldBars) bSell = true;
				}
				// для коротких позиций
				if (ShortCond1 || ShortCond2 || ShortCond3 || ShortCond4 || ShortCond5)
				{
					bShort = true;
				}
				if (ShortPos != null)
				{
					if (bar - ShortEntryBar >= HoldBars) bCover = true;
				}
				
				// приоритет на открытие коротких позиций
				if (bBuy && bSell) bBuy = false;
				
				#endregion	
				//================================================================================
				#region execute signals
				//--------------------------------------------------------------------------------
				// выполнение сигналов для длинной позиции
				LongPos = source.Positions.GetLastActiveForSignal("LN");
				if (LongPos == null)
				{
					// Если нет активной длинной позиции
					if (bBuy)
					{
						// Если есть сигнал Buy, 
						// выдаем ордер на открыте новой длинной позиции.
						source.Positions.BuyAtMarket(bar+1, 1, "LN");
						LongEntryBar = bar+1;
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
				ShortPos = source.Positions.GetLastActiveForSignal("SN");
				if (ShortPos == null)
				{
					// Если нет активной короткой позиции
					if (bShort)
					{
						// Если есть сигнал Short
						// выдаем ордер на открыте новой короткой позиции.
						source.Positions.SellAtMarket(bar+1, 1, "SN");
						ShortEntryBar = bar+1;
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
 
			// Создаем дополнительную панель для ATR.
			IPane ATRPane = ctx.CreatePane("ATR", 10, false, false);

			// Отрисовка графика ATR
			ATRPane.AddList(string.Format("ATR"), nATR, ListStyles.LINE,
			0x5050a0, LineStyles.SOLID, PaneSides.RIGHT);
			

			#endregion
			//--------------------------------------------------------------------------------
		}
	}
}
