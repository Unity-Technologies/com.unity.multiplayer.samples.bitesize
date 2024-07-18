using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

[ExecuteAlways]
public class _3DSkybox : MonoBehaviour
{
    private _3DSkyboxPass pass;

    private DrawObjectsPass drawPass;
    
    public LayerMask mask;

    private FilteringSettings filterOpaqueSettings = FilteringSettings.defaultValue;
    private FilteringSettings filterTransparentSettings = FilteringSettings.defaultValue;
    
    private RenderStateBlock renderStateBlock;

    public int stencilRef;
    
    private StencilState stencilState;

    public Material depthClearMat;

    public float scale = 64;
    
    private void OnEnable()
    {
        pass ??= new _3DSkyboxPass();

        // injection point
        pass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        // setup 3D skybox stuff
        filterOpaqueSettings = new FilteringSettings(RenderQueueRange.opaque, mask.value);
        filterTransparentSettings = new FilteringSettings(RenderQueueRange.transparent, mask.value);
        renderStateBlock = new RenderStateBlock(RenderStateMask.Stencil);
        
        stencilState = StencilState.defaultValue;
        stencilState.enabled = true;
        stencilState.SetCompareFunction(CompareFunction.LessEqual);
        
        renderStateBlock.stencilReference = stencilRef;
        renderStateBlock.stencilState = stencilState;
        
        // setup callback
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
    }

    private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        //Avoid rendering while in terminal
        //if (SceneTransitionManager.IsAvailable() && SceneTransitionManager.IsInTerminal() && cam.CompareTag("MainCamera"))
        //{
        //    return;
        //}
        
        if (pass == null) return;

        if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;
        
        pass.filterOpaqueSettings = filterOpaqueSettings;
        pass.filterTransparentSettings = filterTransparentSettings;
        pass.renderStateBlock = renderStateBlock;

        pass.depthClearMat = depthClearMat;

        // Do transform
        TransformSkybox(cam);
        
        // inject pass
        cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(pass);
    }
    
    private void EndCamera(ScriptableRenderContext arg1, Camera arg2)
    {
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
    }

    private void TransformSkybox(Camera cam)
    {
        if (cam.cameraType == CameraType.SceneView) return;
        
        var offset = cam.transform.position * (1 - (1/scale));

        transform.position = offset;
        transform.localScale = Vector3.one * (1/scale); 
    }

    private class _3DSkyboxPass : ScriptableRenderPass
    {
        public FilteringSettings filterOpaqueSettings;
        public FilteringSettings filterTransparentSettings;
        
        public RenderStateBlock renderStateBlock;

        public Material depthClearMat;

        List<ShaderTagId> shaderTags = new List<ShaderTagId>
        {
            new("SRPDefaultUnlit"), new("UniversalForward"), new("UniversalForwardOnly")
        };
        
        public _3DSkyboxPass()
        {
            profilingSampler = new ProfilingSampler(nameof(_3DSkyboxPass));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawSkyboxObjects(ref renderingData, context);

            // Clear the depth values
            var cmd = CommandBufferPool.Get("3D Skybox");
            
            if (depthClearMat != null)
            {
                CoreUtils.DrawFullScreen(cmd, depthClearMat);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void DrawSkyboxObjects(ref RenderingData renderingData, ScriptableRenderContext context)
        {
            var drawSettings =
                RenderingUtils.CreateDrawingSettings(shaderTags, ref renderingData, SortingCriteria.CommonOpaque);            
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterOpaqueSettings,
                ref renderStateBlock);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterTransparentSettings,
                ref renderStateBlock);
        }
    }

    private Camera cam;

    private void OnDrawGizmosSelected()
    {
        if (cam == null) cam = Camera.main;

        if (cam == null) return;

        Gizmos.matrix = cam.transform.localToWorldMatrix;

        var c = Color.red;
        c.a = 0.2f;
        Gizmos.color = c;
        Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane * (1f/scale), cam.nearClipPlane * (1f/scale), cam.aspect);
    }
}
