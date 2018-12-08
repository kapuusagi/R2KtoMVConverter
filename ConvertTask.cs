using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ImageUtils;

namespace R2KtoMVConverter
{
    public class ConvertJob
    {
        private ConvertRequest _request;

        public ConvertJob(ConvertRequest request) 
        {
            _request = request;
        }

        /// <summary>
        /// リクエスト
        /// </summary>
        public ConvertRequest Request {
            get {
                return _request;
            }
        }

        /// <summary>
        /// 変換処理を実行する。
        /// </summary>
        /// <returns></returns>
        public bool Execute()
        {
            foreach (string path in _request.Files) {
                string dir = System.IO.Path.GetDirectoryName(path);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string outputPath = System.IO.Path.Combine(dir, fileName + "_rtk2000.png");

                Convert(path, outputPath);
            }

            return true;
        }

        /// <summary>
        /// pathで指定されるファイルを変換し、outputPathに書き出す。
        /// </summary>
        /// <param name="path">ソースファイルパス</param>
        /// <param name="outputPath">出力ファイルパス</param>
        private static void Convert(string path, string outputPath)
        {
            BitmapImage fileImage = new BitmapImage();
            fileImage.BeginInit();
            fileImage.UriSource = new Uri(path);
            fileImage.EndInit();

            WriteableBitmap readImage = new WriteableBitmap(fileImage);

            // ImageUtils.ImageBufferに変換する。
            // ついでに(0,0)画素値を透過するピクセルとして処理し、該当ピクセルを透過する。
            ImageBuffer srcImage = ImageBuffer.FromBitmap(readImage);

            ImageBuffer replacedImage = null;
            if (IsRPG2KCharachip(srcImage)) {
                // RPGツクール2000の歩行グラフィック並び順を
                // RPGツクールMV用のものに変更する。
                replacedImage = ReplaceToMV(srcImage);
            } else if (IsRSaga3CharaImage(srcImage)) {
                replacedImage = GenerateCharaDataFromRSaga312(srcImage);
            } else {
                throw new NotSupportedException("Target file not supported. : " + path);
            }


            // 2倍に拡大する。
            // バイリニアなどの処理は入っていない。
            ImageBuffer x2Image = ImageScaler.Resize(replacedImage, 2.0, 2.0);

            // 単純な2倍だと、外枠がギタギタなので、外枠を滑らかにする。
            ImageBuffer smoothImage = ImageSmmother.SmoothOuterFrame(x2Image);
            // 内部に対してフィルタ処理して、多少滑らかにする。
            ImageBuffer filterredImage = ImageFilter.ProcessInternalPart(smoothImage, ImageFilters.M3x3Filter);

            // 書き出す。
            BitmapSource dstImage = filterredImage.ToBitmap();
            using (var outputStream = System.IO.File.Create(outputPath)) {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(dstImage));
                encoder.Save(outputStream);
            }
        }

        /// <summary>
        /// RPGツクール2000のキャラチップ素材かどうかを判定する。
        /// </summary>
        /// <param name="srcImage">画像データ</param>
        /// <returns>キャラチップ素材の場合にはtrue,それ以外はfalseが返る。</returns>
        private static bool IsRPG2KCharachip(ImageBuffer srcImage)
        {
            return ((srcImage.Width == 288) && (srcImage.Height == 256));
        }

        /// <summary>
        /// R.saga3.12用のデータもお試しで変換してみる。
        /// </summary>
        /// <param name="srcImage"></param>
        /// <returns></returns>
        private static bool IsRSaga3CharaImage(ImageBuffer srcImage)
        {
            return ((srcImage.Width == 176) && (srcImage.Height == 133));
        }

