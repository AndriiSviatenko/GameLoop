using UnityEngine;

namespace Azen.GameLoop.Demo
{
    public static class DemoSceneBuilder
    {
        public static void Build()
        {
            EnsureCamera();
            EnsureFloor();
            EnsureLight();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(
                DemoConfig.Camera.PositionX,
                DemoConfig.Camera.PositionY,
                DemoConfig.Camera.PositionZ);
        }

        private static void EnsureFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, DemoConfig.Scene.FloorCenterY, 0f);
            floor.transform.localScale = new Vector3(
                DemoConfig.Scene.FloorWidth,
                DemoConfig.Scene.FloorHeight,
                DemoConfig.Scene.FloorDepth);
        }

        private static void EnsureLight()
        {
            if (Object.FindAnyObjectByType<Light>() != null) return;

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
