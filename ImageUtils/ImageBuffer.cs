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
    public class ImageBuffer : IDisposable
    {
        /// <summary>
        /// 透過色
        /// </summary>
        public static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        /// イメージバッファ
        /// </summary>
        private byte[] _backBuffer; // BGRA BGRA.....
        // private IntPtr _backBuffer; // BGRA BGRA ....

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

        private bool _isDisposed;


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
            _isDisposed = false;
            if ((width > 0) && (height > 0)) {
                _backBuffer = new byte[_backBufferStride * _height];
                Clear();
            } else {
                _backBuffer = null;
            }
        }

        /// <summary>
        /// 空のImageBufferを構築する。
        /// </summary>
        public ImageBuffer() : this(0, 0)
        {
        }

        ~ImageBuffer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed) {
                return;
            }


            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            Array.Clear(_backBuffer, 0, _backBuffer.Length);
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

            int offset = _backBufferStride * y + 4 * x;
            return Color.FromArgb(_backBuffer[offset + 3], _backBuffer[offset + 2],
                _backBuffer[offset + 1], _backBuffer[offset + 0]);
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

            return GetPixel(x, y);
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

            int offset = _backBufferStride * y + x * 4;
            _backBuffer[offset + 3] = c.A;
            _backBuffer[offset + 2] = c.R;
            _backBuffer[offset + 1] = c.G;
            _backBuffer[offset + 0] = c.B;
        }

        /// <summary>
        /// WPFのBitmapへ変換する。
        /// </summary>
        /// <returns>BitmapSourceオブジェクトが返る</returns>
        public BitmapSource ToBitmap()
        {
            return BitmapSource.Create(_width, _height, 96, 96, PixelFormats.Bgra32, null, _backBuffer, _backBufferStride);
        }

        /// <summary>
        /// 指定した色を透過色に設定する。
        /// </summary>
        /// <param name="c"></param>
        public void TransparentByPixel(Color c)
        {
            for (int y = 0; y < _height; y++) {
                int offset = _backBufferStride * y;
                for (int x = 0; x < _width; x++) {
                    if ((_backBuffer[offset + 0] == c.B) 
                        && (_backBuffer[offset + 1] == c.G) && (_backBuffer[offset + 2] == c.R)) {
                        _backBuffer[offset + 0] = 0;
                        _backBuffer[offset + 1] = 0;
                        _backBuffer[offset + 2] = 0;
                        _backBuffer[offset + 3] = 0;
                    }
                    offset += 4;
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

            for (int y = 0; y < _height; y++) {
                int dstOffset = retImage.BackBufferStride * y;
                int srcOffset = 0;
                if (isVerticalFlip) {
                    srcOffset += _backBufferStride * (_height - y - 1);
                } else {
                    srcOffset += _backBufferStride * y;
                }

                if (isHorizontalFlip) {
                    srcOffset += _backBufferStride - 4;
                    for (int x = 0; x < _width; x++) {
                        retImage._backBuffer[dstOffset + 0] = _backBuffer[srcOffset + 0];
                        retImage._backBuffer[dstOffset + 1] = _backBuffer[srcOffset + 1];
                        retImage._backBuffer[dstOffset + 2] = _backBuffer[srcOffset + 2];
                        retImage._backBuffer[dstOffset + 3] = _backBuffer[srcOffset + 3];
                        srcOffset -= 4;
                        dstOffset += 4;
                    }
                } else {
                    Array.Copy(_backBuffer, srcOffset, retImage._backBuffer, dstOffset, _backBufferStride);
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

            Array.Copy(dst._backBuffer, _backBuffer, _backBuffer.Length);

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

                    for (int y = 0; y < height; y++) {
                        byte* srcp = srcBuffer + lineByteSize * y;
                        int dstOffset = dstImage._backBufferStride * y;

                        for (int x = 0; x < width; x++) {
                            dstImage._backBuffer[dstOffset + 0] = srcp[0];
                            dstImage._backBuffer[dstOffset + 1] = srcp[1];
                            dstImage._backBuffer[dstOffset + 2] = srcp[2];
                            dstImage._backBuffer[dstOffset + 3] = srcp[3];

                            srcp += 4;
                            dstOffset += 4;
                        }
                    }

                    return dstImage;
                }
            }
            finally {
                wb.Unlock();
            }
        }

        /// <summary>
        /// ピクセルが有効範囲かどうかを判定する。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Contains(int x, int y)
        {
            return ((x >= 0) && (x < _width) && (y >= 0) && (y < _height));
        }
    }
}
