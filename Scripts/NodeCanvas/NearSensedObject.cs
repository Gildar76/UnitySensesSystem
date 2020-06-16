using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ThreeDtbd.Character;
using UnityEngine;
using static ThreeDtbd.Character.Sense;

namespace ThreeDtbd.BT.Conditions
{

	[Category("Senses")]
	[Description("Player is near sensed object")]
	public class NearSensedObject : ConditionTask<Sense>{
        [SerializeField, ExposeField, Name("Distance to Object")]
        BBParameter<float> distance;

        //Use for initialization. This is called only once in the lifetime of the task.
        //Return null if init was successfull. Return an error string otherwise
        protected override string OnInit(){
			return null;
		}

		//Called whenever the condition gets enabled.
		protected override void OnEnable(){
			
		}

		//Called whenever the condition gets disabled.
		protected override void OnDisable(){
			
		}

		//Called once per frame while the condition is active.
		//Return whether the condition is success or failure.
		protected override bool OnCheck(){
			var sensed = agent.NearestSensedObject;
            if (null == sensed) return false;

            float d = Vector3.Distance(agent.transform.position, sensed.Position);
			return d < distance.value;
		}
	}
}