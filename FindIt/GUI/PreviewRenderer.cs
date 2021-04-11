// modified from SamsamTS's original Find It mod
// https://github.com/SamsamTS/CS-FindIt

using ColossalFramework;
using UnityEngine;

namespace FindIt.GUI
{
    public class PreviewRenderer : MonoBehaviour
    {
        private Camera renderCamera;
        private Mesh currentMesh;
        private float currentRotation = 35f;
        private float currentZoom = 4f;
        private Material _material;
        public Shader propFenceShader = Shader.Find("Custom/Props/Prop/Fence");
        private bool isPropFenceShader = false;

        /// <summary>
        /// Sets material to render.
        /// </summary>
        public Material Material { set => _material = value; }


        /// <summary>
        /// Initialise the new renderer object.
        /// </summary>
        public PreviewRenderer()
        {
            // Set up camera.
            renderCamera = new GameObject("Camera").AddComponent<Camera>();
            renderCamera.transform.SetParent(transform);
            renderCamera.targetTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            renderCamera.allowHDR = true;
            renderCamera.enabled = false;
            renderCamera.clearFlags = CameraClearFlags.Color;

            // Basic defaults.
            renderCamera.pixelRect = new Rect(0f, 0f, 512, 512);
            renderCamera.backgroundColor = new Color(0, 0, 0, 0);
            renderCamera.fieldOfView = 30f;
            renderCamera.nearClipPlane = 1f;
            renderCamera.farClipPlane = 1000f;
        }


        /// <summary>
        /// Image size.
        /// </summary>
        public Vector2 Size
        {
            get => new Vector2(renderCamera.targetTexture.width, renderCamera.targetTexture.height);

            set
            {
                if (Size != value)
                {
                    // New size; set camera output sizes accordingly.
                    renderCamera.targetTexture = new RenderTexture((int)value.x, (int)value.y, 24, RenderTextureFormat.ARGB32);
                    renderCamera.pixelRect = new Rect(0f, 0f, value.x, value.y);
                }
            }
        }


        /// <summary>
        /// Currently rendered mesh.
        /// </summary>
        public Mesh Mesh
        {
            get => currentMesh;

            set => currentMesh = value;
        }


        /// <summary>
        /// Current building texture.
        /// </summary>
        public RenderTexture Texture
        {
            get => renderCamera.targetTexture;
        }


        /// <summary>
        /// Preview camera rotation (degrees).
        /// </summary>
        public float CameraRotation
        {
            get { return currentRotation; }
            set { currentRotation = value % 360f; }
        }


        /// <summary>
        /// Zoom level.
        /// </summary>
        public float Zoom
        {
            get { return currentZoom; }
            set
            {
                currentZoom = Mathf.Clamp(value, 0.5f, 5f);
            }
        }

        /// <summary>
        /// props with prop fence shader need to be handled differently or they are not viewable
        /// </summary>
        public bool IsPropFenceShader
        {
            get { return isPropFenceShader; }
            set { isPropFenceShader = value; }
        }


