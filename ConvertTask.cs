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
                string outputPath = System.IO.Path.Combine(dir, fileName + ".rtk2000.png");

                Convert(path, outputPath);
            }

            return true;
        }

        /// <summary>
        /// 変換する
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
            srcImage.TransparentByPixel(srcImage.GetPixel(0, 0));

            // RPGツクール2000の歩行グラフィック並び順を
            // RPGツクールMV用のものに変更する。
            ImageBuffer replacedImage = ReplaceToMV(srcImage);


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
        /// RPGツクール2K用の並びから、RPGツクールMV用の並びに変換する。
        /// 具体的には、1キャラクタの並びが上から
        /// 2K) 上向き -> 右向き -> 下向き -> 左向き
        ///  ｜ 
        ///  ↓
        /// MV) 下向き -> 左向き -> 右向き -> 上向き
        /// </summary>
        /// <param name="srcImage"></param>
        /// <returns></returns>
        private static ImageBuffer ReplaceToMV(ImageBuffer srcImage)
        {
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

    }
}
