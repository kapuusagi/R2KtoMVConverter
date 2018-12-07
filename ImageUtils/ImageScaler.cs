using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;

namespace ImageUtils
{
    public class ImageScaler
    {
        /// <summary>
        /// 画像のリサイズを行う。
        /// フィルタも何もかけないため、結果はギタギタした画像になる。
        /// 別途フィルタをかけること。
        /// </summary>
        /// <param name="src">画像ソース</param>
        /// <param name="xscale">水平拡大率</param>
        /// <param name="yscale">垂直拡大率</param>
        /// <returns>イメージバッファが返る</returns>
        public static ImageBuffer Resize(ImageBuffer src, double xscale, double yscale)
        {
            if ((xscale < 0) || (yscale < 0)) {
                throw new ArgumentException("xscale or yscale incorrect. xscale=" + xscale + ", yscale=" + yscale);
            }
            int newWidth = (int)(Math.Ceiling(src.Width * xscale));
            int newHeight = (int)(Math.Ceiling(src.Height * yscale));

            ImageBuffer dst = new ImageBuffer(newWidth, newHeight);
            if ((newWidth == 0) || (newHeight == 0)) {
                return dst;
            }

            double rx = 1 / xscale;
            double ry = 1 / yscale;

            for (int y = 0; y < newHeight; y++) {
                int srcY = (int)(Math.Floor(y * ry));
                for (int x = 0; x < newWidth; x++) {
                    int srcX = (int)(Math.Floor(x * rx));
                    Color c = src.GetPixel(srcX, srcY);
                    dst.SetPixel(x, y, c);
                }
            }

            return dst;
        }

    }
}
