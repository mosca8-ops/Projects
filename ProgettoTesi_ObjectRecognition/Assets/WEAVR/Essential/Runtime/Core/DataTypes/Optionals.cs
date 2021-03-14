using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TXT.WEAVR.Common
{
    [Serializable]
    public abstract class Optional
    {
        public bool enabled;
    }

    [Serializable]
    public class Optional<T> : Optional
    {
        public T value;

        public Optional() { }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator T(Optional<T> optional)
        {
            return optional.value;
        }

        public override string ToString()
        {
            return enabled ? value?.ToString() : "none";
        }
    }

    [Serializable]
    public class OptionalObject<T> : Optional where T : UnityEngine.Object
    {
        [Draggable]
        public T value;

        public OptionalObject() { }

        public static implicit operator OptionalObject<T>(T value)
        {
            return new OptionalObject<T>()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator T(OptionalObject<T> optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalBool : Optional<bool>
    {
        public static implicit operator OptionalBool(bool value)
        {
            return new OptionalBool()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator bool(OptionalBool optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalBool(bool? value)
        {
            return new OptionalBool()
            {
                enabled = value.HasValue,
                value = value ?? false
            };
        }

        public static implicit operator bool?(OptionalBool optional)
        {
            return optional.enabled ? optional.value : (bool?)null;
        }
    }
    [Serializable]
    public class OptionalByte : Optional<byte>
    {
        public static implicit operator OptionalByte(byte value)
        {
            return new OptionalByte()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator byte(OptionalByte optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalSbyte : Optional<sbyte>
    {
        public static implicit operator OptionalSbyte(sbyte value)
        {
            return new OptionalSbyte()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator sbyte(OptionalSbyte optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalShort : Optional<short>
    {
        public static implicit operator OptionalShort(short value)
        {
            return new OptionalShort()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator short(OptionalShort optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalShort(short? value)
        {
            return new OptionalShort()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator short? (OptionalShort optional)
        {
            return optional.enabled ? optional.value : (short?)null;
        }
    }
    [Serializable]
    public class OptionalUshort : Optional<ushort>
    {
        public static implicit operator OptionalUshort(ushort value)
        {
            return new OptionalUshort()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator ushort(OptionalUshort optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalUshort(ushort? value)
        {
            return new OptionalUshort()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator ushort? (OptionalUshort optional)
        {
            return optional.enabled ? optional.value : (ushort?)null;
        }
    }
    [Serializable]
    public class OptionalInt : Optional<int>
    {
        public static implicit operator OptionalInt(int value)
        {
            return new OptionalInt()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator int(OptionalInt optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalInt(int? value)
        {
            return new OptionalInt()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator int? (OptionalInt optional)
        {
            return optional.enabled ? optional.value : (int?)null;
        }
    }
    [Serializable]
    public class OptionalUint : Optional<uint>
    {
        public static implicit operator OptionalUint(uint value)
        {
            return new OptionalUint()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator uint(OptionalUint optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalUint(uint? value)
        {
            return new OptionalUint()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator uint? (OptionalUint optional)
        {
            return optional.enabled ? optional.value : (uint?)null;
        }
    }
    [Serializable]
    public class OptionalLong : Optional<long>
    {
        public static implicit operator OptionalLong(long value)
        {
            return new OptionalLong()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator long(OptionalLong optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalLong(long? value)
        {
            return new OptionalLong()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator long? (OptionalLong optional)
        {
            return optional.enabled ? optional.value : (long?)null;
        }
    }
    [Serializable]
    public class OptionalUlong : Optional<ulong>
    {
        public static implicit operator OptionalUlong(ulong value)
        {
            return new OptionalUlong()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator ulong(OptionalUlong optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalUlong(ulong? value)
        {
            return new OptionalUlong()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator ulong? (OptionalUlong optional)
        {
            return optional.enabled ? optional.value : (ulong?)null;
        }
    }
    [Serializable]
    public class OptionalFloat : Optional<float>
    {
        public static implicit operator OptionalFloat(float value)
        {
            return new OptionalFloat()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator float(OptionalFloat optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalFloat(float? value)
        {
            return new OptionalFloat()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator float? (OptionalFloat optional)
        {
            return optional.enabled ? optional.value : (float?)null;
        }
    }
    [Serializable]
    public class OptionalDouble : Optional<double>
    {
        public static implicit operator OptionalDouble(double value)
        {
            return new OptionalDouble()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator double(OptionalDouble optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalDouble(double? value)
        {
            return new OptionalDouble()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator double? (OptionalDouble optional)
        {
            return optional.enabled ? optional.value : (double?)null;
        }
    }
    [Serializable]
    public class OptionalDecimal : Optional<decimal>
    {
        public static implicit operator OptionalDecimal(decimal value)
        {
            return new OptionalDecimal()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator decimal(OptionalDecimal optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalDecimal(decimal? value)
        {
            return new OptionalDecimal()
            {
                enabled = value.HasValue,
                value = value ?? 0
            };
        }

        public static implicit operator decimal? (OptionalDecimal optional)
        {
            return optional.enabled ? optional.value : (decimal?)null;
        }
    }
    [Serializable]
    public class OptionalObject : Optional<object>
    {
    }

    [Serializable]
    public class OptionalChar : Optional<char>
    {
        public static implicit operator OptionalChar(char value)
        {
            return new OptionalChar()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator char(OptionalChar optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalChar(char? value)
        {
            return new OptionalChar()
            {
                enabled = value.HasValue,
                value = value ?? (char)0
            };
        }

        public static implicit operator char? (OptionalChar optional)
        {
            return optional.enabled ? optional.value : (char?)null;
        }
    }
    [Serializable]
    public class OptionalString : Optional<string>
    {
        public static implicit operator OptionalString(string value)
        {
            return new OptionalString()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator string(OptionalString optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalBehaviour : OptionalObject<Behaviour>
    {
        public static implicit operator OptionalBehaviour(Behaviour value)
        {
            return new OptionalBehaviour()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Behaviour(OptionalBehaviour optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalCamera : OptionalObject<Camera>
    {
        public static implicit operator OptionalCamera(Camera value)
        {
            return new OptionalCamera()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Camera(OptionalCamera optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalBoundingSphere : Optional<BoundingSphere>
    {
        public static implicit operator OptionalBoundingSphere(BoundingSphere value)
        {
            return new OptionalBoundingSphere()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator BoundingSphere(OptionalBoundingSphere optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalGameObject : OptionalObject<GameObject>
    {
        public static implicit operator OptionalGameObject(GameObject value)
        {
            return new OptionalGameObject()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator GameObject(OptionalGameObject optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalGradient : Optional<Gradient>
    {
        public static implicit operator OptionalGradient(Gradient value)
        {
            return new OptionalGradient()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Gradient(OptionalGradient optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalAnimationCurve : Optional<AnimationCurve>
    {
        public static implicit operator OptionalAnimationCurve(AnimationCurve value)
        {
            return new OptionalAnimationCurve()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimationCurve(OptionalAnimationCurve optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalMaterial : OptionalObject<Material>
    {
        public static implicit operator OptionalMaterial(Material value)
        {
            return new OptionalMaterial()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Material(OptionalMaterial optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalSprite : OptionalObject<Sprite>
    {
        public static implicit operator OptionalSprite(Sprite value)
        {
            return new OptionalSprite()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Sprite(OptionalSprite optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalTexture : OptionalObject<Texture>
    {
        public static implicit operator OptionalTexture(Texture value)
        {
            return new OptionalTexture()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Texture(OptionalTexture optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalTexture2D : OptionalObject<Texture2D>
    {
        public static implicit operator OptionalTexture2D(Texture2D value)
        {
            return new OptionalTexture2D()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Texture2D(OptionalTexture2D optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalRenderTexture : OptionalObject<RenderTexture>
    {
        public static implicit operator OptionalRenderTexture(RenderTexture value)
        {
            return new OptionalRenderTexture()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator RenderTexture(OptionalRenderTexture optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalScene : Optional<Scene>
    {
        public static implicit operator OptionalScene(Scene value)
        {
            return new OptionalScene()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Scene(OptionalScene optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalTransform : OptionalObject<Transform>
    {
        public static implicit operator OptionalTransform(Transform value)
        {
            return new OptionalTransform()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Transform(OptionalTransform optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalRectTransform : OptionalObject<RectTransform>
    {
        public static implicit operator OptionalRectTransform(RectTransform value)
        {
            return new OptionalRectTransform()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator RectTransform(OptionalRectTransform optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalAxis : Optional<Axis>
    {
        public static implicit operator OptionalAxis(Axis value)
        {
            return new OptionalAxis()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Axis(OptionalAxis optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalPrimitiveType : Optional<PrimitiveType>
    {
        public static implicit operator OptionalPrimitiveType(PrimitiveType value)
        {
            return new OptionalPrimitiveType()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator PrimitiveType(OptionalPrimitiveType optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalBounds : Optional<Bounds>
    {
        public static implicit operator OptionalBounds(Bounds value)
        {
            return new OptionalBounds()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Bounds(OptionalBounds optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalColor : Optional<Color>
    {
        public static implicit operator OptionalColor(Color value)
        {
            return new OptionalColor()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Color(OptionalColor optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalColor32 : Optional<Color32>
    {
        public static implicit operator OptionalColor32(Color32 value)
        {
            return new OptionalColor32()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Color32(OptionalColor32 optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalComponent : OptionalObject<Component>
    {
        public static implicit operator OptionalComponent(Component value)
        {
            return new OptionalComponent()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Component(OptionalComponent optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalMesh : OptionalObject<Mesh>
    {
        public static implicit operator OptionalMesh(Mesh value)
        {
            return new OptionalMesh()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Mesh(OptionalMesh optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalRenderer : OptionalObject<Renderer>
    {
        public static implicit operator OptionalRenderer(Renderer value)
        {
            return new OptionalRenderer()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Renderer(OptionalRenderer optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalKeyCode : Optional<KeyCode>
    {
        public static implicit operator OptionalKeyCode(KeyCode value)
        {
            return new OptionalKeyCode()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator KeyCode(OptionalKeyCode optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalMatrix4x4 : Optional<Matrix4x4>
    {
        public static implicit operator OptionalMatrix4x4(Matrix4x4 value)
        {
            return new OptionalMatrix4x4()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Matrix4x4(OptionalMatrix4x4 optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalVector3 : Optional<Vector3>
    {
        public static implicit operator OptionalVector3(Vector3 value)
        {
            return new OptionalVector3()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Vector3(OptionalVector3 optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalVector3(Vector3? value)
        {
            return new OptionalVector3()
            {
                enabled = value.HasValue,
                value = value ?? Vector3.zero
            };
        }

        public static implicit operator Vector3? (OptionalVector3 optional)
        {
            return optional.enabled ? optional.value : (Vector3?)null;
        }
    }
    [Serializable]
    public class OptionalQuaternion : Optional<Quaternion>
    {
        public static implicit operator OptionalQuaternion(Quaternion value)
        {
            return new OptionalQuaternion()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Quaternion(OptionalQuaternion optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalQuaternion(Quaternion? value)
        {
            return new OptionalQuaternion()
            {
                enabled = value.HasValue,
                value = value ?? Quaternion.identity
            };
        }

        public static implicit operator Quaternion? (OptionalQuaternion optional)
        {
            return optional.enabled ? optional.value : (Quaternion?)null;
        }
    }
    [Serializable]
    public class OptionalPose : Optional<Pose>
    {
        public static implicit operator OptionalPose(Pose value)
        {
            return new OptionalPose()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Pose(OptionalPose optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalRay : Optional<Ray>
    {
        public static implicit operator OptionalRay(Ray value)
        {
            return new OptionalRay()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Ray(OptionalRay optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalRect : Optional<Rect>
    {
        public static implicit operator OptionalRect(Rect value)
        {
            return new OptionalRect()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Rect(OptionalRect optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalVector2 : Optional<Vector2>
    {
        public static implicit operator OptionalVector2(Vector2 value)
        {
            return new OptionalVector2()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Vector2(OptionalVector2 optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalVector2(Vector2? value)
        {
            return new OptionalVector2()
            {
                enabled = value.HasValue,
                value = value ?? Vector2.zero
            };
        }

        public static implicit operator Vector2? (OptionalVector2 optional)
        {
            return optional.enabled ? optional.value : (Vector2?)null;
        }
    }
    [Serializable]
    public class OptionalVector2Int : Optional<Vector2Int>
    {
        public static implicit operator OptionalVector2Int(Vector2Int value)
        {
            return new OptionalVector2Int()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Vector2Int(OptionalVector2Int optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalVector3Int : Optional<Vector3Int>
    {
        public static implicit operator OptionalVector3Int(Vector3Int value)
        {
            return new OptionalVector3Int()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Vector3Int(OptionalVector3Int optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalVector4 : Optional<Vector4>
    {
        public static implicit operator OptionalVector4(Vector4 value)
        {
            return new OptionalVector4()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Vector4(OptionalVector4 optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalVector4(Vector4? value)
        {
            return new OptionalVector4()
            {
                enabled = value.HasValue,
                value = value ?? Vector4.zero
            };
        }

        public static implicit operator Vector4? (OptionalVector4 optional)
        {
            return optional.enabled ? optional.value : (Vector4?)null;
        }
    }
    [Serializable]
    public class OptionalRigidbody : OptionalObject<Rigidbody>
    {
        public static implicit operator OptionalRigidbody(Rigidbody value)
        {
            return new OptionalRigidbody()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Rigidbody(OptionalRigidbody optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalJoint : OptionalObject<Joint>
    {
        public static implicit operator OptionalJoint(Joint value)
        {
            return new OptionalJoint()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Joint(OptionalJoint optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalCollider : OptionalObject<Collider>
    {
        public static implicit operator OptionalCollider(Collider value)
        {
            return new OptionalCollider()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Collider(OptionalCollider optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalBoxCollider : OptionalObject<BoxCollider>
    {
        public static implicit operator OptionalBoxCollider(BoxCollider value)
        {
            return new OptionalBoxCollider()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator BoxCollider(OptionalBoxCollider optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalSphereCollider : OptionalObject<SphereCollider>
    {
        public static implicit operator OptionalSphereCollider(SphereCollider value)
        {
            return new OptionalSphereCollider()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator SphereCollider(OptionalSphereCollider optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalMeshCollider : OptionalObject<MeshCollider>
    {
        public static implicit operator OptionalMeshCollider(MeshCollider value)
        {
            return new OptionalMeshCollider()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator MeshCollider(OptionalMeshCollider optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalCapsuleCollider : OptionalObject<CapsuleCollider>
    {
        public static implicit operator OptionalCapsuleCollider(CapsuleCollider value)
        {
            return new OptionalCapsuleCollider()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator CapsuleCollider(OptionalCapsuleCollider optional)
        {
            return optional.value;
        }
    }
    [Serializable]
    public class OptionalRaycastHit : Optional<RaycastHit>
    {
        public static implicit operator OptionalRaycastHit(RaycastHit value)
        {
            return new OptionalRaycastHit()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator RaycastHit(OptionalRaycastHit optional)
        {
            return optional.value;
        }
    }

}