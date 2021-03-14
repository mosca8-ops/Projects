namespace TXT.WEAVR.Animation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Assertions;

    public delegate void OnDestinationReached(GameObject gameObject, Transform target);

    [Obsolete("Use Animation Engine which is newer and more customizable")]
    [AddComponentMenu("")]
    public class ObjectAnimator : MonoBehaviour
    {
        private const float sqrConnectEpsilon = 0.0001f;
        private const float connectRotationEpsilon = 0.03f * Mathf.Rad2Deg;

        #region [  STATIC PART  ]
        private static ObjectAnimator _main;
        /// <summary>
        /// Gets the first instantiated or found object mover
        /// </summary>
        public static ObjectAnimator Main {
            get {
                if (_main == null) {
                    _main = FindObjectOfType<ObjectAnimator>();
                    if (_main == null) {
                        // Create a default one
                        var go = new GameObject("ObjectMover");
                        _main = go.AddComponent<ObjectAnimator>();
                        _main.Awake();
                    }
                }
                return _main;
            }
        }
        #endregion

        private int _nextAvailableId;
        private Dictionary<GameObject, MovingObject> _movingObjects;
        private Dictionary<int, MovingObject> _movingObjectsWithKeys;
        private List<GameObject> _keysToRemove;

        private void Awake() {
            if(_main == null) {
                _main = this;
            }
            _nextAvailableId = 1;
            _movingObjects = new Dictionary<GameObject, MovingObject>();
            _keysToRemove = new List<GameObject>();
        }

        void FixedUpdate() {
            _keysToRemove.Clear();
            foreach(var keyPair in _movingObjects) {
                if(keyPair.Key == null) {
                    _keysToRemove.Add(keyPair.Key);
                    continue;
                }
                // Update the movement animation
                keyPair.Value.AnimateMovement(Time.fixedDeltaTime);
                if (keyPair.Value.destinationReached) {
                    _keysToRemove.Add(keyPair.Key);
                }
            }

            // Remove the destroyed gameobjects
            foreach(var key in _keysToRemove) {
                _movingObjects.Remove(key);
            }
        }

        /// <summary>
        /// Stops the previously initiated movement on the specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The gameobject to stop movement</param>
        public void StopMovement(GameObject gameObject) {
            MovingObject obj = null;
            if (_movingObjects.TryGetValue(gameObject, out obj)) {
                obj.Stop(false);
                _movingObjects.Remove(gameObject);
            }
        }

        /// <summary>
        /// Translates and rotates the specified object to match the destination
        /// </summary>
        /// <param name="gameObject">The gameobject to translate and rotate</param>
        /// <param name="destination">The destination to reach</param>
        /// <param name="offset">The offset of the gameobject to take into account</param>
        /// <param name="linearVelocity">The translation velocity. Setting 0 will instantly translate to destination</param>
        /// <param name="rotationVelocity">The rotation velocity. Setting 0 will instatntly rotate the object</param>
        /// <param name="callback">The callback when destination is reached</param>
        public void MoveAndRotate(GameObject gameObject, Transform destination, Transform offset, float linearVelocity, float rotationVelocity, OnDestinationReached callback) {
            MovingObject obj = null;
            if (!_movingObjects.TryGetValue(gameObject, out obj)) {
                obj = new MovingObject(gameObject);
                _movingObjects[gameObject] = obj;
            }
            obj.ChangeDestination(destination, offset);
            obj.DestinationReached = callback;
            obj.moveSpeed = linearVelocity;
            obj.rotateSpeed = rotationVelocity;
            obj.rotationEnabled = true;
        }

        /// <summary>
        /// Translates and rotates the specified object to match the destination
        /// </summary>
        /// <param name="gameObject">The gameobject to translate and rotate</param>
        /// <param name="destination">The destination to reach</param>
        /// <param name="offset">The offset of the gameobject to take into account</param>
        /// <param name="velocity">The translation and rotation velocity. Setting 0 will instantly translate and rotate to destination</param>
        /// <param name="callback">The callback when destination is reached</param>
        public void MoveAndRotate(GameObject gameObject, Transform destination, Transform offset, float velocity, OnDestinationReached callback) {
            MoveAndRotate(gameObject, destination, offset, velocity, ConvertLinearToRadial(velocity, offset, destination), callback);
        }

        /// <summary>
        /// Translates and rotates the specified object to match the destination
        /// </summary>
        /// <param name="gameObject">The gameobject to translate and rotate</param>
        /// <param name="destination">The destination to reach</param>
        /// <param name="velocity">The translation and rotation velocity. Setting 0 will instantly translate and rotate to destination</param>
        /// <param name="callback">The callback when destination is reached</param>
        public void MoveAndRotate(GameObject gameObject, Transform destination, float velocity, OnDestinationReached callback) {
            MoveAndRotate(gameObject, destination, null, velocity, ConvertLinearToRadial(velocity, gameObject.transform, destination), callback);
        }

        /// <summary>
        /// Translates and rotates the specified object to match the destination
        /// </summary>
        /// <param name="gameObject">The gameobject to translate and rotate</param>
        /// <param name="destination">The destination to reach</param>
        /// <param name="velocity">The translation and rotation velocity. Setting 0 will instantly translate and rotate to destination</param>
        public void MoveAndRotate(GameObject gameObject, Transform destination, float velocity) {
            MoveAndRotate(gameObject, destination, null, velocity, ConvertLinearToRadial(velocity, gameObject.transform, destination), null);
        }

        /// <summary>
        /// Translates the specified object to match the destination
        /// </summary>
        /// <param name="gameObject">The gameobject to translate and rotate</param>
        /// <param name="destination">The destination to reach</param>
        /// <param name="offset">The offset of the gameobject to take into account</param>
        /// <param name="speed">The translation velocity. Setting 0 will instantly translate to destination</param>
        /// <param name="callback">The callback when destination is reached</param>
        public void Move(GameObject gameObject, Transform destination, Transform offset, float speed, OnDestinationReached callback) {
            MovingObject obj = null;
            if(!_movingObjects.TryGetValue(gameObject, out obj)) {
                obj = new MovingObject(gameObject);
                _movingObjects[gameObject] = obj;
            }
            obj.ChangeDestination(destination, offset);
            obj.DestinationReached = callback;
            obj.moveSpeed = speed;
            obj.rotationEnabled = false;
        }

        /// <summary>
        /// Translates the specified object to match the destination
        /// </summary>
        /// <param name="gameObject">The gameobject to translate and rotate</param>
        /// <param name="destination">The destination to reach</param>
        /// <param name="speed">The translation velocity. Setting 0 will instantly translate to destination</param>
        public void Move(GameObject gameObject, Transform destination, float speed) {
            Move(gameObject, destination, null, speed, null);
        }

        private static float ConvertLinearToRadial(float linearVelocity, Transform source, Transform destination) {
            if(linearVelocity == 0) { return 0; }

            float dt = (destination.position - source.position).magnitude / linearVelocity;
            float angle = Quaternion.Angle(source.rotation, destination.rotation);

            return angle / dt;
        }

        private class MovingObject {
            private static Transform _instantTargetTransform;

            public Rigidbody rigidBody;
            public Transform transform;
            public Transform destination;
            public bool rotationEnabled = false;

            public float moveSpeed;
            public float rotateSpeed;

            public bool destinationReached;
            public bool destinationIsTemporary;

            public bool rigidBodyWasKinematic;

            public OnDestinationReached DestinationReached;

            public MovingObject(GameObject gameobject) {
                rigidBody = gameobject.GetComponent<Rigidbody>();
                rigidBodyWasKinematic = rigidBody != null ? rigidBody.isKinematic : false;
                transform = gameobject.transform;

                if(_instantTargetTransform == null) {
                    var go = new GameObject("instantTargetTransform");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _instantTargetTransform = go.transform;
                }
            }

            public void ChangeDestination(Transform destination, Transform offset) {
                if(offset == null) {
                    this.destination = destination;

                    destinationIsTemporary = false;
                    destinationReached = false;
                    return;
                }
                _instantTargetTransform.SetPositionAndRotation(transform.position, transform.rotation);
                _instantTargetTransform.SetParent(offset, true);

                var lastPosition = offset.position;
                var lastRotation = offset.rotation;

                offset.SetPositionAndRotation(destination.position, destination.rotation);

                this.destination = CreateTemporary(destination, _instantTargetTransform.position, _instantTargetTransform.rotation);

                _instantTargetTransform.SetParent(null);
                offset.position = lastPosition;
                offset.rotation = lastRotation;

                destinationReached = false;
                destinationIsTemporary = true;
            }

            public void ChangeDestination(Vector3 destination) {
                destinationIsTemporary = true;
                this.destination = CreateTemporary(null, destination, transform.rotation);
                destinationReached = false;
            }

            public void AnimateMovement(float dt) {
                if (rigidBody != null) {
                    rigidBody.isKinematic = true;
                    if (rotationEnabled) { PhysicsRotation(dt); }
                    PhysicsMovement(dt);
                }
                else {
                    if (rotationEnabled) { SimpleRotation(dt); }
                    SimpleMovement(dt);
                }
                
                if((transform.position - destination.position).sqrMagnitude < sqrConnectEpsilon 
                    && (!rotationEnabled || Quaternion.Angle(transform.rotation, destination.rotation) < connectRotationEpsilon)) {
                    // Destination Reached !!!
                    destinationReached = true;
                    Stop(true);
                }
            }

            public void Stop(bool notifyObservers) {
                if (rigidBody != null) {
                    rigidBody.isKinematic = rigidBodyWasKinematic;
                }
                if (destinationIsTemporary) { Destroy(destination.gameObject); }
                if (notifyObservers && DestinationReached != null) { DestinationReached(transform.gameObject, destination); }
            }

            private void SimpleRotation(float dt) {
                if(rotateSpeed == 0) {
                    transform.rotation = destination.rotation;
                }
                else {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, destination.rotation, rotateSpeed * dt);
                }
            }

            private void SimpleMovement(float dt) {
                if(moveSpeed == 0) {
                    transform.position = destination.position;
                }
                else {
                    transform.position = Vector3.MoveTowards(transform.position, destination.position, dt * moveSpeed);
                }
            }

            private void PhysicsRotation(float dt) {
                if (rotateSpeed == 0) {
                    rigidBody.MoveRotation(destination.rotation);
                }
                else {
                    rigidBody.MoveRotation(Quaternion.RotateTowards(transform.rotation, destination.rotation, rotateSpeed * dt));
                }
            }

            private void PhysicsMovement(float dt) {
                if(moveSpeed == 0) {
                    rigidBody.MovePosition(destination.position);
                }
                else {
                    rigidBody.MovePosition(Vector3.MoveTowards(transform.position, destination.position, dt * moveSpeed));
                }
            }

            private static Transform CreateTemporary(Transform parent, Vector3 position, Quaternion rotation) {
                var newGO = new GameObject("ObjectMover_Temp");
                newGO.hideFlags = HideFlags.HideAndDontSave;
                newGO.transform.SetParent(parent, false);
                newGO.transform.position = position;
                newGO.transform.rotation = rotation;

                return newGO.transform;
            }
        }
    }
}
