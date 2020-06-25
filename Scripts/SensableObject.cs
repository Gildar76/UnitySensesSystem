using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DoubTech.Senses {
    public class SensableObject : MonoBehaviour {
        public static GameObject[] Registry = new GameObject[0];
        private static readonly HashSet<GameObject> gameObjects = new HashSet<GameObject>();
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

        private void Awake() {
            gameObjects.Add(gameObject);
            Array.Resize(ref Registry, gameObjects.Count);
            Registry[Registry.Length - 1] = gameObject;
        }

        public void SafelyDestroy() {
            IsSensable = false;
            destronNextFrame = true;
        }

        private void LateUpdate() {
            if (destronNextFrame) {
                Destroy(gameObject);
                gameObjects.Remove(gameObject);
                Registry = gameObjects.ToArray();
            }
        }
    }
}
