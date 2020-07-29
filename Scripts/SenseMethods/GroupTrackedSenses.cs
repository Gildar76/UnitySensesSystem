using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DoubTech.Senses;
using DoubTech.Senses.Collections;

namespace DoubTech.Senses.SenseMethods
{
    public abstract class GroupTrackedSenses<T> : Sense where T : ITransformProvider
    {
        public abstract KdTree<T> Enemies { get; }
        public abstract Vector3 Position { get; }

        protected override void OnUpdateSenses() {
            Enemies.UpdatePositions(.5f);
            Enemies.FindClose(Position);
        }
    }
}
