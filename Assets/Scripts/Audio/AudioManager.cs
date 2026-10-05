using Hazari.Utilities;
using UnityEngine;

namespace Hazari.Audio
{
    public sealed class AudioManager : PersistentSingleton<AudioManager>
    {
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null)
                return;

            sfxSource.PlayOneShot(clip);
        }
    }
}
