using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace ImageUtils
{
    /// <summary>
    /// BGRA32bit形式に特化した画像バッファクラス。
    /// 画像の左上を原点(0,0)として、右下が正。
    /// </summary>
    public class ImageBuffer
    {
        /// <summary>
        /// 透過色
        /// </summary>
        public static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        /// イメージバッファ
        /// </summary>
        private IntPtr _imageBuffer; // BGRA, BGRA, BGRA ...

        /// <summary>
        /// 幅
        /// </summary>
        private readonly int _width;
        /// <summary>
        /// 高さ
        /// </summary>
        private readonly int _height;
        /// <summary>
        /// バックバッファ
        /// </summary>
        private readonly int _backBufferStride;

        /// <summary>
        /// 新しいImageBufferを構築する。
        /// </summary>
        /// <param name="width">水平ピクセル巣</param>
        /// <param name="height">垂直ライン数</param>
        public ImageBuffer(int width, int height)
        {
            if ((width < 0) || (height < 0)) {
                throw new ArgumentException("Image size must positive value. width=" + width + ",height=" + height);
            }
            _width = width;
            _backBufferStride = _width * 4;
            _height = height;
            if ((width > 0) && (height > 0)) {
                _imageBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(byte)) * _backBufferStride * _height);
            } else {
                _imageBuffer = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 空のImageBufferを構築する。
        /// </summary>
        public ImageBuffer() : this(0, 0)
        {
        }

        /// <summary>
        /// 画像の幅を取得するプロパティ。
        /// </summary>
        public int Width {
            get { return _width; }
        }

        /// <summary>
        /// 画像の高さを取得するプロパティ。
        /// </summary>
        public int Height {
            get { return _height; }
        }

        /// <summary>
        /// バックバッファのストライド(ラインあたりのバイト数)を取得するプロパティ。
        /// </summary>
        public int BackBufferStride
        {
            get { return _width * 4; }
        }

        /// <summary>
        /// バックバッファを取得するプロパティ。
        /// </summary>
        public IntPtr BackBuffer
        {
            get { return _imageBuffer; }
            
        }

        /// <summary>
        /// 矩形領域を得る
        /// </summary>
        /// <param name="x">矩形領域</param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public ImageBuffer GetRectangle(int x, int y, int width, int height)
        {
            ImageBuffer subImage = new ImageBuffer(width, height);

            Parallel.For(0, height, j => {
                for (int i = 0; i < width; i++) {
                    Color c = GetPixel(x + i, y + j);
                    subImage.SetPixel(i, j, c);
                }
            });

            return subImage;
        }

        /// <summary>
        /// 矩形領域に描画する。
        /// </summary>
        /// <param name="dstX">x座標</param>
        /// <param name="srcYy">y座標</param>
        /// <param name="image">描画するデータ</param>
        public void DrawRectangle(int dstX, int dstY, ImageBuffer image)
        {
            for (int j = 0; j < image.Height; j++) {
                for (int i = 0; i < image.Width; i++) {
                    Color c = image.GetPixel(i, j);
                    SetPixel(dstX + i, dstY + j, c);
                }
            }
        }


        /// <summary>
        /// 矩形領域に描画する。
        /// </summary>
        /// <param name="dstX"></param>
        /// <param name="dstY"></param>
        /// <param name="srcImage"></param>
        /// <param name="srcX"></param>
        /// <param name="srcY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void DrawRectangle(int dstX, int dstY, ImageBuffer srcImage, int srcX, int srcY, int width, int height)
        {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    Color c = srcImage.GetPixel(srcX + i, srcY + j);
                    SetPixel(dstX + i, dstY + j, c);
                }
            }
        }

        /// <summary>
        /// ピクセルを取得する。
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>ピクセルの値。範囲外を指定された場合にはTransparentが返る。</returns>
        public Color GetPixel(int x, int y)
        {
            if ((x < 0) || (x >= _width) || (y < 0) || (y >= _height)) {
                return Transparent;
            }

            unsafe {
                byte* srcp = (byte*)(_imageBuffer) + (_width * y + x) * 4;

                return Color.FromArgb(
                    srcp[3], // A 
                    srcp[2], // R
                    srcp[1], // G
                    srcp[0]); // B
            }
        }

        /// <summary>
        /// ピクセルを取得する。
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>ピクセルの値。範囲外を指定された場合には、縁の画素が返る。</returns>
        public Color GetPixelOnImage(int x, int y)
        {
            if (x < 0) {
                x = 0;
            } else if (x >= _width) {
                x = _width - 1;
            }
            
            if (y < 0) {
                y = 0;
            } else if (y >= _height) {
                y = _height - 1;
            }

            unsafe {
                byte* srcp = (byte*)(_imageBuffer) + (_width * y + x) * 4;

                return Color.FromArgb(
                    srcp[3], // A 
                    srcp[2], // R
                    srcp[1], // G
                    srcp[0]); // B
            }
        }

        /// <summary>
        /// ピクセルを設定する。
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="c">ピクセル値</param>
        public void SetPixel(int x, int y, Color c)
        {
            if ((x < 0) || (x >= _width) || (y < 0) || (y >= _height)) {
                return;
            }

            unsafe {
                byte* srcp = (byte*)(_imageBuffer) + (_width * y + x) * 4;
                srcp[3] = c.A; // A 
                srcp[2] = c.R; // R
                srcp[1] = c.G; // G
                srcp[0] = c.B; // B
            }
        }

        /// <summary>
        /// WPFのBitmapへ変換する。
        /// </summary>
        /// <returns>BitmapSourceオブジェクトが返る</returns>
        public BitmapSource ToBitmap()
        {
            WriteableBitmap wb = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgra32, null);

            try {
                wb.Lock();
                unsafe {
                    byte* dstBuffer = (byte*)(wb.BackBuffer);
                    byte* srcBuffer = (byte*)(_imageBuffer);

                    for (int y = 0; y < _height; y++) {
                        byte* srcp = srcBuffer + BackBufferStride * y;
                        byte* dstp = dstBuffer + wb.BackBufferStride * y;
                        for (int x = 0; x < _width; x++) {
                            dstp[0] = srcp[0]; // B
                            dstp[1] = srcp[1]; // G
                            dstp[2] = srcp[2]; // R
                            dstp[3] = srcp[3]; // A
                            srcp += 4;
                            dstp += 4;
                        }
                    }
                }
                wb.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _width, _height));
                return wb;
            }
            finally {
                wb.Unlock();
            }
        }

        /// <summary>
        /// 指定した色を透過色に設定する。
        /// </summary>
        /// <param name="c"></param>
        public void TransparentByPixel(Color c)
        {
            unsafe {
                for (int y = 0; y < _height; y++) {
                    byte* ptr = (byte*)(_imageBuffer) + _backBufferStride * y;
                    for (int x = 0; x < _width; x++) {
                        if ((ptr[0] == c.B) && (ptr[1] == c.G) && (ptr[2] == c.R)) {
                            ptr[0] = 0;
                            ptr[1] = 0;
                            ptr[2] = 0;
                            ptr[3] = 0;
                        }
                        ptr += 4;
                    }
                }
            }
        }

        /// <summary>
        /// 反転させたイメージを得る。
        /// </summary>
        /// <param name="isHorizontalFlip">水平方向の反転有無</param>
        /// <param name="isVerticalFlip">垂直方向の反転有無</param>
        /// <returns>反転したイメージが変える。</returns>
        public ImageBuffer GetFlippedImage(bool isHorizontalFlip, bool isVerticalFlip)
        {
            ImageBuffer retImage = new ImageBuffer(_width, _height);

            unsafe {
                byte* srcBuffer = (byte*)(BackBuffer);
                byte* dstBuffer = (byte*)(retImage.BackBuffer);

                for (int y = 0; y < _height; y++) {
                    byte* dstp = dstBuffer + retImage.BackBufferStride * y;

                    byte* srcp = srcBuffer;
                    if (isVerticalFlip) {
                        srcp += BackBufferStride * (_height - y - 1);
                    } else {
                        srcp += BackBufferStride * y;
                    }
                    if (isHorizontalFlip) {
                        srcp += (BackBufferStride - 4);
                        for (int x = 0; x < _width; x++) {
                            dstp[0] = srcp[0];
                            dstp[1] = srcp[1];
                            dstp[2] = srcp[2];
                            dstp[3] = srcp[3];
                            srcp -= 4;
                            dstp += 4;
                        }
                    } else {
                        for (int x = 0; x < _width; x++) {
                            dstp[0] = srcp[0];
                            dstp[1] = srcp[1];
                            dstp[2] = srcp[2];
                            dstp[3] = srcp[3];
                            srcp += 4;
                            dstp += 4;
                        }
                    }
                }
            }

            return retImage;
        }


        /// <summary>
        /// データを複製する。
        /// </summary>
        /// <returns>ImageBufferが返る。</returns>
        public ImageBuffer Duplicate()
        {
            ImageBuffer dst = new ImageBuffer(Width, Height);

            unsafe {
                byte* srcp = (byte*)(_imageBuffer);
                byte* dstp = (byte*)(dst._imageBuffer);
                int length = _backBufferStride * Height;
                for (int i = 0; i < length; i++) {
                    *dstp = *srcp;
                    dstp++;
                    srcp++;
                }

            }

            return dst;
        }

        /// <summary>
        /// BitmapSourceからImageBufferを取得する。
        /// </summary>
        /// <param name="source">BitmapSourceオブジェクト</param>
        /// <returns>ImageBufferが返る。</returns>
        public static ImageBuffer FromBitmap(BitmapSource source)
        {
            FormatConvertedBitmap fcb = new FormatConvertedBitmap();
            fcb.BeginInit();
            fcb.Source = source;
            fcb.DestinationFormat = PixelFormats.Bgra32;
            fcb.EndInit();

            WriteableBitmap wb = new WriteableBitmap(fcb);

            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            int lineByteSize = wb.BackBufferStride;

            wb.Lock();
            try {
                ImageBuffer dstImage = new ImageBuffer(width, height);
                unsafe {
                    byte* srcBuffer = (byte*)(wb.BackBuffer);
                    byte* dstBuffer = (byte*)(dstImage.BackBuffer);

                    for (int y = 0; y < height; y++) {
                        byte* srcp = srcBuffer + lineByteSize * y;
                        byte* dstp = dstBuffer + dstImage.BackBufferStride * y;
                        for (int x = 0; x < width; x++) {
                            dstp[0] = srcp[0]; // B
                            dstp[1] = srcp[1]; // G
                            dstp[2] = srcp[2]; // R
                            dstp[3] = srcp[3]; // A

                            srcp += 4;
                            dstp += 4;
                        }
                    }
                }
                return dstImage;
            }
            finally {
                wb.Unlock();
            }
        }

    }
}
