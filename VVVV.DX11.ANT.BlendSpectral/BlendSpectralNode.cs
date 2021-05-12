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
    [PluginInfo (Name = "BlendSpectral", Author = "vux / antokhio", Category ="DX11", Version ="Texture")]
    public class BlendSpectralNode : IPluginEvaluate, IDX11ResourceHost, IPartImportsSatisfiedNotification
    {
        protected enum blendModes
        {
            Normal,
            Screen,
            Multiply,
            Add,
            Subtract,
            Darken,
            Lighten,
            Difference,
            Exclusion,
            Overlay,
            Hardlight,
            Softlight,
            Dodge,
            Burn,
            Reflect,
            Glow,
            Freeze,
            Heat,
            Divide
        }


        [Input("Size", DefaultValues = new double[] { 640, 480 }, AsInt = true)]
        protected ISpread<Vector2> size;

        [Input("Texture In")]
        protected Pin<DX11Resource<DX11Texture2D>> texture;

        [Input("Alpha", MinValue = 0.0f, MaxValue = 1.0f)]
        protected ISpread<float> alpha;

        [Input("Mode")]
        protected ISpread<blendModes> mode;

        [Output("Texture Output")]
        ISpread<DX11Resource<DX11Texture2D>> output;

        private int spreadMax;

        private DX11Effect effect;
        private DX11ShaderInstance instance;

        private DX11ResourcePoolEntry<DX11RenderTarget2D> resourceEntry;

        public void OnImportsSatisfied()
        {
            effect = DX11Effect.FromResource(System.Reflection.Assembly.GetExecutingAssembly(), "VVVV.DX11.ANT.BlendSpectral.Shaders.Blender.fx");
        }

        public void Evaluate(int SpreadMax)
        {
            spreadMax = SpreadMax;

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

            if (!texture.IsConnected)
            {
                output[0][context] = context.DefaultTextures.WhiteTexture;
                return;
            }

            if (texture.SliceCount == 1)
            {
                output[0][context] = texture[0][context];
                return;
            }

            if (instance == null)
            {
                instance = new DX11ShaderInstance(context, effect);
            }

            DX11ResourcePoolEntry<DX11RenderTarget2D> resourceRead = context.ResourcePool.LockRenderTarget((int)size[0].X, (int)size[0].Y, SlimDX.DXGI.Format.R8G8B8A8_UNorm);
            DX11ResourcePoolEntry<DX11RenderTarget2D> resourceWrite = context.ResourcePool.LockRenderTarget((int)size[0].X, (int)size[0].Y, SlimDX.DXGI.Format.R8G8B8A8_UNorm);

            bool first = true;

            // APPLY TRIANGLE SHADER
            context.Primitives.ApplyFullTriVS();

            for (int i = 0; i < spreadMax - 1; i++)
            {
                context.RenderTargetStack.Push(resourceWrite.Element);

                if (!first)
                {
                    instance.SetBySemantic("INPUTTEXTURE", resourceRead.Element.SRV);
                }
                else
                {
                    instance.SetBySemantic("INPUTTEXTURE", texture[0][context].SRV);
                }
            
                instance.SetBySemantic("SECONDTEXTURE", texture[i + 1][context].SRV);
                instance.SelectTechnique(mode[i].ToString());
                instance.SetByName("Opacity", alpha[i]);

                // APPLY
                instance.ApplyPass(0);

                // DRAW
                context.Primitives.FullScreenTriangle.Draw();

                context.RenderTargetStack.Pop();
                first = false;

                var tmp = resourceWrite;
                resourceWrite = resourceRead;
                resourceRead = tmp;
            }

            resourceWrite.UnLock();

            resourceEntry = resourceRead;

            output[0][context] = resourceRead.Element;
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
         
        }

  
    }
}
