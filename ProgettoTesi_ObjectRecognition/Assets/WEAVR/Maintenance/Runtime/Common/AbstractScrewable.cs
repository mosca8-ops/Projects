namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    public enum ScrewDirection
    {
        Clockwise = -1,
        CounterClockwise = 1,
    }

    public abstract class AbstractScrewable : AbstractInteractiveBehaviour
    {
        [Space]
        public float value;
        public ScrewDirection screwDirection = ScrewDirection.Clockwise;

        [Header("Intervals")]
        public Span limits = new Span(0, 100);
        [InclusiveSpan("limits")]
        public Span valid = new Span(50, 70);
        [InclusiveSpan("limits")]
        public Span critical = new Span(70, 100);

        [Header("Colors")]
        public Color validColor = Color.green;
        public Color criticalColor = Color.red;

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return false;
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return "Screwable";
        }

        public override void Interact(ObjectsBag currentBag)
        {
            // Nothing
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public bool IsCriticalValue {
            get { return critical.IsValid(value); }
        }

        public bool IsValidValue {
            get { return valid.IsValid(value); }
        }
    }
}