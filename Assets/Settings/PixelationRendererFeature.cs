using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelationRendererFeature : ScriptableRendererFeature {
    [System.Serializable]
    public class Settings {
        [Range(0, 1)]
        public float scale;
        public LayerMask pixelatedLayer;
        public RenderPassEvent renderPassEvent;
        public Material mergeMaterial;
    }

    class DefaultRenderPass : ScriptableRenderPass {
        private readonly string _profileName;
        private readonly float _scale;
        private FilteringSettings _filteringSettings;
        private readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId> {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit")
        };
        private readonly int _colorId, _depthId, _tmpId;
        private ScriptableRenderer _renderer;

        public DefaultRenderPass(string profileName, LayerMask layerMask, float scale) {
            _profileName = profileName;
            _scale = scale;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
            _colorId = Shader.PropertyToID(profileName + "Color");
            _depthId = Shader.PropertyToID(profileName + "Depth");
            _tmpId = Shader.PropertyToID(profileName + "Tmp");
        }

        public void Setup(ScriptableRenderer renderer) {
            _renderer = renderer;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            CameraControll cc = renderingData.cameraData.camera.gameObject.GetComponent<CameraControll>();
            cc.scale = _scale;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            base.Configure(cmd, cameraTextureDescriptor);
            cameraTextureDescriptor.width = (int)(cameraTextureDescriptor.width * _scale);
            cameraTextureDescriptor.height = (int)(cameraTextureDescriptor.height * _scale);
            cameraTextureDescriptor.msaaSamples = 1;

            cameraTextureDescriptor.colorFormat = RenderTextureFormat.ARGB64;
            cmd.GetTemporaryRT(_colorId, cameraTextureDescriptor, FilterMode.Point);
            cameraTextureDescriptor.depthBufferBits = 16;
            cameraTextureDescriptor.colorFormat = RenderTextureFormat.Depth;
            cmd.GetTemporaryRT(_tmpId, cameraTextureDescriptor, FilterMode.Point);
            cameraTextureDescriptor.colorFormat = RenderTextureFormat.R16;
            cmd.GetTemporaryRT(_depthId, cameraTextureDescriptor, FilterMode.Point);

            ConfigureTarget(_colorId, _tmpId);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIds, ref renderingData, sortingCriteria);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

            CommandBuffer cmd = CommandBufferPool.Get(_profileName);
            cmd.Blit(_tmpId, _depthId);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            cmd.ReleaseTemporaryRT(_tmpId);
        }
    }

    class FinalRenderPass : ScriptableRenderPass {
        private readonly string _profileName;
        private readonly Material _mergeMaterial;
        private ScriptableRenderer _renderer;
        private readonly int _tmpId = Shader.PropertyToID("_tmp");
        private readonly List<int> _temporaryRTs = new List<int> {
            Shader.PropertyToID("_tmp"),
            Shader.PropertyToID("_CommonColor"),
            Shader.PropertyToID("_CommonDepth"),
            Shader.PropertyToID("_PixelatedColor"),
            Shader.PropertyToID("_PixelatedDepth")
        };

        public FinalRenderPass(string profileName, Material mergeMaterial) {
            _profileName = profileName;
            _mergeMaterial = mergeMaterial;
        }

        public void Setup(ScriptableRenderer renderer) {
            _renderer = renderer;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            base.Configure(cmd, cameraTextureDescriptor);
            cmd.GetTemporaryRT(_tmpId, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get(_profileName);
            cmd.Blit(_tmpId, _renderer.cameraColorTarget, _mergeMaterial, 0);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            foreach (int rt in _temporaryRTs)
                cmd.ReleaseTemporaryRT(rt);
        }
    }

    public Settings settings;
    private DefaultRenderPass _commonRenderPass, _pixelatedRenderPass;
    private FinalRenderPass _finalRenderPass;

    public override void Create() {
        _commonRenderPass = new DefaultRenderPass("_Common", ~settings.pixelatedLayer, 1);
        _commonRenderPass.renderPassEvent = settings.renderPassEvent;
        _pixelatedRenderPass = new DefaultRenderPass("_Pixelated", settings.pixelatedLayer, settings.scale);
        _pixelatedRenderPass.renderPassEvent = settings.renderPassEvent;
        _finalRenderPass = new FinalRenderPass("Final", settings.mergeMaterial);
        _finalRenderPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        _commonRenderPass.Setup(renderer);
        _pixelatedRenderPass.Setup(renderer);
        _finalRenderPass.Setup(renderer);
        renderer.EnqueuePass(_commonRenderPass);
        renderer.EnqueuePass(_pixelatedRenderPass);
        renderer.EnqueuePass(_finalRenderPass);
    }
}


