using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DoubTech.Senses {
    public class SensableObject : MonoBehaviour {
        public delegate void OnNoLongerSensable();

        public OnNoLongerSensable onNoLongerSensableListener;

        private bool isSensable = true;
        private bool destronNextFrame;

        public bool IsSensable {
            get => isSensable;
            set {
                if (value != isSensable) {
                    isSensable = value;
                    if (!isSensable) onNoLongerSensableListener?.Invoke();
                }
            }
        }

        public void SafelyDestroy() {
            IsSensable = false;
            destronNextFrame = true;
        }

        private void LateUpdate() {
            if (destronNextFrame) Destroy(gameObject);
        }
    }
}
