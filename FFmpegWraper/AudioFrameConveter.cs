using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegWraper
{
    public class AudioFrameConveter
    {
        public static unsafe AudioData ConvertTo<TOut>(ManagedFrame frame) where TOut : OutputFormat, new()
        {
            return ConvertTo<TOut>(frame.Frame);
        }

        public static unsafe AudioData ConvertTo<TOut>(AVFrame* frame) where TOut : OutputFormat, new()
        {
            var output = new TOut();
            var context = ffmpeg.swr_alloc();

            ffmpeg.av_opt_set_int(context, "in_channel_layout", (long)frame->channel_layout, 0);
            ffmpeg.av_opt_set_int(context, "out_channel_layout", (long)frame->channel_layout, 0);
            ffmpeg.av_opt_set_int(context, "in_sample_rate", frame->sample_rate, 0);
            ffmpeg.av_opt_set_int(context, "out_sample_rate", frame->sample_rate, 0);
            ffmpeg.av_opt_set_sample_fmt(context, "in_sample_fmt", (AVSampleFormat)frame->format, 0);
            ffmpeg.av_opt_set_sample_fmt(context, "out_sample_fmt", output.AVSampleFormat, 0);
            ffmpeg.swr_init(context);

            int size = output.SizeOf;

            int bufferSize = frame->nb_samples * frame->channels * size;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            byte* ptr = (byte*)buffer.ToPointer();

            ffmpeg.swr_convert(context, &ptr, frame->nb_samples, frame->extended_data, frame->nb_samples);

            return new AudioData()
            {
                Samples = frame->nb_samples,
                SampleRate = frame->sample_rate,
                Channel = frame->channels,
                SizeOf = size,
                Data = buffer,
            };
        }
    }

    public class AudioData : IDisposable
    {
        public int Samples { get; set; }
        public int SampleRate { get; set; }
        public int Channel { get; set; }
        public int SizeOf { get; set; }

        public IntPtr Data { get; set; }

        public unsafe ReadOnlySpan<byte> AsSpan()
        {
            return new ReadOnlySpan<byte>(Data.ToPointer(), Samples * Channel * SizeOf);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Data);
        }
    }

    public abstract class OutputFormat
    {
        public abstract AVSampleFormat AVSampleFormat { get; }

        public abstract int SizeOf { get; }
    }

    public class PCMInt16Format : OutputFormat
    {
        public PCMInt16Format() { }

        public override AVSampleFormat AVSampleFormat => AVSampleFormat.AV_SAMPLE_FMT_S16;
        public override int SizeOf => sizeof(ushort);
    }
}