        /// <summary>
        /// Render the current mesh.
        /// </summary>
        public void Render()
        {
            // If no primary mesh and no other meshes, don't do anything here.
            if (currentMesh == null)
            {
                return;
            }

            // Back up current game InfoManager mode.
            InfoManager infoManager = Singleton<InfoManager>.instance;
            InfoManager.InfoMode currentMode = infoManager.CurrentMode;
            InfoManager.SubInfoMode currentSubMode = infoManager.CurrentSubMode; ;

            // Set current game InfoManager to default (don't want to render with an overlay mode).
            infoManager.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            infoManager.UpdateInfoMode();

            // Backup current exposure and sky tint.
            float gameExposure = DayNightProperties.instance.m_Exposure;
            Color gameSkyTint = DayNightProperties.instance.m_SkyTint;

            // Backup current game lighting.
            Light gameMainLight = RenderManager.instance.MainLight;

            // Set exposure and sky tint for render.
            DayNightProperties.instance.m_Exposure = 0.75f;
            DayNightProperties.instance.m_SkyTint = new Color(0, 0, 0);
            DayNightProperties.instance.Refresh();

            // Set up our render lighting settings.
            Light renderLight = DayNightProperties.instance.sunLightSource;
            RenderManager.instance.MainLight = renderLight;

            // Set model position.
            // We render at -100 Y to avoid garbage left at 0,0 by certain shaders and renderers (and we only rotate around the Y axis so will never see the origin).
            Vector3 modelPosition = new Vector3(0f, -100f, 0f);

            // Reset the bounding box to be the smallest that can encapsulate all verticies of the new mesh.
            // That way the preview image is the largest size that fits cleanly inside the preview size.
            Bounds currentBounds = new Bounds(Vector3.zero, Vector3.zero);
            Vector3[] vertices;

            // Set our model rotation parameters, so we look at it obliquely.
            const float xRotation = 20f;

            // Apply model rotation with our camnera rotation into a quaternion.
            Quaternion modelRotation = Quaternion.Euler(xRotation, 0f, 0f) * Quaternion.Euler(0f, currentRotation, 0f);

            // props with prop fence shader need to be handled differently or they are not viewable
            if (IsPropFenceShader)
            {
                modelRotation = Quaternion.Euler(-60f, 180f, 0f) * Quaternion.Euler(0f, currentRotation, 0f);
                modelPosition = modelRotation * -currentMesh.bounds.center;
            }

            // Add our main mesh, if any (some are null, because they only 'appear' through subbuildings - e.g. Boston Residence Garage).
            if (currentMesh != null && _material != null)
            {
                // Calculate rendering matrix and add mesh to scene.
                Matrix4x4 matrix = Matrix4x4.TRS(modelPosition, modelRotation, Vector3.one);
                Graphics.DrawMesh(currentMesh, matrix, _material, 0, renderCamera, 0, null, true, true);

                if (currentMesh.isReadable)
                {
                    // Use separate verticies instance instead of accessing Mesh.vertices each time (which is slow).
                    // >10x measured performance improvement by doing things this way instead.
                    vertices = currentMesh.vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        // Exclude vertices with large negative Y values (underground) from our bounds (e.g. SoCal Laguna houses), otherwise the result doesn't look very good.
                        if (vertices[i].y > -2)
                        {
                            currentBounds.Encapsulate(vertices[i]);
                        }
                    }
                }
                // some vanilla assets have locked meshes. Their vertices are not readable
                else
                {
                    currentBounds = currentMesh.bounds;
                }
            }

            // Set zoom to encapsulate entire model.
            float magnitude = currentBounds.extents.magnitude;
            float clipExtent = (magnitude + 16f) * 1.5f;
            float clipCenter = magnitude * currentZoom;

            // Clip planes.
            renderCamera.nearClipPlane = Mathf.Max(clipCenter - clipExtent, 0.01f);
            renderCamera.farClipPlane = clipCenter + clipExtent;

            // Rotate our camera around the model according to our current rotation.
            renderCamera.transform.position = modelPosition + (Vector3.forward * clipCenter);

            // Aim camera at middle of bounds.
            renderCamera.transform.LookAt(currentBounds.center + modelPosition);

            // If game is currently in nighttime, enable sun and disable moon lighting.
            if (gameMainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = true;
                DayNightProperties.instance.moonLightSource.enabled = false;
            }

            // Light settings.
            renderLight.transform.eulerAngles = new Vector3(40, 210, 70);
            renderLight.intensity = 2f;
            renderLight.color = Color.white;

            // Render!
            renderCamera.RenderWithShader(_material.shader, "");

            // Restore game lighting.
            RenderManager.instance.MainLight = gameMainLight;

            // Reset to moon lighting if the game is currently in nighttime.
            if (gameMainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = false;
                DayNightProperties.instance.moonLightSource.enabled = true;
            }

            // Restore game exposure and sky tint.
            DayNightProperties.instance.m_Exposure = gameExposure;
            DayNightProperties.instance.m_SkyTint = gameSkyTint;
            DayNightProperties.instance.Refresh();

            // Restore game InfoManager mode.
            infoManager.SetCurrentMode(currentMode, currentSubMode);
            infoManager.UpdateInfoMode();
        }
    }
}