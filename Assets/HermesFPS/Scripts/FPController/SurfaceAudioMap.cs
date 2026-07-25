using System;
using UnityEngine;

namespace Hermes
{
    [CreateAssetMenu(menuName = "Hermes/Surface Audio Map")]
    public class SurfaceAudioMap : ScriptableObject
    {
        [Serializable]
        public class SurfaceClipSet
        {
            public ESurfaceType SurfaceType;
            public AudioClip[] Footsteps;
            public AudioClip[] Land;
            public AudioClip[] Slide;
            public AudioClip[] WallRunStart;
            public AudioClip[] WallRunLoop;
        }

        public SurfaceClipSet[] Surfaces;

        public SurfaceClipSet GetClipSet(ESurfaceType type)
        {
            if (Surfaces == null) return null;
            foreach (var s in Surfaces)
            {
                if (s.SurfaceType == type) return s;
            }
            return null;
        }
    }
}
