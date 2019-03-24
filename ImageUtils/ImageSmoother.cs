using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUtils
{
    /// <summary>
    /// エッジを滑らかにする
    /// </summary>
    public class ImageSmoother
    {
        /// <summary>
        /// 外枠を滑らかにする
        /// </summary>
        /// <param name="src">ソースイメージ</param>
        /// <returns>ImageBufferが返る。</returns>
        public static ImageBuffer SmoothOuterFrame(ImageBuffer src)
        {
            ImageBuffer dst = new ImageBuffer(src.Width, src.Height);

            Parallel.For(0, src.Height, y => {
                for (int x = 0; x < src.Width; x++) {
                    if (IsNeedPixel(src, x, y)) {
                        dst.SetPixel(x, y, src.GetPixel(x, y));
                    }
                }
            });

            return dst;
        }

        /// <summary>
        /// 外枠を生成するのに必要なピクセルかどうかを取得する。
        /// </summary>
        /// <param name="image"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static bool IsNeedPixel(ImageBuffer image, int x, int y)
        {
            if ((image.GetPixel(x - 1, y - 1).A != 0)
                && (image.GetPixel(x + 1, y + 1).A != 0)) {
                // 左上と右下が有効ピクセル
                // *
                //   x
                //     *
                return true;
            }

            if ((image.GetPixel(x - 1, y).A != 0)
                && (image.GetPixel(x + 1, y).A != 0)) {
                // 左と右が有効ピクセル
                //  
                // * x *
                //      
                return true;
            }

            if ((image.GetPixel(x - 1, y + 1).A != 0)
                && (image.GetPixel(x + 1, y - 1).A != 0)) {
                // 左下と右上が有効ピクセル
                //     *
                //   x
                // *    
                return true;
            }

            if ((image.GetPixel(x, y - 1).A != 0)
                && (image.GetPixel(x, y + 1).A != 0)) {
                // 上と下が有効ピクセル
                //   *
                //   x
                //   *    
                return true;
            }

            return false;
        }
    }
}
