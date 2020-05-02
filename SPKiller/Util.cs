using GTA;
using GTA.Math;
using GTA.Native;

namespace SPKiller
{
    public static class Util
    {
        public static int PlaySoundFromCoord(Vector3 position, string audioRef, string audioName)
        {
            int soundId = Function.Call<int>(Hash.GET_SOUND_ID);
            Function.Call(Hash.PLAY_SOUND_FROM_COORD, soundId, audioName, position.X, position.Y, position.Z, audioRef, 0, 0, 0);
            return soundId;
        }

        public static void StopSound(ref int soundId)
        {
            if (soundId != -1)
            {
                Audio.StopSound(soundId);
                Audio.ReleaseSound(soundId);

                soundId = -1;
            }
        }

        public static void DisplayHelpTextThisFrame(string gxtEntry)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, gxtEntry);
            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, 1, -1);
        }
    }
}
