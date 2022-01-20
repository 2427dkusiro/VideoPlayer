using NAudio.Wave;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VideoPlayer
{
    class AudioPlayer
    {
        public AudioPlayer() { }

        private IWavePlayer output;

        public async Task Play(IWaveProvider waveProvider, int latency, int delay)
        {
            output = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, latency);
            output.Init(waveProvider);
            await Task.Delay(delay);
            output.Play();
        }

        public IWaveProvider FromInt16(Stream stream, int sampleRate, int channel)
        {
            var provider = new RawSourceWaveStream(stream, new WaveFormat(sampleRate, 16, channel));
            return provider;
        }

        public void Dispose()
        {
            output.Dispose();
        }
    }
}
