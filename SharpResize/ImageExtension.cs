using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SharpResize
{
	public static class ImageExtension
	{
		/// <summary>
		/// Изменяет размер изображения.
		/// </summary>
		/// <param name="inputBitmap">Входное изображение.</param>
		/// <param name="finalSizeX">Ширина результата.</param>
		/// <param name="finalSizeY">Высота результата.</param>
		/// <returns>Копия изображения с измененными размерами.</returns>
		public static Bitmap Resize(this Bitmap inputBitmap, int finalSizeX, int finalSizeY)
		{
			var resizeEngine = new SharpResizeEngine();
			var planes = inputBitmap.ReadBitmap();
			var resizedData = resizeEngine.Resize(planes, finalSizeX, finalSizeY);
			return resizedData.ToBitmap(inputBitmap.PixelFormat);
		}

		/// <summary>
		/// Метод для быстрого чтения RGB-значений пикселей.
		/// </summary>
		/// <param name="inputBitmap">Входное изображение.</param>
		/// <returns>Массив R, G и B цветовых плоскостей (double[,]) входного изображения.</returns>
		private static double[][,] ReadBitmap(this Bitmap inputBitmap)
		{
			var widthInPixels = inputBitmap.Width;
			var heightInPixels = inputBitmap.Height;
			var format = inputBitmap.PixelFormat;

			var resultBitmap = new[]
			{
				new double[widthInPixels, heightInPixels],
				new double[widthInPixels, heightInPixels],
				new double[widthInPixels, heightInPixels]
			};

			// Таблица для быстрого преобразования byte в double
			var byteToDouble = new double[byte.MaxValue + 1];
			for (var i = 0; i < byte.MaxValue + 1; i++)
				byteToDouble[i] = i;

			unsafe
			{
				var data = inputBitmap.LockBits(new Rectangle(0, 0, widthInPixels, heightInPixels), ImageLockMode.ReadOnly, format);
				var bpp = Image.GetPixelFormatSize(format) / 8;
				var widthInBytes = heightInPixels * bpp;
				var firstPtr = (byte*) data.Scan0;

				for (var rowIndex = 0; rowIndex < heightInPixels; rowIndex++)
				{
					var rowPtr = firstPtr + rowIndex * data.Stride;
					var columnIndex = 0;

					for (var x = 0; x < widthInBytes; x = x + bpp)
					{
						resultBitmap[0][columnIndex, rowIndex] = byteToDouble[rowPtr[x + 2]];
						resultBitmap[1][columnIndex, rowIndex] = byteToDouble[rowPtr[x + 1]];
						resultBitmap[2][columnIndex, rowIndex] = byteToDouble[rowPtr[x]];
						columnIndex++;
					}
				}

				inputBitmap.UnlockBits(data);
			}

			return resultBitmap;
		}

		/// <summary>
		/// Преобразует массив R, G и B цветовых плоскостей (double[,]) в Bitmap.
		/// </summary>
		/// <param name="inputData">Массив R, G и B цветовых плоскостей.</param>
		/// <param name="format">Формат кодирования пикселя целевого изображения.</param>
		/// <returns>Объект Bitmap, содержащий преобразованные входные данные.</returns>
		private static Bitmap ToBitmap(this double[][,] inputData, PixelFormat format)
		{
			var widthInPixels = inputData[0].GetLength(0);
			var heightInPixels = inputData[0].GetLength(1);

			var bitmap = new Bitmap(widthInPixels, heightInPixels, format);

			unsafe
			{
				var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, format);
				var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
				var widthInBytes = widthInPixels * bpp;
				var firstPtr = (byte*) data.Scan0;

				for (var rowIndex = 0; rowIndex < heightInPixels; rowIndex++)
				{
					var rowPtr = firstPtr + rowIndex * data.Stride;
					var columnIndex = 0;
					for (var x = 0; x < widthInBytes; x = x + bpp)
					{
						var r = (int) Math.Round(inputData[0][columnIndex, rowIndex]);
						var g = (int) Math.Round(inputData[1][columnIndex, rowIndex]);
						var b = (int) Math.Round(inputData[2][columnIndex, rowIndex]);

						// Проверяем и фиксим "вылеты" за целевой диапазон
						r = r > 255 ? 255 : (r < 0 ? 0 : r);
						g = g > 255 ? 255 : (g < 0 ? 0 : g);
						b = b > 255 ? 255 : (b < 0 ? 0 : b);

						rowPtr[x + 2] = (byte) r;
						rowPtr[x + 1] = (byte) g;
						rowPtr[x] = (byte) b;

						columnIndex++;
					}
				}

				bitmap.UnlockBits(data);
			}

			return bitmap;
		}
	}
}