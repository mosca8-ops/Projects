namespace TXT.WEAVR.Procedure
{
    interface IControlledElement
    {
        Controller Controller
        {
            get;
        }
        void OnControllerChanged(ref ControllerChangedEvent e);
    }

    interface IControllerListener
    {
        void OnControllerEvent(ControllerEvent e);
    }

    interface IControlledElement<T> : IControlledElement where T : Controller
    {
        new T Controller
        {
            get;
        }
    }
    interface ISettableControlledElement<T> where T : Controller
    {
        T Controller
        {
            get;
            set;
        }
    }
}
