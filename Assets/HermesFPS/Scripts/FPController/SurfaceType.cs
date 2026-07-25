using UnityEngine;

namespace Hermes
{
    public enum ESurfaceType
    {
        Default,
        Stone,
        Tile,
        Carpet,
    }

    public class SurfaceType : MonoBehaviour
    {
        public ESurfaceType Type = ESurfaceType.Default;
    }
}
