using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoPlayer
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly MainWindowViewModelContext context;
        public MainWindowViewModel(MainWindowViewModelContext mainWindowViewModelContext)
        {
            this.context = mainWindowViewModelContext;
        }

        public class MainWindowViewModelContext
        {
            public int DpiX { get; set; }
            public int DpiY { get; set; }
        }

        public async Task DebugMain()
        {
            VideoPlayController videoPlayController = new VideoPlayController();

            // const string path = @"E:\一般動画\原神\musics\【原神】稲妻OST戦闘曲MV「斬霧破竹」-EzQfeJTHD3M.mkv";
            // FHD24pのMV

            // const string path = @"E:\一般動画\原神\musics\【原神】OST selection　世を見渡す神岩の旅路-PvWXaRMxfSA.webm";
            // 4k60pのMV、負荷テスト用

            // const string path = @"E:\一般動画\【音MAD】5WAY帝君【原神】-tXqQbMxpSAI.mkv";
            // FHD60p音MAD、音ズレ確認用

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (System.IO.Directory.Exists(@"E:\一般動画"))
            {
                openFileDialog.InitialDirectory = @"E:\一般動画";
            }
            bool? result = openFileDialog.ShowDialog();
            if (result ?? false)
            {
                string path = openFileDialog.FileName;

                videoPlayController.OpenFile(path);
                Bitmap = videoPlayController.CreateBitmap(context.DpiX, context.DpiY);
                await videoPlayController.Play();
            }
        }

        private WriteableBitmap bitmap;
        public WriteableBitmap Bitmap
        {
            get => bitmap;
            set => SetProperty(ref bitmap, value);
        }
    }
}