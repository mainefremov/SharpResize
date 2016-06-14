using System;

namespace SharpResize
{
	public class SharpResizeEngine
	{
		private int _analyDegree;
		private int _corrDegree;
		private double _halfSupport;
		private double[] _splineArrayHeight;
		private double[] _splineArrayWidth;
		private int[] _indexMinHeight;
		private int[] _indexMaxHeight;
		private int[] _indexMinWidth;
		private int[] _indexMaxWidth;

		/// <summary>
		/// Изменяет размер нескольких двумерных массивов, интерпретируя их данные как значения пикселей.
		/// </summary>
		/// <param name="input">Двумерные массивы с входными данными.</param>
		/// <param name="targetSizeX">Ширина результата.</param>
		/// <param name="targetSizeY">Высота результата.</param>
		/// <returns>Массивы с измененным размером.</returns>
		public double[][,] Resize(double[][,] input, int targetSizeX, int targetSizeY)
		{
			var result = new double[input.Length][,];
			for (var i = 0; i < input.Length; i++)
				result[i] = Resize(input[i], targetSizeX, targetSizeY);

			return result;
		}


		/// <summary>
		/// Изменяет размер двумерного массива, интерпретируя его данные как значения пикселей.
		/// </summary>
		/// <param name="input">Двумерный массив входных данных.</param>
		/// <param name="targetSizeX">Ширина результата.</param>
		/// <param name="targetSizeY">Высота результата.</param>
		/// <returns>Массив с измененным размером.</returns>
		public double[,] Resize(double[,] input, int targetSizeX, int targetSizeY)
		{
			_analyDegree = input.GetLength(0) > 3000 || input.GetLength(1) > 3000 ? 1 : 3;
			var totalDegree = _analyDegree + 4;
			_corrDegree = _analyDegree + 4;
			_halfSupport = (totalDegree + 1.0D) / 2D;

			var zoomX = (double) targetSizeX / input.GetLength(0);
			var zoomY = (double) targetSizeY / input.GetLength(1);

			var addBorderHeight = Border(targetSizeY, _corrDegree);
			if (addBorderHeight < totalDegree)
				addBorderHeight += totalDegree;
			var finalTotalHeight = targetSizeY + addBorderHeight;
			var lengthTotalHeight = input.GetLength(1) + (int) Math.Ceiling(addBorderHeight / zoomY);
			_indexMinHeight = new int[finalTotalHeight];
			_indexMaxHeight = new int[finalTotalHeight];
			var lengthArraySplnHeight = finalTotalHeight * (2 + totalDegree);
			var i = 0;
			var factHeight = Math.Pow(zoomY, _analyDegree + 1);
			_splineArrayHeight = new double[lengthArraySplnHeight];
			for (var l = 0; l < finalTotalHeight; l++)
			{
				var affineIndex = l / zoomY;
				_indexMinHeight[l] = (int) Math.Ceiling(affineIndex - _halfSupport);
				_indexMaxHeight[l] = (int) Math.Floor(affineIndex + _halfSupport);
				for (var k = _indexMinHeight[l]; k <= _indexMaxHeight[l]; k++)
				{
					_splineArrayHeight[i] = factHeight * Beta(affineIndex - k, totalDegree);
					i++;
				}
			}

			var addBorderWidth = Border(targetSizeX, _corrDegree);
			if (addBorderWidth < totalDegree)
				addBorderWidth += totalDegree;
			var finalTotalWidth = targetSizeX + addBorderWidth;
			var lengthTotalWidth = input.GetLength(0) + (int) Math.Ceiling(addBorderWidth / zoomX);
			_indexMinWidth = new int[finalTotalWidth];
			_indexMaxWidth = new int[finalTotalWidth];
			var lengthArraySplnWidth = finalTotalWidth * (2 + totalDegree);
			i = 0;
			var factWidth = Math.Pow(zoomX, _analyDegree + 1);
			_splineArrayWidth = new double[lengthArraySplnWidth];
			for (var l = 0; l < finalTotalWidth; l++)
			{
				var affineIndex = l / zoomX;
				_indexMinWidth[l] = (int) Math.Ceiling(affineIndex - _halfSupport);
				_indexMaxWidth[l] = (int) Math.Floor(affineIndex + _halfSupport);
				for (var k = _indexMinWidth[l]; k <= _indexMaxWidth[l]; k++)
				{
					_splineArrayWidth[i] = factWidth * Beta(affineIndex - k, totalDegree);
					i++;
				}
			}

			var outputColumn = new double[targetSizeY];
			var outputRow = new double[targetSizeX];
			var workingRow = new double[input.GetLength(0)];
			var workingColumn = new double[input.GetLength(1)];
			var addVectorHeight = new double[lengthTotalHeight];
			var addOutputVectorHeight = new double[finalTotalHeight];
			var addVectorWidth = new double[lengthTotalWidth];
			var addOutputVectorWidth = new double[finalTotalWidth];
			var periodColumnSym = 2 * input.GetLength(1) - 2;
			var periodRowSym = 2 * input.GetLength(0) - 2;
			var image = new double[targetSizeX, input.GetLength(1)];
			var output = new double[targetSizeX, targetSizeY];

			for (var y = 0; y < input.GetLength(1); y++)
			{
				for (var x = 0; x < workingRow.Length; x++)
					workingRow[x] = input[x, y];

				GetInterpolationCoefficients(workingRow, 3);
				ResamplingRow(workingRow, outputRow, addVectorWidth, addOutputVectorWidth, periodRowSym);

				for (var x = 0; x < outputRow.Length; x++)
					image[x, y] = outputRow[x];
			}

			for (var x = 0; x < targetSizeX; x++)
			{
				for (var y = 0; y < workingColumn.Length; y++)
					workingColumn[y] = image[x, y];

				GetInterpolationCoefficients(workingColumn, 3);
				ResamplingColumn(workingColumn, outputColumn, addVectorHeight, addOutputVectorHeight, periodColumnSym);

				for (var y = 0; y < outputColumn.Length; y++)
					output[x, y] = outputColumn[y];
			}

			return output;
		}

