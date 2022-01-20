using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using FFmpeg.AutoGen;

namespace FFmpegWraper
{
    /// <summary>
    /// フレームを変換する機能を提供します。
    /// </summary>
    public unsafe class FrameConveter : IDisposable
    {
        public FrameConveter() { }

        private AVPixelFormat srcFormat;
        private int srcWidth;
        private int srcHeight;
        private AVPixelFormat distFormat;
        private int distWidth;
        private int distHeight;

        private SwsContext* swsContext;

        /// <summary>
        /// フレームの変換を設定します。
        /// </summary>
        /// <param name="srcFormat">変換元のフォーマット。</param>
        /// <param name="srcWidth">変換元の幅。</param>
        /// <param name="srcHeight">変換元の高さ。</param>
        /// <param name="distFormat">変換先のフォーマット。</param>
        /// <param name="distWidth">変換先の幅。</param>
        /// <param name="distHeight">変換先の高さ。</param>
        public void Configure(AVPixelFormat srcFormat, int srcWidth, int srcHeight, AVPixelFormat distFormat, int distWidth, int distHeight)
        {
            this.srcFormat = srcFormat;
            this.srcWidth = srcWidth;
            this.srcHeight = srcHeight;
            this.distFormat = distFormat;
            if (this.distWidth == distWidth || this.distHeight == distHeight)
            {
                return;
            }
            this.distWidth = distWidth;
            this.distHeight = distHeight;

            ffmpeg.sws_freeContext(swsContext);
            swsContext = ffmpeg.sws_getContext(srcWidth, srcHeight, srcFormat, distWidth, distHeight, distFormat, 0, null, null, null);
        }

        /// <summary>
        /// フレームを変換します。
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public unsafe byte* ConvertFrame(ManagedFrame frame)
        {
            return ConvertFrame(frame.Frame);
        }

        /// <summary>
        /// フレームを変換します。
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public unsafe byte* ConvertFrame(AVFrame* frame)
        {
            byte_ptrArray4 data = default;
            int_array4 lizesize = default;
            byte* buffer = (byte*)ffmpeg.av_malloc((ulong)ffmpeg.av_image_get_buffer_size(distFormat, srcWidth, srcHeight, 1));
            ffmpeg.av_image_fill_arrays(ref data, ref lizesize, buffer, distFormat, srcWidth, srcHeight, 1)
                .OnError(() => throw new InvalidOperationException("フレームスケーリング用バッファの確保に失敗しました。"));
            ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, srcHeight, data, lizesize)
                .OnError(() => throw new InvalidOperationException("フレームのスケーリングに失敗しました。"));
            return buffer;
        }

        /// <summary>
        /// フレームを変換します。変換したフレームを指定したバッファに直接書き込みます。
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public unsafe void ConvertFrameDirect(ManagedFrame frame, IntPtr buffer)
        {
            ConvertFrameDirect(frame.Frame, (byte*)buffer.ToPointer());
        }

        /// <summary>
        /// フレームを変換します。変換したフレームを指定したバッファに直接書き込みます。
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public unsafe void ConvertFrameDirect(ManagedFrame frame, byte* buffer)
        {
            ConvertFrameDirect(frame.Frame, buffer);
        }

        /// <summary>
        /// フレームを変換します。変換したフレームを指定したバッファに直接書き込みます。
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public unsafe void ConvertFrameDirect(AVFrame* frame, byte* buffer)
        {
            byte_ptrArray4 data = default;
            int_array4 lizesize = default;
            ffmpeg.av_image_fill_arrays(ref data, ref lizesize, buffer, distFormat, srcWidth, srcHeight, 1)
                .OnError(() => throw new InvalidOperationException("フレームスケーリング用バッファの確保に失敗しました。"));
            ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, srcHeight, data, lizesize)
                .OnError(() => throw new InvalidOperationException("フレームのスケーリングに失敗しました。"));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeUnManaged();
            GC.SuppressFinalize(this);
        }

        ~FrameConveter()
        {
            DisposeUnManaged();
        }

        private bool isDisposed = false;
        private void DisposeUnManaged()
        {
            if (isDisposed) { return; }
            ffmpeg.sws_freeContext(swsContext);
            isDisposed = true;
        }
    }
}
