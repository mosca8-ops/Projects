using System.Collections;
using UnityEngine;
using System.Linq;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Interactions/Hand")]
    public partial class VR_Hand :

#if WEAVR_VR
        Hand
#else 
        MonoBehaviour
#endif
    {
        private ObjectsBag.Hand m_hand;

#if WEAVR_VR


        public bool m_usePoserHoverComponent = false;
        public Transform m_PoserHoverTransform = null;
        public float m_PoserHoverRadius = 0.2f;

        private Transform m_defaultHandAttachmentPoint = null;
        //this flag is used to force the usage of the attachmentpoint also when a skeleton is used
        //it seems the default logic of steamVR is to use the attachmentpoint only when a skeleton pose is not active
        private bool m_forceAttachmentPoint = false;
        private bool m_parentToHand = false;
        private bool m_useRotationAxis = false;
        private Vector3 m_rotationAxis = new Vector3(0.0f, 0.0f, 0.0f);

        private VR_Skeleton_Poser m_lastPoser = null;
        private Collider[] m_poserColliders = new Collider[20];
        private GameObject m_menuPointerGO = null;
        private bool m_menuHand;

        private VR_SimplePointer m_menuPointer = null;
        private VR_Skeleton_Poser m_menuPose = null;
        private Transform m_lastInteractable = null;
        private VR_Object m_closestInteractable = null;
        private const float c_invalidDistance = -1.0f;
        private float m_initialDistanceToInteractable = c_invalidDistance;
        private float m_distanceToInteractable = c_invalidDistance;
        private bool m_handGlued = false;
        private bool m_wasHandGlued = false;
        private float m_HandGluedTime;
        [Tooltip("Controller height threshold, below this value the hovering radius is increased proportionally to ease interaction with object on the floor")]
        public float m_TerrainInteractionThreshold = 0.2f;
        private bool m_terrainCorrectionEnabled;
        private float m_previousHoverInteractionRadius;
        [Tooltip("Value of Controller height to be considered as floor")]
        public float m_zeroTerrainDistance;
        [Tooltip("Value of Hovering radius when hand is in proximity of terrain")]
        public float m_terrainHoverRadius = 0.2f;

        private bool onPath = false;

        public bool IsMenuHand
        {
            get
            {
                return m_menuHand;
            }
            set
            {
                m_menuHand = value;
                
            }
        }

        private bool HandGlued
        {
            get
            {
                return m_handGlued;
            }
            set {
                m_handGlued = value;
                if (m_handGlued != m_wasHandGlued)
                {
                    m_HandGluedTime = Time.time;
                }
                m_wasHandGlued = m_handGlued;
            }
        }

        protected override IEnumerator Start()
        {
            if (!TryRegisterViveHand())
            {
                m_hand = BagHolder.Main.Bag.RegisterHand(this);
            }
            //m_zeroTerrainDistance = transform.parent.position.y;
            m_menuPointerGO = new GameObject();
            m_menuPointerGO.SetActive(false);
            m_menuPointerGO.name = "ZAxisFix";
            m_menuPointerGO.transform.parent = null;
            m_menuPointerGO.transform.localPosition = new Vector3(0, 0, 0);
            m_menuPointerGO.transform.localScale = new Vector3(1, 1, 1);
            m_menuPointerGO.transform.localRotation = Quaternion.Euler(0, 90, 0);
            m_menuPointer = m_menuPointerGO.AddComponent<VR_SimplePointer>();
            m_menuPointer.Initialize(this);
            m_menuHand = false;
            return base.Start();

        }
        private bool TryRegisterViveHand()
        {
            var hand = GetComponent<Hand>();
            if (hand)
            {
                m_hand = BagHolder.Main.Bag.RegisterHand(hand);
                return true;
            }
            return false;
        }

        public void StartAttachmentPointOverride(Transform iAttachmentPoint, bool iParentToHand)
        {
            m_forceAttachmentPoint = true;
            m_parentToHand = iParentToHand;
            m_defaultHandAttachmentPoint = objectAttachmentPoint ?? transform;
            objectAttachmentPoint = iAttachmentPoint;
            m_useRotationAxis = false;
        }

        public void StartAttachmentPointOverride(Transform iAttachmentPoint, bool iParentToHand, Vector3 iRotationAxis)
        {
            StartAttachmentPointOverride(iAttachmentPoint, iParentToHand);
            m_useRotationAxis = true;
            m_rotationAxis = iRotationAxis;
        }

        public void StopAttachmentPointOverride()
        {
            m_forceAttachmentPoint = false;
            objectAttachmentPoint = m_defaultHandAttachmentPoint ?? transform;
        }

        protected virtual bool CheckHoveringForTransform(Vector3 hoverPosition, float iRadius, ref float closestDistance, ref VR_Object closestInteractable)
        {
            bool foundCloser = false;
            // null out old vals
            for (int i = 0; i < m_poserColliders.Length; ++i)
            {
                m_poserColliders[i] = null;
            }

            int numColliders = Physics.OverlapSphereNonAlloc(hoverPosition, iRadius, m_poserColliders);

            // DebugVar
            int iActualColliderCount = 0;

            // Pick the closest hovering
            for (int colliderIndex = 0; colliderIndex < numColliders; colliderIndex++)
            {
                Collider collider = m_poserColliders[colliderIndex];

                if (collider == null)
                    continue;

                VR_Object contacting = collider.GetComponentInParent<VR_Object>();

                // Yeah, it's null, skip
                if (contacting == null)
                    continue;

                // Ignore this collider for hovering
                IgnoreHovering ignore = collider.GetComponent<IgnoreHovering>();
                if (ignore != null)
                {
                    if (ignore.onlyIgnoreHand == null || ignore.onlyIgnoreHand == this)
                    {
                        continue;
                    }
                }

                // Can't hover over the object if it's attached
                bool hoveringOverAttached = false;
                for (int attachedIndex = 0; attachedIndex < AttachedObjects.Count; attachedIndex++)
                {
                    if (AttachedObjects[attachedIndex].attachedObject == contacting.gameObject)
                    {
                        hoveringOverAttached = true;
                        break;
                    }
                }
                if (hoveringOverAttached)
                    continue;

                // Occupied by another hand, so we can't touch it
                if (otherHand && otherHand.hoveringInteractable == contacting)
                    continue;

                // Best candidate so far...
                float distance = Vector3.Distance(contacting.transform.position, hoverPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = contacting;
                    foundCloser = true;
                }
                iActualColliderCount++;
            }
            return foundCloser;
        }

        private Transform GetBone(Valve.VR.SteamVR_Skeleton_JointIndexEnum iIndex)
        {
            return mainRenderModel ? mainRenderModel.GetBone((int)iIndex) : null;
        }

        public void SetPoser(VR_Skeleton_Poser iNewPose)
        {
            m_lastPoser = iNewPose;
            skeleton.BlendToPoser(m_lastPoser, 0.1f);
        }

        public void UpdatePose()
        {
            float wBlendAmount = 0.0f;
            if (m_initialDistanceToInteractable > 0)
            {
                wBlendAmount = m_distanceToInteractable / m_initialDistanceToInteractable;
                if (wBlendAmount < 0)
                {
                    wBlendAmount = 0.0f;
                }
                else if (wBlendAmount > 1)
                {
                    wBlendAmount = 1.0f;
                }
            }
            skeleton.BlendTo(wBlendAmount, 0.1f);
        }

        public void RestoreDefaultPose()
        {
            if (skeleton != null && m_lastPoser != null &&
                AttachedObjects.Count == 0 &&
                (m_closestInteractable == null ||
                (m_closestInteractable.GetInteractionMode() == VR_Object.InteractionMode.GlueToObject && m_handGlued == false)))
            {
                if (m_menuPointerGO.activeSelf)
                {
                    if (m_menuPose != null)
                    {
                        HideController();
                        SetPoser(m_menuPose);
                    }
                }
                else
                {
                    skeleton.BlendToSkeleton(0.2f);
                    ShowController();
                    m_lastPoser = null;
                }
                m_distanceToInteractable = c_invalidDistance;
                HandGlued = false;
                useHoverSphere = true;
                useFingerJointHover = true;
                fingerJointHover = Valve.VR.SteamVR_Skeleton_JointIndexEnum.indexTip;
            }
        }

        private bool IsFirstInteractionFrame()
        {
            return m_distanceToInteractable < 0.0f;
        }

        private void HandleGlueToObjectPose(VR_Object iVRObject, VR_Skeleton_Poser iPose)
        {
            HandGlued = m_distanceToInteractable < iVRObject.GetGlueHandOnHoverDistance();
            if (HandGlued && !object.ReferenceEquals(iPose, m_lastPoser))
            {
                HideController();
                SetPoser(iPose);
            }
            else
            {
                RestoreDefaultPose();
            }
        }

        private void HandleBlendToFinalPose(VR_Skeleton_Poser iPose)
        {
            HandGlued = false;
           
            HideController();
            if (AttachedObjects.Count == 0)
            {
                if (!object.ReferenceEquals(iPose, m_lastPoser))
                {
                    SetPoser(iPose);
                }
                UpdatePose();
            }
        }

        private void SetFingerInteraction(Valve.VR.SteamVR_Skeleton_JointIndexEnum iIndexJoint)
        {
            useHoverSphere = false;
            useFingerJointHover = true;
            fingerJointHover = iIndexJoint;
        }

        private void HandleInteractable(VR_Object iVRObject)
        {
            if (iVRObject != null && skeleton != null)
            {
                VR_Skeleton_Poser wCurPoser = iVRObject.GetCurrentPoser();
                if (wCurPoser != null)
                {
                    Collider wCollider = iVRObject.GetComponentInChildren<Collider>();
                    float wDistance;
                    if (wCollider != null)
                    {
                        wDistance = Vector3.Distance(wCollider.ClosestPoint(transform.position), transform.position);
                    }
                    else
                    {
                        wDistance = Vector3.Distance(transform.position, iVRObject.transform.position);
                    }
                    if (IsFirstInteractionFrame())
                    {
                        m_initialDistanceToInteractable = wDistance;
                    }
                    m_distanceToInteractable = wDistance;
                    switch (iVRObject.GetInteractionMode())
                    {
                        case VR_Object.InteractionMode.GlueToObject:
                            HandleGlueToObjectPose(iVRObject, wCurPoser);
                            break;
                        case VR_Object.InteractionMode.BlendToFinalPose:
                            HandleBlendToFinalPose(wCurPoser);
                            break;
                        default:
                            break;
                    }
                    switch (iVRObject.GetHoveringMode())
                    {
                        case VR_Object.HoveringMode.Finger:
                            SetFingerInteraction(iVRObject.GetFingerJointHovering());
                            break;
                    }
                }
                else
                {
                    RestoreDefaultPose();
                }
            }
            else
            {
                RestoreDefaultPose();
            }
        }

        protected override void FixedUpdate()
        {

        }

        protected override void UpdateHovering()
        {
           
            VR_Hand wOtherHand = otherHand as VR_Hand;

            if (wOtherHand != null && !wOtherHand.IsMenuModeActive())
            {
                if (objectAttachmentPoint != null && m_forceAttachmentPoint && currentAttachedObject != null && !onPath)
                {
                    if (m_parentToHand)
                    {
                        currentAttachedObject.transform.localPosition = objectAttachmentPoint.localPosition;
                        currentAttachedObject.transform.localRotation = objectAttachmentPoint.localRotation;
                    }
                }
                base.UpdateHovering();
                if (!HandGlued)
                {
                    m_closestInteractable = hoveringInteractable as VR_Object;
                    if (AttachedObjects.Count == 0 && m_closestInteractable == null && m_usePoserHoverComponent && m_PoserHoverTransform != null)
                    {
                        float closestDistance = float.MaxValue;
                        CheckHoveringForTransform(m_PoserHoverTransform.position, m_PoserHoverRadius, ref closestDistance, ref m_closestInteractable);
                    }
                }
                else if (hoveringInteractable != null && !Object.ReferenceEquals(hoveringInteractable, m_closestInteractable))
                {
                    hoveringInteractable = m_closestInteractable;
                }

                if (m_closestInteractable != null)
                {
                    m_lastInteractable = m_closestInteractable.transform;
                    HandleInteractable(m_closestInteractable);
                }
                else
                {
                    RestoreDefaultPose();
                }
            }
            else
            {
                RestoreDefaultPose();
            }
        }

        public void SwitchParentTransform(GameObject objectToDetach, bool toHand)
        {
            var foundObject = AttachedObjects.FirstOrDefault(a => a.attachedObject == objectToDetach);

            if (foundObject.Equals(default))
            {
                return;
            }

            if (toHand && !foundObject.isParentedToHand)
            {
                return;
            }

            if (!toHand)
            {
                Transform parentTransform = null;
                if (foundObject.isParentedToHand)
                {
                    if (foundObject.originalParent != null)
                        parentTransform = foundObject.originalParent.transform;

                    if (foundObject.attachedObject != null)
                        foundObject.attachedObject.transform.parent = parentTransform;
                }

                onPath = true;
            }
            else
            {
                foundObject.attachedObject.transform.parent = this.transform;
                foundObject.attachedObject.transform.localPosition = Vector3.zero;

                onPath = false;
            }
        }

        public RenderModel GetRender()
        {
            return mainRenderModel;
        }

        private void UpdateRendererModel(Transform iAttachmentPoint,
                                         bool iObjectToHand,
                                         bool iUseRotationAxis,
                                         float iStartTime,
                                         float wDuration,
                                         Transform iReferenceTransform)
        {
            if (iObjectToHand && currentAttachedObject != null && !onPath)
            {
                currentAttachedObject.transform.localPosition = iAttachmentPoint.localPosition;
                currentAttachedObject.transform.localRotation = iAttachmentPoint.localRotation;
            }
            else
            {
                Vector3 wNewPosition = iAttachmentPoint.position;
                Quaternion wNewRotation = iAttachmentPoint.rotation;
                if (iReferenceTransform != null && iStartTime > 0)
                {
                    float wDeltaTime = Time.time - iStartTime;
                    float wPercentage = wDeltaTime / wDuration;
                    wNewPosition = Vector3.Lerp(iReferenceTransform.position, iAttachmentPoint.position, wPercentage);
                    wNewRotation = Quaternion.Lerp(iReferenceTransform.rotation, iAttachmentPoint.rotation, wPercentage);
                }
                if (mainRenderModel)
                {
                    if (iUseRotationAxis)
                    {
                        //Vector3 wRotationAxis = m_rotationAxis.normalized;
                        Vector3 wDirection = GetDirection();
                        Vector3 wDirectionZProjected = Vector3.ProjectOnPlane(wDirection, Vector3.up);
                        float wYaw = Vector2.SignedAngle(new Vector2(wDirectionZProjected.x, wDirectionZProjected.z), Vector2.right) + 90.0f;
                        float wPitch = Vector3.SignedAngle(wDirectionZProjected, wDirection, Vector3.Cross(wDirectionZProjected, wDirection));
                        wPitch = wDirection.y >= 0 ? -wPitch : wPitch;
                        iAttachmentPoint.transform.parent.rotation = Quaternion.Euler(m_rotationAxis.x * wPitch,
                                                                          m_rotationAxis.y * wYaw,
                                                                          0.0f);
                    }
                    mainRenderModel.SetHandPosition(wNewPosition);
                    mainRenderModel.SetHandRotation(wNewRotation);
                }
                if (hoverhighlightRenderModel != null)
                {
                    hoverhighlightRenderModel.SetHandPosition(wNewPosition);
                    hoverhighlightRenderModel.SetHandRotation(wNewRotation);
                }
            }
        }

        protected override void HandFollowUpdate()
        {
            base.HandFollowUpdate();
            if (AttachedObjects.Count != 0 && objectAttachmentPoint != null && m_forceAttachmentPoint)
            {
                UpdateRendererModel(objectAttachmentPoint, m_parentToHand, m_useRotationAxis, -1.0f, 0.0f, null);
            }
            else if (HandGlued && m_closestInteractable is VR_Object wClosestVRObject)
            {
                UpdateRendererModel(wClosestVRObject.GetAttachmentPoint(this),
                                    wClosestVRObject.IsHandParent(),
                                    wClosestVRObject.HasRotationAxis(),
                                    m_HandGluedTime,
                                    wClosestVRObject.GetGlueHandTime(),
                                    transform);
            }
            else
            {
                UpdateRendererModel(transform, false, false, -1.0f, 0.1f, null);
            }
        }

        public VR_Hand GetOtherHand()
        {
            VR_Hand wRet = null;
            if (otherHand != null)
            {
                wRet = otherHand as VR_Hand;
            }
            return wRet;
        }

        public void EnterMenuMode(VR_Skeleton_Poser iMenuPose)
        {
            Transform wBoneTransform = GetBone(Valve.VR.SteamVR_Skeleton_JointIndexEnum.indexDistal);
            if (wBoneTransform)
            {
                if (iMenuPose)
                {
                    m_menuPose = iMenuPose;
                    SetPoser(m_menuPose);
                    m_menuPointerGO.transform.position = wBoneTransform.position;
                    m_menuPointerGO.transform.rotation = wBoneTransform.rotation;
                    m_menuPointerGO.transform.parent = wBoneTransform;
                    m_menuPointerGO.transform.localPosition = new Vector3(0, 0, 0);
                    m_menuPointerGO.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    m_menuPointer.pointer = m_menuPointerGO;
                    m_menuPointerGO.SetActive(true);
                    HideController();
                }
                else
                {
                    // todo
                }

            }
        }

        public bool IsMenuModeActive()
        {
            return m_menuPointerGO.activeSelf;
        }

        public void ExitMenuMode()
        {
            if (m_menuPointerGO != null)
            {
                m_menuPose = null;
                m_menuPointerGO.transform.parent = null;
                m_menuPointerGO.SetActive(false);
                RestoreDefaultPose();
            }
        }

        private void HandleTerrainInteraction()
        {



            if (transform.localPosition.y < m_TerrainInteractionThreshold)
            {
                if (m_terrainHoverRadius < hoverSphereRadius)
                {
                    m_terrainCorrectionEnabled = false;
                    hoverSphereRadius = m_previousHoverInteractionRadius;
                    return;
                }
                if (!m_terrainCorrectionEnabled)
                {
                    m_terrainCorrectionEnabled = true;
                    m_previousHoverInteractionRadius = hoverSphereRadius;
                }

                if (m_terrainCorrectionEnabled)
                {
                    if (transform.localPosition.y < m_zeroTerrainDistance)
                    {
                        hoverSphereRadius = m_terrainHoverRadius;
                    }
                    else
                    {
                        hoverSphereRadius = (m_terrainHoverRadius - m_previousHoverInteractionRadius) *
                                             (1 - Mathf.Abs(transform.localPosition.y - m_zeroTerrainDistance) / Mathf.Abs(m_TerrainInteractionThreshold - m_zeroTerrainDistance)) + m_previousHoverInteractionRadius;
                    }
                }

            }
            else
            {
                if (m_terrainCorrectionEnabled)
                {
                    m_terrainCorrectionEnabled = false;
                    hoverSphereRadius = m_previousHoverInteractionRadius;
                }
            }
        }


        protected override void Update()
        {
            base.Update();
            HandleTerrainInteraction();
            if (hoveringInteractable != null)
            {
                ControllerButtonHints wControllerButtonHints = gameObject.transform.GetComponentInChildren<ControllerButtonHints>();
                if (wControllerButtonHints != null)
                {
                    Valve.VR.SteamVR_RenderModel wControllerRenderer = wControllerButtonHints.GetComponentInChildren<Valve.VR.SteamVR_RenderModel>();
                    if (wControllerRenderer != null)
                    {
                        wControllerRenderer.gameObject.SetActive(false);
                    }
                }
            }
        }

        public Vector3 GetDirection()
        {
            return (transform.TransformPoint(new Vector3(0, 0, 1.0f)) - transform.TransformPoint(new Vector3(0, 0, 0))).normalized;
        }


        protected override void OnDrawGizmos()
        {
            if (m_PoserHoverTransform != null)
            {
                // Draw a yellow cube at the transform position
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(m_PoserHoverTransform.position, m_PoserHoverRadius);
            }
        }

#else
        void Start()
        {
            if (!TryRegisterViveHand())
            {
                m_hand = BagHolder.Main.Bag.RegisterHand(this);
            }
        }
        private bool TryRegisterViveHand()
        {
            return false;
        }
#endif

        public void MakeActive(bool enable)
        {
            m_hand?.MakeActive(enable);
        }
    }
}