		private void ResamplingRow(double[] inputVector, double[] outputVector, double[] addVector, double[] addOutputVector, int maxSymBoundary)
		{
			var lengthInput = inputVector.Length;
			var lengthOutput = outputVector.Length;
			var lengthtotal = addVector.Length;
			var lengthOutputtotal = addOutputVector.Length;
			var average = DoInteg(inputVector, _analyDegree + 1);
			Array.Copy(inputVector, 0, addVector, 0, lengthInput);
			for (var l = lengthInput; l < lengthtotal; l++)
			{
				var l2 = l;
				if (l >= maxSymBoundary)
					l2 = (int) Math.Abs(Math.IEEERemainder(l, maxSymBoundary));
				if (l2 >= lengthInput)
					l2 = maxSymBoundary - l2;
				addVector[l] = inputVector[l2];
			}

			var i = 0;
			for (var l = 0; l < lengthOutputtotal; l++)
			{
				addOutputVector[l] = 0.0D;
				for (var k = _indexMinWidth[l]; k <= _indexMaxWidth[l]; k++)
				{
					var index = k;
					if (k < 0)
					{
						index = -k;
					}
					if (k >= lengthtotal)
						index = lengthtotal - 1;
					addOutputVector[l] += addVector[index] * _splineArrayWidth[i];
					i++;
				}

			}

			DoDiff(addOutputVector, _analyDegree + 1);
			for (i = 0; i < lengthOutputtotal; i++)
				addOutputVector[i] += average;

			GetInterpolationCoefficients(addOutputVector, _corrDegree);
			GetSamples(addOutputVector);
			Array.Copy(addOutputVector, 0, outputVector, 0, lengthOutput);
		}

