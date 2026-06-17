namespace _001_Scripts.Interface
{
    public interface IAudioService
    {
        void PlayBGM(string bgmName);
        void PlaySFX(string sfxName);
        void StopSound(); 
        void SetVolume(float volume);
    }
}