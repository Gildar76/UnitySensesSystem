#if NODE_CANVAS
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using static DoubTech.Senses.Sense;

namespace DoubTech.Senses.NodeCanvas
{

    [Category("Senses")]
    [Description("Some form of prey was detected by one of the senses")]
    public class SensesTarget : ConditionTask<Sense>
    {
        [SerializeField, ExposeField, Name("Sensed Object")]
        public BBParameter<SensedObject> sensedObject;

        [SerializeField, ExposeField, Name("Sensed Object Position")]
        public BBParameter<Vector3> sensedObjectPosition;

        [SerializeField, ExposeField, Name("Sensed Object Position")]
        public BBParameter<GameObject> sensedGameObject;

        //Use for initialization. This is called only once in the lifetime of the task.
        //Return null if init was successfull. Return an error string otherwise
        protected override string OnInit()
        {
            return null;
        }

        //Called whenever the condition gets enabled.
        protected override void OnEnable()
        {

        }

        //Called whenever the condition gets disabled.
        protected override void OnDisable()
        {

        }

        //Called once per frame while the condition is active.
        //Return whether the condition is success or failure.
        protected override bool OnCheck()
        {
            if (null == agent.NearestSensedObject) {
                sensedObject.value = null;
                sensedGameObject.value = null;
                return false;
            }

            sensedObjectPosition.value = agent.NearestSensedObject.Position;
            sensedObject.value = agent.NearestSensedObject;
            sensedGameObject.value = agent.NearestSensedObject.TargetObject;
            
            return true;
        }
    }
}
#endif