		private void ResamplingColumn(double[] inputVector, double[] outputVector, double[] addVector, double[] addOutputVector, int maxSymBoundary)
		{
			var lengthInput = inputVector.Length;
			var lengthOutput = outputVector.Length;
			var lengthtotal = addVector.Length;
			var lengthOutputtotal = addOutputVector.Length;
			var average = DoInteg(inputVector, _analyDegree + 1);
			Array.Copy(inputVector, 0, addVector, 0, lengthInput);
			for (var l = lengthInput; l < lengthtotal; l++)
			{
				var l2 = l;
				if (l >= maxSymBoundary)
					l2 = (int) Math.Abs(Math.IEEERemainder(l, maxSymBoundary));
				if (l2 >= lengthInput)
					l2 = maxSymBoundary - l2;
				addVector[l] = inputVector[l2];
			}

			var i = 0;
			for (var l = 0; l < lengthOutputtotal; l++)
			{
				addOutputVector[l] = 0.0D;
				for (var k = _indexMinHeight[l]; k <= _indexMaxHeight[l]; k++)
				{
					var index = k;
					if (k < 0)
					{
						index = -k;
					}
					if (k >= lengthtotal)
						index = lengthtotal - 1;
					addOutputVector[l] += addVector[index] * _splineArrayHeight[i];
					i++;
				}
			}

			DoDiff(addOutputVector, _analyDegree + 1);
			for (i = 0; i < lengthOutputtotal; i++)
				addOutputVector[i] += average;

			GetInterpolationCoefficients(addOutputVector, _corrDegree);
			GetSamples(addOutputVector);
			Array.Copy(addOutputVector, 0, outputVector, 0, lengthOutput);
		}

		private static double Beta(double x, int degree)
		{
			var betan = 0.0D;
			if (degree == 5)
			{
				x = Math.Abs(x);
				if (x < 1.0D)
				{
					var a = x * x;
					betan = a * (a * (0.25D - x * 0.083333333333333329D) - 0.5D) + 0.55000000000000004D;
					return betan;
				}
				if (x < 2D)
				{
					betan = x * (x * (x * (x * (x * 0.041666666666666664D - 0.375D) + 1.25D) - 1.75D) + 0.625D) + 0.42499999999999999D;
					return betan;
				}
				if (x < 3D)
				{
					var a = 3D - x;
					x = a * a;
					betan = a * x * x * 0.0083333333333333332D;
				}
				return betan;
			}

			x = Math.Abs(x);
			if (x < 1.0D)
			{
				var a = x * x;
				betan = a * (a * (a * (x * 0.0069444444444444441D - 0.027777777777777776D) + 0.1111111111111111D) - 0.33333333333333331D) + 0.47936507936507938D;
				return betan;
			}
			if (x < 2D)
			{
				betan = x * (x * (x * (x * (x * (x * (0.050000000000000003D - x * 0.0041666666666666666D) - 0.23333333333333334D) + 0.5D) - 0.3888888888888889D) - 0.10000000000000001D) - 0.077777777777777779D) + 0.49047619047619045D;
				return betan;
			}
			if (x < 3D)
			{
				betan = x * (x * (x * (x * (x * (x * (x * 0.0013888888888888889D - 0.027777777777777776D) + 0.23333333333333334D) - 1.0555555555555556D) + 2.7222222222222223D) - 3.8333333333333335D) + 2.411111111111111D) - 0.22063492063492063D;
				return betan;
			}
			if (x < 4D)
			{
				var a = 4D - x;
				x = a * a * a;
				betan = x * x * a * 0.00019841269841269841D;
			}
			return betan;
		}

		private static double DoInteg(double[] c, int nb)
		{
			var size = c.Length;
			var m = 0.0D;
			var average = 0.0D;
			if (nb == 2)
			{
				for (var f = 0; f < size; f++)
					average += c[f];

				average = (2D * average - c[size - 1] - c[0]) / (2 * size - 2);
				IntegSa(c, average);
				IntegAs(c, c);
			}
			else
			{
				for (var f = 0; f < size; f++)
					average += c[f];

				average = (2D * average - c[size - 1] - c[0]) / (2 * size - 2);
				IntegSa(c, average);
				IntegAs(c, c);
				for (var f = 0; f < size; f++)
					m += c[f];

				m = (2D * m - c[size - 1] - c[0]) / (2 * size - 2);
				IntegSa(c, m);
				IntegAs(c, c);
			}
			return average;
		}

		private static void IntegSa(double[] c, double m)
		{
			var size = c.Length;
			c[0] = (c[0] - m) * 0.5D;
			for (var i = 1; i < size; i++)
				c[i] = (c[i] - m) + c[i - 1];

		}

		private static void IntegAs(double[] c, double[] y)
		{
			var size = c.Length;
			var z = new double[size];
			Array.Copy(c, 0, z, 0, size);
			y[0] = z[0];
			y[1] = 0.0D;
			for (var i = 2; i < size; i++)
				y[i] = y[i - 1] - z[i - 1];

		}

