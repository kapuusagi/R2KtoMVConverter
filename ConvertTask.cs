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
                Convert(path);
            }

            return true;
        }

        /// <summary>
        /// pathで指定されるファイルを変換し、outputPathに書き出す。
        /// </summary>
        /// <param name="path">ソースファイルパス</param>
        private static void Convert(string path)
        {
            BitmapImage fileImage = new BitmapImage();
            fileImage.BeginInit();
            fileImage.UriSource = new Uri(path);
            fileImage.EndInit();

            WriteableBitmap readImage = new WriteableBitmap(fileImage);

            // ImageUtils.ImageBufferに変換する。
            // ついでに(0,0)画素値を透過するピクセルとして処理し、該当ピクセルを透過する。
            ImageBuffer srcImage = ImageBuffer.FromBitmap(readImage);

            if (IsRPG2KCharachip(srcImage)) {
                ConvertFrom2KCharSet(path, srcImage);
            } else if (IsRSaga3CharaImage(srcImage)) {
                ConvertFromRSagaData(path, srcImage);
            } else {
                throw new NotSupportedException("Target file not supported. : " + path);
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
        /// ツクール2000の歩行データを変換する。
        /// </summary>
        /// <param name="srcPath"></param>
        /// <param name="srcImage"></param>
        private static void ConvertFrom2KCharSet(string srcPath, ImageBuffer srcImage)
        {
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

            string dir = System.IO.Path.GetDirectoryName(srcPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(srcPath);
            string outputPath = System.IO.Path.Combine(dir, fileName + "_rtk2000.png");

            using (var outputStream = System.IO.File.Create(outputPath)) {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(dstImage));
                encoder.Save(outputStream);
            }
        }

        /// <summary>
        /// R.Sagaのデータを変換する。
        /// </summary>
        /// <param name="srcPath">パス</param>
        /// <param name="srcImage">イメージデータ</param>
        private static void ConvertFromRSagaData(string srcPath, ImageBuffer srcImage)
        {
            // 背景色を透明にする。
            srcImage.TransparentByPixel(srcImage.GetPixel(0, 0));
            srcImage.TransparentByPixel(srcImage.GetPixel(1, 1));

            ImageBuffer charaChipData = GenerateCharaChipFromRSaga312(srcImage);
            ImageBuffer battleGraphic = GenerateBattleGraphicFromRSaga312(srcImage);

            // 2倍に拡大する。
            // バイリニアなどの処理は入っていない。
            ImageBuffer x2Image = ImageScaler.Resize(charaChipData, 2.0, 2.0);
            ImageBuffer x2ImageBtl = ImageScaler.Resize(battleGraphic, 2.0, 2.0);

            // 単純な2倍だと、外枠がギタギタなので、外枠を滑らかにする。
            ImageBuffer smoothImage = ImageSmmother.SmoothOuterFrame(x2Image);
            ImageBuffer smoothImageBtl = ImageSmmother.SmoothOuterFrame(x2ImageBtl);

            // 内部に対してフィルタ処理して、多少滑らかにする。
            ImageBuffer filterredImage = ImageFilter.ProcessInternalPart(smoothImage, ImageFilters.M3x3Filter);
            ImageBuffer filterredImageBtl = ImageFilter.ProcessInternalPart(smoothImageBtl, ImageFilters.M3x3Filter);

            // 書き出す。
            BitmapSource dstImage = filterredImage.ToBitmap();

            string dir = System.IO.Path.GetDirectoryName(srcPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(srcPath);
            string outputPath = System.IO.Path.Combine(dir, "$" + fileName + "_rsg.png");

            using (var outputStream = System.IO.File.Create(outputPath)) {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(dstImage));
                encoder.Save(outputStream);
            }

            BitmapSource dstImageBtl = filterredImageBtl.ToBitmap();
            string outPathBtl = System.IO.Path.Combine(dir, fileName + "_btl_rsg.png");
            using (var outputStream = System.IO.File.Create(outPathBtl)) {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(dstImageBtl));
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
        private static ImageBuffer GenerateCharaChipFromRSaga312(ImageBuffer srcImage)
        {
            // 並び替えしつつ画像を生成する。
            ImageBuffer replacedImage = new ImageBuffer(72, 128);

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
        /// <summary>
        /// R.Saga3.12のキャラクタチップから生成する。
        /// 
        /// 
        /// RPG2K相当の画角（キャラクタあたり 24x32）になるように構成。
        /// 
        /// </summary>
        /// <param name="srcImage">元画像</param>
        /// <returns></returns>
        private static ImageBuffer GenerateBattleGraphicFromRSaga312(ImageBuffer srcImage)
        {
            ImageBuffer battleImage = new ImageBuffer(32 * 3 * 3, 32 * 6);

            int leftTopX = 0;
            int leftTopY = 0;

            // 前進
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 76, 34, 24, 32);
            battleImage.DrawRectangle(leftTopX + 36, leftTopY + 0,
                srcImage, 101, 34, 24, 32);
            battleImage.DrawRectangle(leftTopX + 68, leftTopY + 0,
                srcImage, 76, 34, 24, 8);
            battleImage.DrawRectangle(leftTopX + 68, leftTopY + 8,
                srcImage, 126, 42, 24, 24);
            leftTopY += 32;

            // 通常待機
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 76, 1, 24, 8);
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 8,
                srcImage, 101, 9, 24, 24);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            leftTopY += 32;


            // 詠唱
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 51, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 8,
                srcImage, 76, 75, 16, 16);
            battleImage.DrawRectangle(leftTopX + 32 + 4, leftTopY + 0,
                srcImage, 51, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            leftTopY += 32;

            // 防御
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 26, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            leftTopY += 32;

            // 被ダメ
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 126, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 32 + 5, leftTopY + 0,
                srcImage, 126, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 64 + 7, leftTopY + 0,
                srcImage, 126, 67, 24, 32);
            leftTopY += 32;

            // 回避
            battleImage.DrawRectangle(leftTopX + 4 + 2, leftTopY + 0,
                srcImage, 26, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 36 + 3, leftTopY + 0,
                srcImage, 26, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 64 + 4, leftTopY + 0,
                srcImage, 26, 67, 24, 32);
            leftTopX += (32 * 3);
            leftTopY = 0;

            // 突き
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 76, 1, 24, 32);
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 8,
                srcImage, 151, 9, 16, 24);
            battleImage.DrawRectangle(leftTopX + 32 + 2, leftTopY + 0,
                srcImage, 101, 34, 24, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY + 0,
                srcImage, 26, 67, 24, 32);

            leftTopY += 32;

            // 振り
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 76, 1, 24, 32);
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 8,
                srcImage, 126, 9, 24, 24);
            battleImage.DrawRectangle(leftTopX + 32 + 4, leftTopY + 0,
                srcImage, 76, 1, 24, 32);
            battleImage.DrawRectangle(leftTopX + 32 + 4, leftTopY + 8,
                srcImage, 151, 9, 16, 24);
            battleImage.DrawRectangle(leftTopX + 64 + 4, leftTopY + 0,
                srcImage, 26, 67, 24, 32);

            leftTopY += 32;

            // 飛び道具
            battleImage.DrawRectangle(leftTopX + 0 + 4, leftTopY + 0,
                srcImage, 76, 1, 24, 32);
            battleImage.DrawRectangle(leftTopX + 0 + 4, leftTopY + 8,
                srcImage, 151, 9, 16, 24);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);

            leftTopY += 32;

            // 汎用スキル
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 26, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);


            leftTopY += 32;
            // 魔法使用
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 0,
                srcImage, 101, 67, 24, 32);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);

            leftTopY += 32;

            // アイテム使用 (魔法使用をコピー)
            battleImage.DrawRectangle(leftTopX, leftTopY,
                battleImage, 32 * 3 * 1, 32 * 4, 32 * 3, 32);


            leftTopX += (32 * 3);
            leftTopY = 0;

            // 逃げる　(前進の反転)
            battleImage.DrawRectangle(leftTopX, leftTopY,
                battleImage.GetRectangle(0, 0, 32 * 3, 32).GetFlippedImage(true, false));

            leftTopY += 32;

            // 勝利 (防御からコピー)
            battleImage.DrawRectangle(leftTopX, leftTopY,
                battleImage, 0, 32 * 3, 32 * 3, 32);

            leftTopY += 32;

            // 瀕死
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 8,
                srcImage, 151, 75, 24, 24);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);

            leftTopY += 32;

            // 状態異常
            battleImage.DrawRectangle(leftTopX + 4, leftTopY + 8,
                srcImage, 151, 75, 24, 24);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);

            leftTopY += 32;

            // 睡眠
            battleImage.DrawRectangle(leftTopX + 0, leftTopY + 16,
                srcImage, 51, 100, 32, 16);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            leftTopY += 32;

            // 戦闘不能
            battleImage.DrawRectangle(leftTopX + 0, leftTopY + 16,
                srcImage, 51, 100, 32, 16);
            battleImage.DrawRectangle(leftTopX + 32, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);
            battleImage.DrawRectangle(leftTopX + 64, leftTopY,
                battleImage, leftTopX, leftTopY, 32, 32);

            return battleImage;
        }
    }
}
