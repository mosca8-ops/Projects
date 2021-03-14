using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    interface IPortsProvider { }

    interface IOutputPortsProvider : IPortsProvider
    {
        IEnumerable<IPortController> OutputPorts { get; }
    }

    interface IInputPortsProvider : IPortsProvider
    {
        IEnumerable<IPortController> InputPorts { get; }
    }

    interface IPortController
    {
        Object PortModel { get; }
        void OnTransitionDisabled(TransitionController transition);
    }

    interface ISinglePortController : IPortController
    {
        TransitionController TransitionController { get; }
    }

    interface IMultiPortController
    {
        IEnumerable<TransitionController> TransitionsControllers { get; }
    }
}
