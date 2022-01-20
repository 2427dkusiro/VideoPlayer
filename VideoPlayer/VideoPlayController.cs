using FFmpeg.AutoGen;

using FFmpegWraper;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoPlayer
{
    public class VideoPlayController
    {
        private static readonly AVPixelFormat ffPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
        private static readonly PixelFormat wpfPixelFormat = PixelFormats.Bgr24;

        private Decoder decoder;
        private ImageWriter imageWriter;
        private FrameConveter frameConveter;

        public VideoPlayController()
        {

        }

        public void OpenFile(string path)
        {
            decoder = new Decoder();
            decoder.OpenFile(path);
        }

        public WriteableBitmap CreateBitmap(int dpiX, int dpiY)
        {
            if (decoder is null)
            {
                throw new InvalidOperationException("描画先を作成する前に動画を開く必要があります。");
            }
            var context = decoder.VideoCodecContext;
            int width = context.width;
            int height = context.height;

            WriteableBitmap writeableBitmap = new WriteableBitmap(width, height, dpiX, dpiY, wpfPixelFormat, null);
            this.imageWriter = new ImageWriter(width, height, writeableBitmap);

            this.frameConveter = new FrameConveter();
            frameConveter.Configure(context.pix_fmt, context.width, context.height, ffPixelFormat, width, height);

            return writeableBitmap;
        }

        public async Task Play()
        {
            await PlayInternal();
        }

        const int frameCap = 4;
        const int waitTime = 10;
        private bool isFrameEnded;
        private ConcurrentQueue<ManagedFrame> frames = new ConcurrentQueue<ManagedFrame>();

        private async Task PlayInternal()
        {
            Task.Run(() => ReadFrames());

            // init audio(仮実装、メモリ上に全展開するクソコード)
            AudioData firstData;
            using (var _frame = decoder.ReadAudioFrame())
            {
                firstData = AudioFrameConveter.ConvertTo<PCMInt16Format>(_frame);
            }

            MemoryStream stream = new();
            AudioPlayer audioPlayer = new();
            stream.Write(firstData.AsSpan());

            while (true)
            {
                using (var frame2 = decoder.ReadAudioFrame())
                {
                    if (frame2 is null)
                    {
                        break;
                    }
                    using (var audioData2 = AudioFrameConveter.ConvertTo<PCMInt16Format>(frame2))
                    {
                        stream.Write(audioData2.AsSpan());
                    }
                }
            }

            stream.Position = 0;
            var source = audioPlayer.FromInt16(stream, firstData.SampleRate, firstData.Channel);
            firstData.Dispose();
            // end of init audio

            await WaitForBuffer();
            var fps = decoder.VideoStream.r_frame_rate;

            Stopwatch stopwatch = Stopwatch.StartNew();
            int skipped = 0;
            List<double> delays = new();

            for (int i = 0; ; i++)
            {
                TimeSpan time = TimeSpan.FromMilliseconds(fps.den * i * 1000L / (double)fps.num);
                if (stopwatch.Elapsed < time)
                {
                    var rem = time - stopwatch.Elapsed;
                    await Task.Delay(rem);
                }

                if (frames.TryDequeue(out var frame))
                {
                    imageWriter.WriteFrame(frame, frameConveter);
                    if (i == 0)
                    {
                        await audioPlayer.Play(source, 50, 500);
                    }
                    frame.Dispose();
                }
                else
                {
                    if (isFrameEnded)
                    {
                        audioPlayer.Dispose();
                        stream.Dispose();
                        return;
                    }
                    skipped++;
                    Debug.WriteLine($"frame skipped(frame={i},total={skipped}/{i})");
                }
            }
        }

        private async Task WaitForBuffer()
        {
            while (true)
            {
                if (frames.Count == frameCap)
                {
                    return;
                }
                await Task.Delay(waitTime);
            }
        }

        private async Task ReadFrames()
        {
            while (true)
            {
                if (frames.Count < frameCap)
                {
                    var frame = decoder.ReadFrame();
                    if (frame is null)
                    {
                        isFrameEnded = true;
                        return;
                    }
                    frames.Enqueue(frame);
                }
                else
                {
                    await Task.Delay(waitTime);
                }
            }
        }
    }

    public class ImageWriter
    {
        private readonly Int32Rect rect;
        private readonly WriteableBitmap writeableBitmap;

        public ImageWriter(int width, int height, WriteableBitmap writeableBitmap)
        {
            this.rect = new Int32Rect(0, 0, width, height);
            this.writeableBitmap = writeableBitmap;
        }

        public void WriteFrame(ManagedFrame frame, FrameConveter frameConveter)
        {
            var bitmap = writeableBitmap;
            bitmap.Lock();
#warning ここアトミック保証ない
            try
            {
                IntPtr ptr = bitmap.BackBuffer;
                frameConveter.ConvertFrameDirect(frame, ptr);
                bitmap.AddDirtyRect(rect);
            }
            finally
            {
                bitmap.Unlock();
            }
        }
    }
}