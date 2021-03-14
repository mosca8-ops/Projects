namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    [AddComponentMenu("")]
    public class SimpleWrench : MaintenanceTool
    {
        public float screwTest;

        private void Connectable_ConnectionChanged(AbstractConnectable source, AbstractInteractiveBehaviour previous, AbstractInteractiveBehaviour current, AbstractConnectable otherConnectable) {
            Debug.Log("Connected to: " + current);
            if (current == null) {
                return;
            }

            //var screw = current.GetComponent<Screw>();
            ValueChangerMenu.Show(transform, true, "Fasten Bolt", screwTest, 0.2f, v => screwTest = v);
        }

        private void Execute() {
            ValueChangerMenu.Show(transform, true, "Fasten Bolt", screwTest, 0.2f, v => screwTest = v);
        }

        // Update is called once per frame
        void Update() {

        }

        protected override ObjectClass GetDefaultObjectClass() {
            return new ObjectClass() { type = "SimpleWrench" };
        }

        private bool Executable_ConditionToExecute(GameObject arg1, ObjectsBag arg2) {
            return _connectable.IsConnected;
        }

        protected override Type[] GetRequiredComponentsTypes() {
            return null;
        }

        protected override void SetupEventHandlers() {
            _connectable.ConnectionChanged -= Connectable_ConnectionChanged;
            _connectable.ConnectionChanged += Connectable_ConnectionChanged;
        }
    }
}