using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DoubTech.Senses.SenseMethods
{
    public class IndividuallyTrackedSenses : Sense
    {
        private int frame;
        private int frequency = 4;

        protected float lastSenseTime;
        protected readonly int nearbySenseCapacity = 10;

        protected override void OnUpdateSenses() {
            if (!HasAdrenaline && frame++ < frequency) {
                return;
            }
            frame = 0;
            float nearest = float.MaxValue;

            Collider[] colliders = null;
            GameObject[] gameObjects = null;
            int possibleTargetCount;
            if (useSphereCast) {
                colliders = Physics.OverlapSphere(transform.position, maxSenseRange, targetMask);
                possibleTargetCount = colliders.Length;
            } else {
                gameObjects = SensableObject.Registry;
                possibleTargetCount = gameObjects.Length;
            }
            seenObjects.Clear();
            heardObjects.Clear();
            smelledObjects.Clear();

            // TODO: We may want to think about creating a new set each frame. This was just updating
            // the set, but I was getting a concurrent modification exception from AnimalUtilityData since
            // it is operating in a coroutine instead of on the main thread. Let's discuss how to properly
            // optimize this. We could potentially go with a static list and create a copy for consumers
            // that request the list when they want it. We can also optimize consumers of the list to use
            // the OnNewObjectsSensed listener for updates
            var sensedObjects = new SortedSet<SensedObject>();
            var newlySensedObjects = new HashSet<SensedObject>();
            float detectionTime = Time.fixedTime;

            for (int i = 0; i < possibleTargetCount; i++) {
                GameObject targetObject = useSphereCast ? colliders[i].gameObject : gameObjects[i];
                SensedObject sensedObject;

                float actualDistance = Vector3.Distance(targetObject.transform.position, transform.position);
                if (actualDistance > maxSenseRange) continue;

                if (targetObject == gameObject) continue;
                if (null != sensedObjectValidationListener && !sensedObjectValidationListener(targetObject)) continue;


                sensedObject = RememberSensedObject(targetObject);

                float sensedDistance = actualDistance;

                // Detection from least accurate to most.
                if (actualDistance < scentRange) {
                    smelledObjects.Add(targetObject);
                    if (sensedObject.actualPosition != targetObject.transform.position) {
                        sensedObject.position = targetObject.transform.position + UnityEngine.Random.insideUnitSphere * scentAccuracyRadius;
                    }
                    sensedDistance = Vector3.Distance(targetObject.transform.position, transform.position);
                    sensedObject.smelled = true;
                }
                if (CanSee(targetObject)) {
                    seenObjects.Add(targetObject);
                    sensedObject.position = targetObject.transform.position;
                    sensedObject.seen = true;
                }
                if (actualDistance < implicitDetectionRange) {
                    implicitDetectedObjects.Add(targetObject);
                    sensedObject.position = targetObject.transform.position;
                    sensedObject.implicitlyDetected = true;
                }

                sensedObject.actualPosition = targetObject.transform.position;
                sensedObject.distance = sensedDistance;

                if (lastSenseTime != sensedObject.lastDetection) {
                    newlySensedObjects.Add(sensedObject);
                }

                sensedObject.lastDetection = detectionTime;
                SensedObjects[sensedObject] = sensedObject;

                while (SensedObjects.Count > nearbySenseCapacity) {
                    SensedObjects.RemoveAt(nearbySenseCapacity);
                }
            }
            if (newlySensedObjects.Count > 0) {
                OnSensedNewObjects(newlySensedObjects);
            }

            lastSenseTime = detectionTime;

            SensedObject old = NearestSensedObject;
            NearestSensedObject = SensedObjects.Count > 0 ? SensedObjects.First().Value : null;

            if (null != NearestSensedObject && Time.fixedTime - NearestSensedObject.lastDetection > timeToRetainNearest) {
                NearestSensedObject = null;
                nearest = float.PositiveInfinity;
                nearestSensedChanged?.Invoke(old, null);
            } else if (old != NearestSensedObject) {
                nearestSensedChanged?.Invoke(old, null);
            }
        }

        private SensedObject RememberSensedObject(GameObject targetObject) {
            SensedObject sensedObject;
            if (!rememberedObjects.TryGetValue(targetObject, out sensedObject)) {
                sensedObject = new SensedObject(targetObject);
                if (null != sensedObject.sensableObject && !sensedObject.sensableObject.IsSensable) {
                    sensedObject.sensableObject.onNoLongerSensableListener += () => {
                        Forget(sensedObject);
                    };
                }
                rememberedObjects[targetObject] = sensedObject;
            }

            return sensedObject;
        }
    }
}
