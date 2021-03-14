namespace TXT.WEAVR.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class CarouselMenu : MonoBehaviour
    {
        [Draggable]
        private List<Transform> ListMenuOption;

        [Draggable]
        public GameObject CarouselCenter;
        public float CarouselRadius;
        public bool ApplyForceAngle;
        public float ForceAngle;
        public float CarouselRowHeight = 400;
        public float CarouselCurrentRowHeight = 0;
        public float CameraOffsetAngle = 0;

        // Use this for initialization
        void Start()
        {
            ListMenuOption = GetTransformChildrenParent();
        }

        public void CleanCarousel()
        {
            ListMenuOption = GetTransformChildrenParent();
            foreach (var button in ListMenuOption)
            {
                GameObject.Destroy(button.gameObject);
            }
        }

        public void BuildCarousel()
        {
            CarouselCenter = Camera.main.gameObject;
            ListMenuOption = new List<Transform>();
            ListMenuOption = GetTransformChildrenParent();
            if (ListMenuOption.Count > 0)
            {
                Vector3 centerPosition = CarouselCenter.transform.position;

                float angle = 0;
                float lapNumber = 1;

                for (var pointNum = 0; pointNum < ListMenuOption.Count; pointNum++)
                {
                    // "i" now represents the progress around the circle from 0-1
                    // we multiply by 1.0 to ensure we get a fraction as a result.
                    var i = (pointNum * 1.0) / ListMenuOption.Count;
                    // get the angle for this step (in radians, not degrees)
                    if (!ApplyForceAngle)
                    {
                        angle = (float)(i * Mathf.PI * 2);
                    }
                    else
                    {
                        angle = (float)((ForceAngle * pointNum * 1.0) / 360) * Mathf.PI * 2;
                    }

                    var y = CarouselCurrentRowHeight;

                    if (angle >= (Mathf.PI * 2) * lapNumber)
                    {
                        y += CarouselRowHeight;
                        lapNumber++;
                    }

                    // the X  Y position for this angle are calculated using Sin  Cos
                    var x = Mathf.Sin(angle + (((CarouselCenter.transform.localEulerAngles.y)/ 360) * Mathf.PI * 2)) * CarouselRadius;
                    var z = Mathf.Cos(angle + (((CarouselCenter.transform.localEulerAngles.y)/ 360) * Mathf.PI * 2)) * CarouselRadius;
                    var pos = new Vector3(x, CarouselCurrentRowHeight, z) + centerPosition;
                    // no need to assign the instance to a variable unless you're using it afterwards:

                    ListMenuOption[pointNum].transform.position = pos;
                }
            }
        }


        public List<Transform> GetTransformChildrenParent()
        {
            List<Transform> listTransfrom = new List<Transform>();
            foreach (Transform trans in gameObject.transform)
            {
                listTransfrom.Add(trans);
            }

            return listTransfrom;
        }


        public List<Transform> GetTransformChildrenItem()
        {
            List<Transform> listTransfrom = new List<Transform>();
            foreach (Transform transParent in gameObject.transform)
            {
                foreach (Transform transItem in transParent.gameObject.transform)
                {
                    listTransfrom.Add(transItem);
                }
            }

            return listTransfrom;
        }

        void OnEnable()
        {
            //BuildCarousel();
        }
    }
}