using UnityEngine;

namespace Hermes
{
    public class FPActivity : Activity
    {
        protected FPController Controller;

        protected FPControllerPreset Preset => Controller.Preset;

        protected virtual void Awake()
        {
            Controller = GetComponentInParent<FPController>();
        }
    }
}