        /// <summary>
        /// RPGツクール2K用の並びから、RPGツクールMV用の並びに変換する。
        /// 具体的には、1キャラクタの並びが上から
        /// 2K) 上向き -> 右向き -> 下向き -> 左向き
        ///  ｜ 
        ///  ↓
        /// MV) 下向き -> 左向き -> 右向き -> 上向き
        /// </summary>
        /// <param name="srcImage">元画像</param>
        /// <returns></returns>
        private static ImageBuffer ReplaceToMV(ImageBuffer srcImage)
        {
            // 背景色を透明にする。
            srcImage.TransparentByPixel(srcImage.GetPixel(0, 0));

            // 並びを変える。
            ImageBuffer replacedImage = new ImageBuffer(srcImage.Width, srcImage.Height);

            int charaHeight = replacedImage.Height / 8;

            for (int yoffs = 0; yoffs < srcImage.Height; yoffs += (charaHeight * 4)) {
                // 上向き
                replacedImage.DrawRectangle(0, yoffs + charaHeight * 3,
                    srcImage, 0, yoffs + charaHeight * 0,
                    srcImage.Width, charaHeight);
                // 右向き
                replacedImage.DrawRectangle(0, yoffs + charaHeight * 2,
                    srcImage, 0, yoffs + charaHeight * 1,
                    srcImage.Width, charaHeight);
                // 下向き
                replacedImage.DrawRectangle(0, yoffs + charaHeight * 0,
                    srcImage, 0, yoffs + charaHeight * 2,
                    srcImage.Width, charaHeight);
                // 左向き
                replacedImage.DrawRectangle(0, yoffs + charaHeight * 1,
                    srcImage, 0, yoffs + charaHeight * 3,
                    srcImage.Width, charaHeight);
            }

            return replacedImage;
        }

        /// <summary>
        /// R.Saga3.12のキャラクタチップから生成する。
        /// 
        /// 
        /// RPG2K相当の画角（キャラクタあたり 24x32）になるように構成。
        /// 
        /// </summary>
        /// <param name="srcImage">元画像</param>
        /// <returns></returns>
        private static ImageBuffer GenerateCharaDataFromRSaga312(ImageBuffer srcImage)
        {
            // 背景色を透明にする。
            srcImage.TransparentByPixel(srcImage.GetPixel(0, 0));
            srcImage.TransparentByPixel(srcImage.GetPixel(1, 1));

            // 並び替えしつつ画像を生成する。
            ImageBuffer replacedImage = new ImageBuffer(288, 256);

            int charWidth = 24;
            int charHeight = 32;

            // 下向き1
            replacedImage.DrawRectangle(charWidth * 0, charHeight * 0,
                srcImage, 1, 1, 24, 8);
            replacedImage.DrawRectangle(charWidth * 0, charHeight * 0 + 8,
                srcImage, 26, 9, 24, 24);
            // 下向き2
            replacedImage.DrawRectangle(charWidth * 1, charHeight * 0,
                srcImage, 1, 1, 24, 32);
            // 下向き3
            replacedImage.DrawRectangle(charWidth * 2, charHeight * 0,
                srcImage, 1, 1, 24, 8);
            replacedImage.DrawRectangle(charWidth * 2, charHeight * 0 + 8,
                srcImage, 51, 9, 24, 32);

            // 左向き1
            replacedImage.DrawRectangle(charWidth * 0, charHeight * 1,
                srcImage, 76, 1, 24, 32);

            // 左向き2
            replacedImage.DrawRectangle(charWidth * 1, charHeight * 1,
                srcImage, 76, 1, 24, 8);
            replacedImage.DrawRectangle(charWidth * 1, charHeight * 1 + 8,
                srcImage, 101, 9, 24, 24);

            // 左向き3
            replacedImage.DrawRectangle(charWidth * 2, charHeight * 1,
                srcImage, 76, 1, 24, 8);
            replacedImage.DrawRectangle(charWidth * 2, charHeight * 1 + 8,
                srcImage, 126, 9, 24, 24);


            // 右向き は左向き画像を反転させて作る
            ImageBuffer rightImage = replacedImage.GetRectangle(
                0, charHeight * 1, charWidth * 3, charHeight).GetFlippedImage(true, false);
            replacedImage.DrawRectangle(charWidth * 0, charHeight * 2, rightImage);


            // 上向き1
            replacedImage.DrawRectangle(charWidth * 0, charHeight * 3,
                srcImage, 1, 34, 24, 8);
            replacedImage.DrawRectangle(charWidth * 0, charHeight * 3 + 8,
                srcImage, 26, 42, 24, 24);

            // 上向き2
            replacedImage.DrawRectangle(charWidth * 1, charHeight * 3,
                srcImage, 1, 34, 24, 32);


            // 上向き3
            replacedImage.DrawRectangle(charWidth * 2, charHeight * 3,
                srcImage, 1, 34, 24, 8);
            replacedImage.DrawRectangle(charWidth * 2, charHeight * 3 + 8,
                srcImage, 51, 42, 24, 24);

            return replacedImage;
        }
    }
}
