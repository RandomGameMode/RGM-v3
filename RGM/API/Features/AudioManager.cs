using System;
using Exiled.API.Features;

namespace RGM.API.Features
{
    /**
     * <summary>오디오 관련된 작업을 처리합니다</summary>
     */
    public static class AudioManager
    {
        /**
         * <summary>AudioPlayer의 확장 메서드로서 클립을 로드하는 기능을 추가합니다</summary>
         * <param name="audioPlayer">AudioPlayer 인스턴스</param>
         * <param name="clipName">재생할 클립 이름</param>
         * <param name="volume">음량 (기본 1)</param>
         * <param name="loop">반복 재생 여부</param>
         * <param name="destroyOnEnd">재생 후 클립 객체 제거</param>
         * <returns>AudioClipPlayback 을 반환합니다</returns>
         */
        public static AudioClipPlayback TryPlay(this AudioPlayer audioPlayer, string clipName, float volume = 1, bool loop = false, bool destroyOnEnd = true)
        {
            if (audioPlayer == null)
            {
                Log.Warn($"Cannot play audio clip '{clipName}': the audio player is unavailable.");
                return null;
            }

            string audioDir = Paths.Plugins + "/audio/";
            string clipPath = System.IO.Path.Combine(audioDir, clipName + ".ogg");

            if (!System.IO.File.Exists(clipPath))
                return null;

            try
            {
                if (!AudioClipStorage.AudioClips.TryGetValue(clipName, out var clip) || clip == null)
                    AudioClipStorage.LoadClip(clipPath, clipName);

                if (!AudioClipStorage.AudioClips.TryGetValue(clipName, out clip) || clip == null)
                {
                    Log.Warn($"Cannot play audio clip '{clipName}': loading '{clipPath}' failed.");
                    return null;
                }

                return audioPlayer.AddClip(clipName, volume, loop, destroyOnEnd);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to play audio clip '{clipName}' from '{clipPath}': {exception}");
                return null;
            }
        }
    }
}