		private static void DoDiff(double[] c, int nb)
		{
			if (nb == 2)
			{
				DiffSa(c);
				DiffAs(c);
			}
			else
			{
				DiffSa(c);
				DiffAs(c);
				DiffSa(c);
				DiffAs(c);
			}
		}

		private static void DiffSa(double[] c)
		{
			var size = c.Length;
			var old = c[size - 2];
			for (var i = 0; i <= size - 2; i++)
				c[i] = c[i] - c[i + 1];

			c[size - 1] = c[size - 1] - old;
		}

		private static void DiffAs(double[] c)
		{
			var size = c.Length;
			for (var i = size - 1; i > 0; i--)
				c[i] = c[i] - c[i - 1];

			c[0] = 2D * c[0];
		}

		private static int Border(int size, int degree)
		{
			var horizon = degree == 5 ? 26 : 35;
			horizon = horizon >= size ? size : horizon;
			return horizon;
		}

		private static void GetInterpolationCoefficients(double[] c, int degree)
		{
			double[] z;
			var lambda = 1.0D;
			switch (degree)
			{
				case 3:
					z = new double[1];
					z[0] = Math.Sqrt(3D) - 2D;
					break;

				case 5:
					z = new double[2];
					z[0] = (Math.Sqrt(67.5D - Math.Sqrt(4436.25D)) + Math.Sqrt(26.25D)) - 6.5D;
					z[1] = Math.Sqrt(67.5D + Math.Sqrt(4436.25D)) - Math.Sqrt(26.25D) - 6.5D;
					break;

				default:
					z = new double[3];
					z[0] = -0.53528043079643817D;
					z[1] = -0.12255461519232669D;
					z[2] = -0.0091486948096082769D;
					break;
			}

			if (c.Length == 1)
				return;

			// ReSharper disable once ForCanBeConvertedToForeach
			// ReSharper disable once LoopCanBeConvertedToQuery
			for (var k = 0; k < z.Length; k++)
				lambda = lambda * (1.0D - z[k]) * (1.0D - 1.0D / z[k]);

			for (var n = 0; n < c.Length; n++)
				c[n] = c[n] * lambda;

			// ReSharper disable once ForCanBeConvertedToForeach
			for (var k = 0; k < z.Length; k++)
			{
				c[0] = GetInitialCausalCoefficient(c, z[k]);
				for (var n = 1; n < c.Length; n++)
					c[n] = c[n] + z[k] * c[n - 1];

				c[c.Length - 1] = GetInitialAntiCausalCoefficient(c, z[k]);
				for (var n = c.Length - 2; n >= 0; n--)
					c[n] = z[k] * (c[n + 1] - c[n]);
			}
		}

		private static void GetSamples(double[] c)
		{
			var s = new double[c.Length];

			if (c.Length > 1)
			{
				s[0] = (4D * c[0] + 2D * c[1]) / 6D;

				for (var i = 1; i < c.Length - 1; i++)
					s[i] = (4D * c[i] + c[i - 1] + c[i + 1]) / 6D;

				s[s.Length - 1] = (4D * c[c.Length - 1] + 2D * c[c.Length - 2]) / 6D;

			}
			else
			{
				s[0] = c[0];
			}

			Array.Copy(s, 0, c, 0, s.Length);
		}

		private static double GetInitialAntiCausalCoefficient(double[] c, double z)
		{
			return (z * c[c.Length - 2] + c[c.Length - 1]) * z / (z * z - 1.0D);
		}

		private static double GetInitialCausalCoefficient(double[] c, double z)
		{
			var z1 = z;
			var zn = Math.Pow(z, c.Length - 1);
			var sum = c[0] + zn * c[c.Length - 1];
			var horizon = 2 + (int) (-20.7232658369464D / Math.Log(Math.Abs(z)));
			horizon = horizon >= c.Length ? c.Length : horizon;
			zn *= zn;
			for (var n = 1; n < horizon - 1; n++)
			{
				zn /= z;
				sum += (z1 + zn) * c[n];
				z1 *= z;
			}

			return sum / (1.0D - Math.Pow(z, 2 * c.Length - 2));
		}
	}
}
