using System.Collections.Generic;
using System.Reflection;
using Exploration.Camera;
using NUnit.Framework;
using UnityEngine;

public class CameraFollowEditModeTests
{
    private const float FloatTolerance = 0.0001f;
    private static readonly BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<Object> cleanupTargets = new();

    [TearDown]
    public void TearDown()
    {
        for (int index = cleanupTargets.Count - 1; index >= 0; index--)
        {
            Object target = cleanupTargets[index];
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        cleanupTargets.Clear();
    }

    [Test]
    public void ClampToBounds_UsesCameraBoundsAsHardLimit_WhenOverrideIsLarger()
    {
        BoxCollider2D cameraBounds = CreateBoxCollider(CreateGameObject("CameraBounds"), new Vector2(20f, 20f));
        CameraFollow follow = CreateCameraFollow(cameraBounds);
        GameObject overrideObject = CreateGameObject("RoomBounds");
        BoxCollider2D overrideBounds = CreateBoxCollider(overrideObject, new Vector2(40f, 40f));
        follow.SetBoundsOverride(overrideBounds);

        Vector3 clampedPosition = InvokeClampToBounds(follow, new Vector3(10f, 0f, -10f));

        Assert.That(clampedPosition.x, Is.EqualTo(5f).Within(FloatTolerance));
        Assert.That(clampedPosition.y, Is.EqualTo(0f).Within(FloatTolerance));
    }

    [Test]
    public void ClampToBounds_ReducesOrthographicSize_WhenViewportIsWiderThanBounds()
    {
        BoxCollider2D cameraBounds = CreateBoxCollider(CreateGameObject("CameraBounds"), new Vector2(10f, 10f));
        CameraFollow follow = CreateCameraFollow(cameraBounds);
        Camera camera = follow.GetComponent<Camera>();
        camera.aspect = 2f;
        SetPrivateField(follow, "requestedOrthographicSize", 5f);

        Vector3 clampedPosition = InvokeClampToBounds(follow, new Vector3(0f, 0f, -10f));

        Assert.That(camera.orthographicSize, Is.EqualTo(2.5f).Within(FloatTolerance));
        Assert.That(clampedPosition.x, Is.EqualTo(0f).Within(FloatTolerance));
        Assert.That(clampedPosition.y, Is.EqualTo(0f).Within(FloatTolerance));
    }

    [Test]
    public void TryGetEffectiveBounds_IntersectsMapBoundsAndOverride()
    {
        BoxCollider2D cameraBounds = CreateBoxCollider(CreateGameObject("CameraBounds"), new Vector2(20f, 20f));
        CameraFollow follow = CreateCameraFollow(cameraBounds);

        GameObject overrideObject = CreateGameObject("RoomBounds");
        overrideObject.transform.position = new Vector3(4f, 0f, 0f);
        BoxCollider2D overrideBounds = CreateBoxCollider(overrideObject, new Vector2(8f, 8f));
        follow.SetBoundsOverride(overrideBounds);

        Bounds effectiveBounds = InvokeTryGetEffectiveBounds(follow);

        Assert.That(effectiveBounds.center.x, Is.EqualTo(4f).Within(FloatTolerance));
        Assert.That(effectiveBounds.center.y, Is.EqualTo(0f).Within(FloatTolerance));
        Assert.That(effectiveBounds.size.x, Is.EqualTo(8f).Within(FloatTolerance));
        Assert.That(effectiveBounds.size.y, Is.EqualTo(8f).Within(FloatTolerance));
    }

    [Test]
    public void TryGetEffectiveBounds_UsesMapBounds_WhenOverrideDoesNotIntersect()
    {
        BoxCollider2D cameraBounds = CreateBoxCollider(CreateGameObject("CameraBounds"), new Vector2(20f, 20f));
        CameraFollow follow = CreateCameraFollow(cameraBounds);
        GameObject overrideObject = CreateGameObject("RoomBounds");
        overrideObject.transform.position = new Vector3(40f, 0f, 0f);
        BoxCollider2D overrideBounds = CreateBoxCollider(overrideObject, new Vector2(8f, 8f));
        follow.SetBoundsOverride(overrideBounds);

        Bounds effectiveBounds = InvokeTryGetEffectiveBounds(follow);

        Assert.That(effectiveBounds.center.x, Is.EqualTo(0f).Within(FloatTolerance));
        Assert.That(effectiveBounds.center.y, Is.EqualTo(0f).Within(FloatTolerance));
        Assert.That(effectiveBounds.size.x, Is.EqualTo(20f).Within(FloatTolerance));
        Assert.That(effectiveBounds.size.y, Is.EqualTo(20f).Within(FloatTolerance));
    }

    [Test]
    public void TryGetEffectiveBounds_UsesOverride_WhenMapBoundsIsMissing()
    {
        CameraFollow follow = CreateCameraFollow(null);
        GameObject overrideObject = CreateGameObject("RoomBounds");
        overrideObject.transform.position = new Vector3(3f, -2f, 0f);
        BoxCollider2D overrideBounds = CreateBoxCollider(overrideObject, new Vector2(6f, 4f));
        follow.SetBoundsOverride(overrideBounds);

        Bounds effectiveBounds = InvokeTryGetEffectiveBounds(follow);

        Assert.That(effectiveBounds.center.x, Is.EqualTo(3f).Within(FloatTolerance));
        Assert.That(effectiveBounds.center.y, Is.EqualTo(-2f).Within(FloatTolerance));
        Assert.That(effectiveBounds.size.x, Is.EqualTo(6f).Within(FloatTolerance));
        Assert.That(effectiveBounds.size.y, Is.EqualTo(4f).Within(FloatTolerance));
    }

    private CameraFollow CreateCameraFollow(Collider2D mapBounds)
    {
        GameObject cameraObject = CreateGameObject("MainCamera");
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.aspect = 1f;

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        SetPrivateField(follow, "mapBounds", mapBounds);
        InvokePrivateMethod(follow, "Awake");
        return follow;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        cleanupTargets.Add(gameObject);
        return gameObject;
    }

    private static BoxCollider2D CreateBoxCollider(GameObject gameObject, Vector2 size)
    {
        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = size;
        return collider;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceFlags);
        Assert.That(field, Is.Not.Null, $"필드 {fieldName}를 찾을 수 없습니다.");
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstanceFlags);
        Assert.That(method, Is.Not.Null, $"메서드 {methodName}를 찾을 수 없습니다.");
        method.Invoke(target, null);
    }

    private static Vector3 InvokeClampToBounds(CameraFollow follow, Vector3 position)
    {
        MethodInfo method = typeof(CameraFollow).GetMethod("ClampToBounds", PrivateInstanceFlags);
        Assert.That(method, Is.Not.Null, "ClampToBounds를 찾을 수 없습니다.");
        return (Vector3)method.Invoke(follow, new object[] { position });
    }

    private static Bounds InvokeTryGetEffectiveBounds(CameraFollow follow)
    {
        MethodInfo method = typeof(CameraFollow).GetMethod("TryGetEffectiveBounds", PrivateInstanceFlags);
        Assert.That(method, Is.Not.Null, "TryGetEffectiveBounds를 찾을 수 없습니다.");

        object[] arguments = { new Bounds() };
        bool hasBounds = (bool)method.Invoke(follow, arguments);
        Assert.That(hasBounds, Is.True, "유효 bounds를 계산하지 못했습니다.");
        return (Bounds)arguments[0];
    }
}
