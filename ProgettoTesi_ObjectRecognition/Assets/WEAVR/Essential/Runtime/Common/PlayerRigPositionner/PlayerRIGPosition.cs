using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TXT.WEAVR.Interaction;


namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("")]
    public class PlayerRIGPosition : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        public GameObject PlayerRigCanvas;
        [SerializeField]
        [Draggable]
        public GameObject PlayerRig;



        private bool m_IsActive;
        private Button TranslationButton;
        private Button RotationButton;
        private Transform Arrows;
        private Animator ArrowAnimator;
        private Animator TranslationAnimator;
        private Animator RotationAnimator;
        private Text TranslationText;
        private Text RotationText;

        private bool m_translationActive = true;
        private bool m_keyBoardActive = false;

        private bool Keyboard
        {
            get { return m_keyBoardActive; }
            set
            {
                if (m_keyBoardActive != value)
                {
                    ArrowAnimator.Rebind();

                    if (value == true)
                        Arrows.gameObject.SetActive(true);
                    else
                        Arrows.gameObject.SetActive(false);

                    m_keyBoardActive = value;

                }
            }
        }

        public bool Active
        {
            get { return m_IsActive; }
            set
            {
                m_IsActive = value;

                if (m_IsActive)
                    PlayerRigCanvas.SetActive(true);
                else
                    PlayerRigCanvas.SetActive(false);
            }
        }


        public delegate void CurrentMove(int axis, float step);
        CurrentMove Move;

        // Start is called before the first frame update
        void Start()
        {
            Move = RotateAxis;

            if (!File.Exists(Application.streamingAssetsPath + "/playerRIG_position.json"))
            {
                if (!File.Exists(Application.streamingAssetsPath))
                    Directory.CreateDirectory(Application.streamingAssetsPath);

                JsonSeralize();
            }
            else
                JsonDeseralize();

            Active = Active;
        }

        private void Reset()
        {
            PlayerRigCanvas = GameObject.Find("/WEAVR/PlayerRigCanvas");
        }

        private void OnValidate()
        {
            if (!PlayerRigCanvas)
            {
                PlayerRigCanvas = GameObject.Find("/WEAVR/PlayerRigCanvas");
            }
            if (PlayerRigCanvas)
            {
                if (TranslationButton == null)
                {
                    TranslationButton = PlayerRigCanvas.transform.Find("TranslationButton").GetComponent<Button>();
                    if (TranslationButton)
                    {
                        TranslationAnimator = TranslationButton.GetComponentInChildren<Animator>();
                        TranslationText = TranslationButton.GetComponentInChildren<Text>();
                    }
                }

                if (RotationButton == null)
                {
                    RotationButton = PlayerRigCanvas.transform.Find("RotationButton").GetComponent<Button>();
                    if (RotationButton)
                    {
                        RotationAnimator = RotationButton.GetComponent<Animator>();
                        RotationText = RotationButton.GetComponentInChildren<Text>();
                    }
                }

                if (Arrows == null)
                {
                    Arrows = PlayerRigCanvas.transform.Find("ArrowGroup");
                    if (Arrows)
                    {
                        ArrowAnimator = Arrows.GetComponent<Animator>();
                    }
                }
            }
        }



        void OnApplicationQuit()
        {
            JsonSeralize();
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown("h"))
            {
                if(Active)
                {
                    RotationAnimator.Rebind();
                    TranslationAnimator.Rebind();
                    TranslationText.text = "T";
                    RotationText.text = "R";
                    Keyboard = false;
                    ArrowAnimator.Rebind();
                }
                Active = !Active;
            }

            if (Active)
            { 
                if (Input.GetKeyDown("r"))
                    ShowButtonRotation();

                if (Input.GetKeyDown("t"))
                    ShowButtonTranslation();

                if (Keyboard)
                {
                    if (Input.GetKey("up") || Input.GetKey("w"))
                    {
                        ArrowAnimator.SetTrigger("ForwardArrow");
                        Move(1, 0.1f);
                    }
                    if (Input.GetKey("down") || Input.GetKey("s"))
                    {
                        ArrowAnimator.SetTrigger("BackwardArrow");
                        Move(1, -0.1f);
                    }
                    if (Input.GetKey("space"))
                    {
                        ArrowAnimator.SetTrigger("UpArrow");
                        Move(0, 0.1f);
                    }
                    if (Input.GetKey("right ctrl") || Input.GetKey("left ctrl"))
                    {
                        ArrowAnimator.SetTrigger("DownArrow");
                        Move(0, -0.1f);
                    }
                    if (Input.GetKey("left") || Input.GetKey("a"))
                    {
                        ArrowAnimator.SetTrigger("LeftArrow");
                        Move(2, -0.1f);
                    }
                    if (Input.GetKey("right") || Input.GetKey("d"))
                    {
                        ArrowAnimator.SetTrigger("RightArrow");
                        Move(2, 0.1f);
                    }
                }
            }
        }

        public void RotateAxis(int axis, float step)
        {
            step *= -5f;

            Vector3 angleToAdd = PlayerRig.transform.eulerAngles;
            angleToAdd[axis] += step;
            PlayerRig.transform.rotation = Quaternion.Euler(angleToAdd);
        }

        public void TranslateAxis(int axis, float step)
        {
            if (axis == 0)
                axis = 1;
            else if (axis == 2)
                axis = 0;
            else if (axis == 1)
                axis = 2;

            Vector3 positionToAdd = new Vector3(0, 0, 0);
            positionToAdd[axis] += step;
            PlayerRig.transform.Translate(positionToAdd  * 50 * Time.deltaTime, Space.Self);
        }

        private void JsonSeralize()
        {
            SeralizedInfo data = new SeralizedInfo();

            data.position = PlayerRig.transform.position;
            data.rotation = PlayerRig.transform.eulerAngles;
            data.m_IsActive = Active;

            string json = JsonUtility.ToJson(data);
            File.WriteAllText(Application.streamingAssetsPath + "/playerRIG_position.json", json);

        }

        private void JsonDeseralize()
        {
            string json  = File.ReadAllText(Application.streamingAssetsPath + "/playerRIG_position.json");

            if(json != null)
            {
                SeralizedInfo jsonCatcher = JsonUtility.FromJson<SeralizedInfo>(json);
                PlayerRig.transform.position = jsonCatcher.position;
                PlayerRig.transform.rotation = Quaternion.Euler(jsonCatcher.rotation);
                Active = jsonCatcher.m_IsActive;
            }
        }

        public void ShowButtonRotation()
        {
            RotationAnimator.SetTrigger("Pressed");

            if (RotationAnimator.GetBool("IsPressed"))
            {
                RotationText.text = "R";
                Keyboard = false;
            }
            else if(!RotationAnimator.GetBool("IsPressed"))
            {
                RotationText.text = "Rotation";
                Keyboard = true;
                Move = RotateAxis;

                //Check if the other animation is not active
                if (TranslationAnimator.GetBool("IsPressed"))
                {
                    TranslationAnimator.SetTrigger("PressedReturn");
                    TranslationText.text = "T";
                }
            }
        }

        public void ShowButtonTranslation()
        {
            TranslationAnimator.SetTrigger("Pressed");

            if (TranslationAnimator.GetBool("IsPressed"))
            {
                TranslationText.text = "T";
                Keyboard = false;
            }
            else if (!TranslationAnimator.GetBool("IsPressed"))
            {
                TranslationText.text = "Translation";
                Keyboard = true;
                Move = TranslateAxis;

                //Check if the other animation is not active
                if (RotationAnimator.GetBool("IsPressed"))
                {
                    RotationAnimator.SetTrigger("PressedReturn");
                    RotationText.text = "R";
                }
            }
        }
    }

   

    public class SeralizedInfo
    {
        public Vector3 position;
        public Vector3 rotation;
        public bool m_IsActive;  
    }
}