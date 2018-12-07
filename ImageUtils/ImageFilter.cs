using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;

namespace ImageUtils
{
    public class ImageFilter
    {
        /// <summary>
        /// フィルタ処理を行う。
        /// </summary>
        /// <param name="src">ソースイメージ</param>
        /// <param name="filter">フィルターデータ</param>
        /// <returns>フィルタ処理した結果の画像データが返る。</returns>
        public static ImageBuffer Process(ImageBuffer src, FilterData filter)
        {
            ImageBuffer dst = new ImageBuffer(src.Width, src.Height);

            Parallel.For(0, src.Height, y => {
                for (int x = 0; x < src.Width; x++) {
                    Color c = GetFilterredPixel(src, x, y, filter);
                    dst.SetPixel(x, y, c);
                }
            });

            return dst;
        }
        /// <summary>
        /// フィルタ対象のデータがあるピクセルに対して、フィルタ処理を行う。
        /// </summary>
        /// <param name="src">ソースイメージ</param>
        /// <param name="filter">フィルターデータ</param>
        /// <returns>フィルタ処理した結果の画像データが返る。</returns>
        public static ImageBuffer ProcessInternalPart(ImageBuffer src, FilterData filter)
        {
            ImageBuffer dst = new ImageBuffer(src.Width, src.Height);

            /*
            for (int y = 0; y < src.Height; y++) {
                for (int x = 0; x < src.Width; x++) {
                    if (IsFilterAreaHasValidData(src, x, y, filter)) {
                        Color c = GetFilterredPixel(src, x, y, filter);
                        dst.SetPixel(x, y, c);
                    } else {
                        dst.SetPixel(x, y, src.GetPixel(x, y));
                    }
                }
            }
             */

            Parallel.For(0, src.Height, y => {
                for (int x = 0; x < src.Width; x++) {
                    if (IsFilterAreaHasValidData(src, x, y, filter)) {
                        Color c = GetFilterredPixel(src, x, y, filter);
                        dst.SetPixel(x, y, c);
                    } else {
                        dst.SetPixel(x, y, src.GetPixel(x, y));
                    }
                }
            });


            return dst;
        }

        /// <summary>
        /// フィルタ対象のピクセルが有効データを持っているかどうかを取得する。
        /// </summary>
        /// <param name="src">ソースイメージ</param>
        /// <param name="centerX">センターX座標</param>
        /// <param name="centerY">センターY座標</param>
        /// <param name="filter">フィルタデータ</param>
        /// <returns>フィルタ対象のピクセルがすべて有効なデータを持っている場合にはtrue、
        ///          いずれかの画素が無効（透過）な場合にはfalseが返る。</returns>
        private static bool IsFilterAreaHasValidData(ImageBuffer src, int centerX, int centerY, FilterData filter)
        {
            int n = 0;
            int y = centerY - ((filter.VSampleCount - 1) >> 1);
            for (int j = 0; j < filter.VSampleCount; j++) {
                int x = centerX - ((filter.HSampleCount - 1) >> 1);
                for (int i = 0; i < filter.HSampleCount; i++) {
                    if ((filter.Coefficient[n] != 0) &&  (src.GetPixel(x, y).A == 0)) {
                        return false;
                    }
                    x++;
                    n++;
                }
                y++;
            }

            return true;
        }

        /// <summary>
        /// フィルタ処理したピクセルデータを得る。
        /// </summary>
        /// <param name="src">ソース</param>
        /// <param name="centerX">センターX座標</param>
        /// <param name="centerY">センターY座標</param>
        /// <param name="filter">フィルタデータ</param>
        /// <returns>フィルタ処理結果のピクセルデータが返る。</returns>
        private static Color GetFilterredPixel(ImageBuffer src, int centerX, int centerY, FilterData filter)
        {
            double sumB, sumG, sumR, sumA;

            sumB = sumG = sumR = sumA = 0.0;

            int n = 0;
            int y = centerY - ((filter.VSampleCount - 1) >> 1);
            for (int j = 0; j < filter.VSampleCount; j++) {
                int x = centerX - ((filter.HSampleCount - 1) >> 1);
                for (int i = 0; i < filter.HSampleCount; i++) {
                    Color c = src.GetPixel(x, y);
                    sumB += c.B * filter.Coefficient[n];
                    sumG += c.G * filter.Coefficient[n];
                    sumR += c.R * filter.Coefficient[n];
                    sumA += c.A * filter.Coefficient[n];
                    x++;
                    n++;
                }
                y++;
            }


            if (filter.Total == 0) {
                return ImageBuffer.Transparent;
            } else {
                return Color.FromArgb(Limit(sumA / filter.Total),
                    Limit(sumR / filter.Total),
                    Limit(sumG / filter.Total),
                    Limit(sumB / filter.Total));
            }
        }

        /// <summary>
        /// 0～255に制限した値を得る
        /// </summary>
        /// <param name="val">値</param>
        /// <returns>byte値</returns>
        private static byte Limit(double val)
        {
            if (val > 255) {
                return 255;
            } else if (val < 0) {
                return 0;
            } else {
                return (byte)(Math.Round(val));
            }
        }

        private ImageFilter()
        {

        }

    }
}
