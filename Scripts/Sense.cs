using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DoubTech.Senses
{
    public abstract class Sense : MonoBehaviour
    {
        public delegate void OnSensedNewObjectsEvent(HashSet<SensedObject> newObjects);
        public delegate void OnNearestSensedObjectChanged(SensedObject oldObject, SensedObject newObject);
        public delegate void OnSensedObjectForgotten(SensedObject sensedObject);
        public delegate bool ValidTargetCheck(GameObject sensedObject);

        [Header("Vision")]
        [Tooltip("A transform representing this creature's eyes")]
        [SerializeField]
        protected Transform eyes;

        [Tooltip("The maximum fov this creature can see at once")]
        [SerializeField]
        protected float fieldOfView = 110;

        [Tooltip("The maximum range that this creature can see other creatures")]
        [Header("Senses Range")]
        [SerializeField]
        protected float visionRange = 100f;

        [Tooltip("The maximum range this creature can smell other creatures or scent trails")]
        [SerializeField]
        protected float scentRange = 0;

        [Tooltip("The maximum distance a creature can hear. This is used for filtering. There will be additional falloff and volume level filtering.")]
        [SerializeField]
        protected float hearingRange = 10f;

        [Tooltip("The distance from this creature that the creature knows there is another creature present. This fills any gaps from other senses that may not be 'true to life accurate' This should probably not be any larger than 1-2m")]
        [SerializeField]
        protected float implicitDetectionRange = 2f;

        [Header("Targeting")]
        [SerializeField]
        protected LayerMask targetMask;
        [SerializeField, Tooltip("If checked the sense object will use a sphere collider to find all game objects within the target mask range. If this is not checked only objects with a SensableObject component will be tracked and distance will be ignored until sense evaluation.")]
        protected bool useSphereCast = true;
        [SerializeField, Tooltip("The amount of time the nearest target should be considered still detected after it is no longer sensed")]
        protected float timeToRetainNearest = 5;
        [SerializeField]
        protected float scentAccuracyRadius = 2;


        public OnSensedNewObjectsEvent sensedNewObjectsListener;
        public OnNearestSensedObjectChanged nearestSensedChanged;
        public OnSensedObjectForgotten sensedObjectForgottenListener;
        public ValidTargetCheck sensedObjectValidationListener;

        protected float maxSenseRange;

        protected HashSet<GameObject> seenObjects = new HashSet<GameObject>();
        protected HashSet<GameObject> heardObjects = new HashSet<GameObject>();

        protected HashSet<GameObject> smelledObjects = new HashSet<GameObject>();
        protected HashSet<GameObject> implicitDetectedObjects = new HashSet<GameObject>();

        protected Dictionary<GameObject, SensedObject> rememberedObjects = new Dictionary<GameObject, SensedObject>();

        public int LayerMask => targetMask;
        public bool SensesSomething => null != NearestSensedObject;
        public SensedObject NearestSensedObject { get; protected set; }
        public SortedList<SensedObject, SensedObject> SensedObjects { get; protected set; } = new SortedList<SensedObject, SensedObject>();
        public bool HasAdrenaline { get; set; } = false;

        public delegate void NoLongerSensableEvent(GameObject gameObject);
        public static NoLongerSensableEvent OnNoLongerSensable;

        public bool showGizmos;
        public bool Senses(GameObject gameObject) {
            return rememberedObjects.ContainsKey(gameObject);
        }

        public SensedObject this[GameObject gameObject] {
            get {
                SensedObject so;
                if(rememberedObjects.TryGetValue(gameObject, out so)) {
                    return so;
                }

                return null;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            maxSenseRange = Mathf.Max(visionRange, scentRange, hearingRange);
        }

        private void OnEnable() {
            Reset();
            OnNoLongerSensable += Forget;
        }

        private void OnDisable() {
            OnNoLongerSensable -= Forget;
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            OnUpdateSenses();
        }

        protected abstract void OnUpdateSenses();

        /// <summary>
        /// Fully reset the senses state. Callbacks will be left intact, but no callbacks will be issued.
        /// </summary>
        public void Reset()
        {
            SensedObjects.Clear();
            rememberedObjects.Clear();
            seenObjects.Clear();
            heardObjects.Clear();
            smelledObjects.Clear();
            implicitDetectedObjects.Clear();
        }

        public void Forget(GameObject gameObject)
        {
            SensedObject so;
            if (rememberedObjects.TryGetValue(gameObject, out so)) Forget(so);
        }

        public void Forget(SensedObject target) {
            if (!SensedObjects.ContainsKey(target)) return;

            GameObject go = target.TargetObject;
            SensedObjects.Remove(target);
            if (null != go) {
                rememberedObjects.Remove(go);
                seenObjects.Remove(go);
                heardObjects.Remove(go);
                smelledObjects.Remove(go);
                implicitDetectedObjects.Remove(go);
            }

            if (NearestSensedObject == target) {
                var e = SensedObjects.GetEnumerator();
                e.MoveNext();
                NearestSensedObject = e.Current.Value;
                nearestSensedChanged?.Invoke(target, NearestSensedObject);
            }
            sensedObjectForgottenListener?.Invoke(target);
        }
        protected bool RememberSensedObject(GameObject targetObject, out SensedObject sensedObject) {
            if (!rememberedObjects.TryGetValue(targetObject, out sensedObject)) {
                sensedObject = new SensedObject(targetObject);
                if (null != sensedObject.sensableObject && !sensedObject.sensableObject.IsSensable) {
                    var forgettable = sensedObject;
                    sensedObject.sensableObject.onNoLongerSensableListener += () => {
                        Forget(forgettable);
                    };
                }
                rememberedObjects[targetObject] = sensedObject;
                return true;
            }

            return false;
        }

        protected void OnSensedNewObjects(HashSet<SensedObject> newlySensedObjects)
        {
            sensedNewObjectsListener?.Invoke(newlySensedObjects);
        }

        public bool CanSee(GameObject target)
        {
            Vector3 startVec = eyes.position;
            Vector3 startVecFwd = eyes.forward;

            Vector3 rayDirection = target.transform.position - startVec;

            // If the ObjectToSee is close to this object and is in front of it, then return true
            return Vector3.Angle(rayDirection, startVecFwd) < fieldOfView;

            // TODO: Check if object is actually visible in line of sight
        }

        private void OnDrawGizmos() {
            if (!showGizmos) return;

            if (null != NearestSensedObject) {
                Gizmos.DrawSphere(NearestSensedObject.Position, .3f);
            }
        }

        void OnDrawGizmosSelected() {
            if (!showGizmos) return;

            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, maxSenseRange);
            if (eyes != null)
            {
                Gizmos.DrawRay(eyes.transform.position, eyes.transform.forward);
            }

            foreach (var pos in rememberedObjects.Values)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireSphere(pos.Position, .9f);
            }
            foreach (var obj in seenObjects)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(obj.transform.position, 1f);
            }
            foreach (var obj in implicitDetectedObjects)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(obj.transform.position, .5f);
            }
            foreach (var obj in heardObjects)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(obj.transform.position, 1.1f);
            }
            foreach (var obj in smelledObjects)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(obj.transform.position, 1.2f);
            }

            if (null != NearestSensedObject)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(NearestSensedObject.Position, .95f);
            }
        }

        public class SensedObject : System.IComparable<SensedObject>
        {
            internal float distance;
            internal Vector3 position;
            internal Vector3 actualPosition;
            internal GameObject target;
            internal SensableObject sensableObject;

            internal bool heard;
            internal bool seen;
            internal bool smelled;
            internal bool implicitlyDetected;

            internal float lastDetection;

            public float Distance => distance;
            public Vector3 Position => position;
            public GameObject TargetObject => target;
            public Vector3 ActualPosition => actualPosition;

            public SensedObject(GameObject target)
            {
                this.target = target;
                sensableObject = target.GetComponent<SensableObject>();
            }

            private int CompareSense(bool a, bool b)
            {
                int cmp = 0;
                if (a && !b) cmp = -1;
                if (b && !a) cmp = 1;
                return cmp;
            }

            public int CompareTo(SensedObject other)
            {
                if (other.target == target) return 0;


                int cmp = distance.CompareTo(other.distance);

                if (cmp == 0) {

                    // Next compare last detection. If the other was not sensed as recently that means it wasn't
                    // detected this frame and is now a memory. Scent, hearing, and immediate sense should keep this fresh
                    // for near targets so this really only applies to objects that are farther away
                    cmp = -lastDetection.CompareTo(other.lastDetection);
                }

                // If multiple targets are the same distance away we'll compare
                // senses based on sense accuracy
                if (cmp == 0)
                {
                    cmp = CompareSense(implicitlyDetected, other.implicitlyDetected);
                }

                if (cmp == 0)
                {
                    cmp = CompareSense(seen, other.seen);
                }

                if (cmp == 0)
                {
                    cmp = CompareSense(heard, other.heard);
                }

                if (cmp == 0)
                {
                    cmp = CompareSense(smelled, other.smelled);
                }

                // Lastly we'll provide a constant comparison between the two objects so sorting
                // has somewhere to finish. We'll use instance ids for this comparison.
                if (cmp == 0)
                {
                    cmp = target.GetInstanceID().CompareTo(other.target.GetInstanceID());
                }

                return cmp;
            }


            public override bool Equals(object obj)
            {
                if (obj is SensedObject)
                {
                    return target == ((SensedObject)obj).target;
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}