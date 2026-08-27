namespace AstraNope.Contracts
{
    public interface IAudioPlayback
    {
        void PlayBGM(string bgmName);
        void PlaySFX(string sfxName);
        void StopSound();
    }

    public interface IAudioMixerControl
    {
        void SetVolume(float volume);
    }

    public interface IAudioService : IAudioPlayback, IAudioMixerControl { }
}
