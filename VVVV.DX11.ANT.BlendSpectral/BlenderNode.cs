using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;
using FeralTic;
using FeralTic.DX11.Resources;
using FeralTic.DX11;
using SlimDX;

namespace VVVV.DX11.ANT.BlendSpectral
{
    [PluginInfo (Name = "Blender", Author = "vux / antokhio", Category ="DX11", Version ="Texture")]
    public class BlenderNode : IPluginEvaluate, IDX11ResourceHost, IPartImportsSatisfiedNotification
    {
        [Input("Size", DefaultValues = new double[] { 640, 480 }, AsInt = true)]
        protected ISpread<Vector2> size;

        [Input("Texture Input")]
        protected Pin<DX11Resource<DX11Texture2D>> tex0;

        [Input("Texture Input 2")]
        protected Pin<DX11Resource<DX11Texture2D>> tex1;

        [Input("Alpha", MinValue = 0.0f, MaxValue = 1.0f)]
        protected ISpread<float> alpha;

        [Output("Texture Output")]
        ISpread<DX11Resource<DX11Texture2D>> output;

        private DX11Effect effect;
        private DX11ShaderInstance instance;

        private DX11ResourcePoolEntry<DX11RenderTarget2D> resourceEntry;

        public void OnImportsSatisfied()
        {
            effect = DX11Effect.FromResource(System.Reflection.Assembly.GetExecutingAssembly(), "VVVV.DX11.ANT.BlendSpectral.Shaders.Blender.fx");
        }

        public void Evaluate(int SpreadMax)
        {
            if (this.output[0] == null)
                output[0] = new DX11Resource<DX11Texture2D>();
        }

        public void Update(DX11RenderContext context)
        {
            if (resourceEntry != null)
            {
                resourceEntry.UnLock();
                resourceEntry = null;
            }

            if (!tex0.IsConnected && !tex1.IsConnected)
            {
                output[0][context] = context.DefaultTextures.WhiteTexture;
                return;
            }

            if (tex0.IsConnected && !tex1.IsConnected)
            {
                output[0][context] = tex0[0][context];
                return;
            }

            if (!tex0.IsConnected && tex1.IsConnected)
            {
                output[0][context] = tex1[0][context];
                return;
            }

            if (instance == null)
            {
                instance = new DX11ShaderInstance(context, effect);
            }

            resourceEntry = context.ResourcePool.LockRenderTarget((int)size[0].X, (int)size[0].Y, SlimDX.DXGI.Format.R8G8B8A8_UInt);

            // APPLY VIEWPORT
            context.RenderTargetStack.Push(resourceEntry.Element);
            context.Primitives.ApplyFullTriVS();

            // APPLY PARAMS
            instance.SetBySemantic("INPUTTEXTURE", tex0[0][context].SRV);
            instance.SetBySemantic("SECONDTEXTURE", tex1[0][context].SRV);
            instance.SetBySemantic("TARGETSIZE", size[0]);
            instance.SetByName("Opacity", alpha[0]);

            instance.ApplyPass(0);

            // DRAW
            context.CurrentDeviceContext.Draw(3, 9);
            //context.Primitives.FullScreenTriangle.Draw();

            context.RenderTargetStack.Pop();

            
            output[0][context] = resourceEntry.Element;
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
         
        }

  
    }
}
