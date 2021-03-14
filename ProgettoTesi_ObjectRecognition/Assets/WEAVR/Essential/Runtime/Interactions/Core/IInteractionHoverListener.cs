using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Interaction
{

    public interface IInteractionHoverListener
    {
        void OnHoverEnter(AbstractInteractionController controller);
        void OnHoverExit(AbstractInteractionController controller);
    }
}
