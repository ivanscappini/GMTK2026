using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    public class FPControllerModule : MonoBehaviour
    {
        protected FPController Controller;

        protected FPControllerPreset Preset => Controller.Preset;


        protected virtual void Awake()
        {
            Controller = GetComponentInParent<FPController>();
        }
    }
}
