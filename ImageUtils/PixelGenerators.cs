using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;

namespace ImageUtils
{
    public class PixelGenerators
    {
        private PixelGenerators()
        {
        }

        /// <summary>
        /// バイリニア補間によるピクセル生成を行う。
        /// </summary>
        /// <param name="image">画像データ</param>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>ピクセルのカラー値が返る</returns>
        public static Color GetPixelByBilinear(ImageBuffer image, double x, double y)
        {
            Color c00 = image.GetPixel((int)(Math.Floor(x)), (int)(Math.Floor(y)));
            Color c01 = image.GetPixel((int)(Math.Floor(x)), (int)(Math.Ceiling(y)));
            Color c10 = image.GetPixel((int)(Math.Ceiling(x)), (int)(Math.Floor(y)));
            Color c11 = image.GetPixel((int)(Math.Ceiling(x)), (int)(Math.Ceiling(y)));

            double dx = x - Math.Floor(x);
            double dy = y - Math.Floor(y);

            double a = (1 - dx) * (1 - dy) * c00.A + dx * (1 - dy) * c01.A
                + (1 - dx) * dy * c10.A + dx * dy * c11.A;
            double r = (1 - dx) * (1 - dy) * c00.R + dx * (1 - dy) * c01.R
                + (1 - dx) * dy * c10.R + dx * dy * c11.R;
            double g = (1 - dx) * (1 - dy) * c00.G + dx * (1 - dy) * c01.G
                + (1 - dx) * dy * c10.G + dx * dy * c11.G;
            double b = (1 - dx) * (1 - dy) * c00.B + dx * (1 - dy) * c01.B
                + (1 - dx) * dy * c10.B + dx * dy * c11.B;
            return Color.FromArgb(Limit(a), Limit(r), Limit(g), Limit(b));
        }

        /// <summary>
        /// 0～255に制限されたバイト値を取得する。
        /// </summary>
        /// <param name="d">輝度の実装表現値</param>
        /// <returns>バイト値が返る。</returns>
        private static byte Limit(double d)
        {
            if (d > 255) {
                return 255;
            } else if (d < 0) {
                return 0;
            } else {
                return (byte)(Math.Round(d));
            }
        }

    }
}
