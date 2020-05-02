using System;
using GTA.Math;
using SPKiller.Classes;

namespace SPKiller.Managers
{
    public static class LocationManager
    {
        private static readonly Random _rng = new Random();

        #region Methods
        public static Location GetClueLocation(int index)
        {
            if (index < 0 || index >= _clueLocations.Length)
            {
                return null;
            }

            return _clueLocations[index];
        }

        public static Location GetVanLocation(int index)
        {
            if (index < 0 || index >= _vanLocations.Length)
            {
                return null;
            }

            return _vanLocations[index];
        }

        public static int GetRandomVanIndex()
        {
            return _rng.Next(0, _vanLocations.Length);   
        }

        public static Location GetRewardLocation(int index)
        {
            if (index < 0 || index >= _rewardLocations.Length)
            {
                return null;
            }

            return _rewardLocations[index];
        }
        #endregion

        #region Clue locations
        // Limb -> writing -> handprint -> machete
        private static readonly Location[] _clueLocations =
        {
            new Location(new Vector3(1111.436f, 3143.763f, 37.26f), new Vector3(57.659f, -117.4738f, -12.265f), 0.0f),
            new Location(new Vector3(-133.811f, 1912.405f, 197.5978f), new Vector3(0.0f, 0.0f, -89.8002f), 0.0f),
            new Location(new Vector3(-679.3499f, 5800.786f, 17.5134f), new Vector3(0f, 0f, 155.5997f), 0.0f),
            new Location(new Vector3(1904.069f, 4912.708f, 49.0629f), new Vector3(0.3f, -20.0012f, -103.5004f), 0.0f)
        };
        #endregion

        #region Van locations
        private static readonly Location[] _vanLocations =
        {
            new Location(new Vector3(2899.191f, 3652.564f, 43.7877f), Vector3.Zero, 185.7f),
            new Location(new Vector3(2567.877f, 1265.993f, 43.6561f), Vector3.Zero, 13.6996f),
            new Location(new Vector3(-1568.4f, 4420.712f, 5.9114f), Vector3.Zero, 184.0995f),
            new Location(new Vector3(-1708.313f, 2617.513f, 1.9614f), Vector3.Zero, 221.8996f),
            new Location(new Vector3(2438.552f, 5833.117f, 57.6787f), Vector3.Zero, 202.6991f)
        };
        #endregion

        #region Reward locations
        private static readonly Location[] _rewardLocations =
        {
            new Location(new Vector3(-815.8118f, 181.4038f, 76.4106f), new Vector3(-90.0f, -61.0f, 0.0f), 0.0f),
            new Location(new Vector3(-9.3134f, -1433.617f, 30.8715f), new Vector3(-90.0f, 45.0f, 0.0f), 0.0f),
            new Location(new Vector3(1970.4990f, 3814.7153f, 34.0063f), new Vector3(-90.0f, 117.0f, 0.0f), 0.0f),
            new Location(new Vector3(96.2785f, -1294.009f, 29.1188f), new Vector3(-90.0f, 27.0f, 0.0f), 0.0f),
            new Location(new Vector3(0.9167f, 527.6237f, 170.5274f), new Vector3(-90.0f, -23.0f, 0.0f), 0.0f)
        };
        #endregion
    }
}