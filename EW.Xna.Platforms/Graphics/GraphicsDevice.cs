using System;
using System.Collections;
using System.Collections.Generic;

using OpenTK.Graphics.ES20;
namespace EW.Xna.Platforms.Graphics
{

    public enum ClearOptions
    {
        /// <summary>
        /// Color buffer
        /// </summary>
        Target = 1,
        /// <summary>
        /// Depth buffer
        /// </summary>
        DepthBuffer = 2,
        /// <summary>
        /// Stencil buffer
        /// </summary>
        Stencil = 4
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class GraphicsDevice:IDisposable
    {

        private bool _isDisposed;
        /// <summary>
        /// ��Ⱦ��Ϣ(���Ժ����)
        /// </summary>
        internal GraphicsMetrics _graphicsMetrics;

        public GraphicsMetrics Metrics { get { return _graphicsMetrics; } set { _graphicsMetrics = value; } }

        private IndexBuffer _indexBuffer;

        private bool _indexBufferDirty;

        private VertexBufferBindings _vertexBuffers;
        private bool _vertexBuffersDirty;
        private int _maxVertexBufferSlots;

        private readonly ConstantBufferCollection _vertexConstantBuffers = new ConstantBufferCollection(ShaderStage.Vertex, 16);
        private readonly ConstantBufferCollection _pixelConstantBuffers = new ConstantBufferCollection(ShaderStage.Pixel, 16);

        public TextureCollection Textures { get; private set; }


        private BlendState _blendStateAdditive;
        private BlendState _blendStateAlphaBlend;
        private BlendState _blendStateNonPremultiplied;
        private BlendState _blendStateOpaque;

        private BlendState _blendState;
        private BlendState _actualBlendState;

        private DepthStencilState _depthStencilState;
        private DepthStencilState _actualDepthStencilState;
        private bool _depthStencilStateDirty;
        
        private DepthStencilState _depthStencilStateDefault;
        private DepthStencilState _depthStencilStateDepthRead;
        private DepthStencilState _depthStencilStateNone;


        private RasterizerState _rasterizerState;
        private RasterizerState _actualRasterizerState;
        private bool _rasterizerStateDirty;

        private RasterizerState _rasterizerStateCullClockwise;
        private RasterizerState _rasterizerStateCullCounterClockwise;
        private RasterizerState _rasterizerStateCullNone;


        private Color _blendFactor = Color.White;
        private bool _blendFactorDirty;

        private int _currentRenderTargetCount;  //��ǰ������ȾĿ������
        private readonly RenderTargetBinding[] _currentRenderTargetBindings = new RenderTargetBinding[4];
        internal bool IsRenderTargetBound
        {
            get { return _currentRenderTargetCount > 0; }
        }

        private Rectangle _scissorRectangle;//�ü����η�Χ
        private bool _scissorRectangleDirty;


        public Rectangle ScissorRectangle
        {
            get { return _scissorRectangle; }
            set
            {
                if (_scissorRectangle == value)
                    return;
                _scissorRectangle = value;
                _scissorRectangleDirty = true;
            }
        }
        
        /// <summary>
        /// ���&ģ��״̬
        /// </summary>
        public DepthStencilState DepthStencilState
        {
            get { return _depthStencilState; }
            set
            {
                if (_depthStencilState == value)
                    return;
                _depthStencilState = value;

                var newDepthStencilState = _depthStencilState;
                if (ReferenceEquals(_depthStencilState, DepthStencilState.Default))
                    newDepthStencilState = _depthStencilStateDefault;
                else if (ReferenceEquals(_depthStencilState, DepthStencilState.DepthRead))
                    newDepthStencilState = _depthStencilStateDepthRead;
                else if (ReferenceEquals(_depthStencilState, DepthStencilState.None))
                    newDepthStencilState = _depthStencilStateNone;

                newDepthStencilState.BindToGraphicsDevice(this);

                _actualDepthStencilState = newDepthStencilState;
                _depthStencilStateDirty = true;
            }
        }

        public RasterizerState RasterizerState
        {
            get { return _rasterizerState; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (_rasterizerState == value)
                    return;

                var newRasterizerState = _rasterizerState;
                if (ReferenceEquals(_rasterizerState, RasterizerState.CullClockwise))
                    newRasterizerState = _rasterizerStateCullClockwise;
                else if (ReferenceEquals(_rasterizerState, RasterizerState.CullCounterClockwise))
                    newRasterizerState = _rasterizerStateCullCounterClockwise;
                else if (ReferenceEquals(_rasterizerState, RasterizerState.CullNone))
                    newRasterizerState = _rasterizerStateCullNone;

                newRasterizerState.BindToGraphicsDevice(this);
                _actualRasterizerState = newRasterizerState;

                _rasterizerStateDirty = true;
            }
        }


        public event EventHandler<EventArgs> DeviceLost;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        /// <summary>
        /// ͼ����Դ������Ƴ����������ڶ���̵߳Ĳ�������ʱ��Ҫlock
        /// </summary>
        private readonly object _resourceLock = new object();

        private readonly List<WeakReference> _resources = new List<WeakReference>();

        private readonly GraphicsProfile _graphicsProfile;

        internal int MaxTextureSlots;
        internal int MaxVertexTextureSlots;

        public GraphicsProfile GraphicsProfile
        {
            get { return _graphicsProfile; }
        }



        internal GraphicsCapabilities GraphicsCapabilities { get; private set; }
        /// <summary>
        /// ������ɫ��
        /// </summary>
        private Shader _vertexShader;

        internal Shader VertexShader
        {
            get { return _vertexShader; }
            set
            {
                if (_vertexShader == value)
                    return;
                _vertexShader = value;
                _vertexConstantBuffers.Clear();
                _vertexShaderDirty = true;
            }
        }

        /// <summary>
        /// ��ʶ����shader�Ƿ��ѷ���
        /// </summary>
        private bool _vertexShaderDirty;
        private bool VertexShaderDirty
        {
            get { return _vertexShaderDirty; }
        }

        /// <summary>
        /// Ƭ����ɫ��
        /// </summary>
        private Shader _pixelShader;

        internal Shader PixelShader
        {
            get { return _pixelShader; }
            set
            {
                if (_pixelShader == value)
                    return;
                _pixelShader = value;
                _pixelConstantBuffers.Clear();
                _pixelShaderDirty = true;
            }
        }

        /// <summary>
        /// ��ʶ����shader �Ƿ��ѷ���
        /// </summary>
        private bool _pixelShaderDirty;
        private bool PixelShaderDirty
        {
            get { return _pixelShaderDirty; }
        }

        private Viewport _viewport;

        public Viewport Viewport
        {
            get
            {
                return _viewport;
            }
            set
            {
                _viewport = value;
                PlatformSetViewport(ref value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public PresentationParameters PresentationParameters
        {
            get;private set;
        }

        internal GraphicsDevice()
        {
            PresentationParameters = new PresentationParameters();
            PresentationParameters.DepthStencilFormat = DepthFormat.Depth24;
            Setup();
            GraphicsCapabilities = new GraphicsCapabilities(this);
            GraphicsCapabilities.Initialize(this);
            Initialize();
        }

        ~GraphicsDevice()
        {
            Dispose(false);
        }

        /// <summary>
        /// ��ʼ��
        /// </summary>
        internal void Initialize()
        {
            PlatformInitialize();

            _blendStateDirty = _depthStencilStateDirty = _rasterizerStateDirty = true;
            BlendState = BlendState.Opaque;
            DepthStencilState = DepthStencilState.Default;
            RasterizerState = RasterizerState.CullCounterClockwise;



            _vertexBuffers = new VertexBufferBindings(_maxVertexBufferSlots);
            _vertexBuffersDirty = true;
            _indexBufferDirty = true;
            _vertexShaderDirty = true;
            _pixelShaderDirty = true;

            //����Ĭ�ϵĲü�����
            _scissorRectangleDirty = true;
            ScissorRectangle = _viewport.Bounds;

            //
            ApplyRenderTargets(null);
        }

        public GraphicsDevice(GraphicsAdapter adapter, GraphicsProfile graphicsProfile, PresentationParameters presentationParameters)
        {
            if (adapter == null)
                throw new ArgumentNullException("adapter");
            if (!adapter.IsProfileSupported(graphicsProfile))
                throw new NoSuitableGraphicsDeviceException(string.Format("Adapter '{0}' does not support the {1} profile.", adapter.Description, graphicsProfile));
            if (presentationParameters == null)
                throw new ArgumentNullException("presentationParameters");

            Adapter = adapter;
            PresentationParameters = presentationParameters;
            _graphicsProfile = graphicsProfile;
            Setup();
            GraphicsCapabilities = new GraphicsCapabilities(this);
            GraphicsCapabilities.Initialize(this);
            Initialize();
        }

        

        /// <summary>
        /// 
        /// </summary>
        private void Setup()
        {

            if(DisplayMode == null)
            {
                throw new Exception("");
            }
            _viewport = new Viewport(0, 0, DisplayMode.Width, DisplayMode.Height);

            _viewport.MaxDepth = 1.0f;

            PlatformSetup();

            Textures = new TextureCollection(this, MaxTextureSlots, false);

            _blendStateAdditive = BlendState.Additive.Clone();
            _blendStateAlphaBlend = BlendState.AlphaBlend.Clone();
            _blendStateNonPremultiplied = BlendState.NonPremultiplied.Clone();
            _blendStateOpaque = BlendState.Opaque.Clone();
            BlendState = BlendState.Opaque;

        }

       

        
        /// <summary>
        /// ��������Ļ��
        /// </summary>
        public void Present()
        {
            if (_currentRenderTargetCount != 0)
                throw new InvalidOperationException("Cannot call Present when a render target is active");

            _graphicsMetrics = new GraphicsMetrics();
            PlatformPresent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        public void Clear(Color color)
        {
            var options = ClearOptions.Target;
            options |= ClearOptions.DepthBuffer;
            options |= ClearOptions.Stencil;
            PlatformClear(options, color.ToVector4(), _viewport.MaxDepth, 0);

            unchecked
            {
                _graphicsMetrics._clearCount++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal void OnDeviceResetting()
        {
            if (DeviceResetting != null)
                DeviceResetting(this, EventArgs.Empty);
            lock (_resourceLock)
            {
                foreach(var resource in _resources)
                {
                    var target = resource.Target as GraphicsResource;
                    if (target != null)
                        target.GraphicsDeviceResetting();
                }
                _resources.RemoveAll(wr => !wr.IsAlive);
            }

        }

        internal void AddResourceReference(WeakReference resourceReference)
        {
            lock (_resourceLock)
                _resources.Add(resourceReference);
        }

        internal void RemoveResourceReference(WeakReference resourceReference)
        {
            lock (_resourceLock)
                _resources.Remove(resourceReference);
        }

        /// <summary>
        /// 
        /// </summary>
        internal void OnDeviceReset()
        {
            if (DeviceReset != null)
                DeviceReset(this, EventArgs.Empty);
        }

        public GraphicsAdapter Adapter
        {
            get;private set;
        }
        public DisplayMode DisplayMode
        {
            get { return Adapter.CurrentDisplayMode; }
        }

        public Color BlendFactor
        {
            get { return _blendFactor; }
            set
            {
                if (_blendFactor == value)
                    return;
                _blendFactor = value;
                _blendFactorDirty = true;
            }
        }

        public BlendState BlendState
        {
            get { return _blendState; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("BleneState Null");

                if (_blendState == value)
                    return;

                _blendState = value;

                var newBlendState = _blendState;
                if (ReferenceEquals(_blendState, BlendState.Additive))
                    newBlendState = _blendStateAdditive;
                else if (ReferenceEquals(_blendState, BlendState.AlphaBlend))
                    newBlendState = _blendStateAlphaBlend;
                else if (ReferenceEquals(_blendState, BlendState.NonPremultiplied))
                    newBlendState = _blendStateNonPremultiplied;
                else if (ReferenceEquals(_blendState, BlendState.Opaque))
                    newBlendState = _blendStateOpaque;

                newBlendState.BindToGraphicsDevice(this);
                _actualBlendState = newBlendState;
                BlendFactor = _actualBlendState.BlendFactor;
                _blendStateDirty = true;
            }
        }





        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primitiveT"></param>
        /// <param name="vertexData"></param>
        /// <param name="vertexOffset"></param>
        /// <param name="numVertices"></param>
        /// <param name="indexData"></param>
        /// <param name="indexOffset"></param>
        /// <param name="primitiveCount"></param>
        /// <param name="vertexDeclaration"></param>
        public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveT,T[] vertexData,int vertexOffset,
            int numVertices,short[] indexData,int indexOffset,int primitiveCount,VertexDeclaration vertexDeclaration) where T:struct
        {
            if (vertexData == null || vertexData.Length == 0)
                throw new ArgumentNullException("vertexData");
            if (vertexOffset < 0 || vertexOffset >= vertexData.Length)
                throw new ArgumentNullException("vertexOffset");
            if (numVertices <= 0 || numVertices > vertexData.Length)
                throw new ArgumentNullException("numVertices");
            if (vertexOffset + numVertices > vertexData.Length)
                throw new ArgumentOutOfRangeException("numVertices & vertexOffset out of index");
            if (indexData == null || indexData.Length == 0)
                throw new ArgumentNullException("indexData");
            if (indexOffset < 0 || indexOffset >= indexData.Length)
                throw new ArgumentOutOfRangeException("indexOffset");
            if (primitiveCount <= 0)
                throw new ArgumentOutOfRangeException("primitiveCount");
            if (indexOffset + GetElementCountArray(primitiveT, primitiveCount) > indexData.Length)
                throw new ArgumentOutOfRangeException("primitiveCount");

            if (vertexDeclaration == null)
                throw new ArgumentNullException("vertexDeclaration");

            PlatformDrawUserIndexedPrimitives<T>(primitiveT, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, vertexDeclaration);

            unchecked
            {
                _graphicsMetrics._drawCount++;
                _graphicsMetrics._primitiveCount += primitiveCount;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primitiveT"></param>
        /// <param name="primitiveCount"></param>
        /// <returns></returns>
        private static int GetElementCountArray(PrimitiveType primitiveT,int primitiveCount)
        {
            switch (primitiveT)
            {
                case PrimitiveType.LineList:
                    return primitiveCount * 2;
                case PrimitiveType.LineStrip:
                    return primitiveCount + 1;
                case PrimitiveType.TriangleList:
                    return primitiveCount * 3;
                case PrimitiveType.TriangleStrip:
                    return primitiveCount + 2;

            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applyShaders"></param>
        internal void ApplyState(bool applyShaders)
        {
            PlatformBeginApplyState();

            PlatformApplyBlend();

            if (_depthStencilStateDirty)
            {
                _actualDepthStencilState.PlatformApplyState(this);
                _depthStencilStateDirty = false;
            }
            if (_rasterizerStateDirty)
            {
                _actualRasterizerState.PlatformApplyState(this);
                _rasterizerStateDirty = false;
            }

            PlatformApplyState(applyShaders);

        }

        internal void ApplyRenderTargets(RenderTargetBinding[] renderTargets)
        {
            var clearTarget = false;

            PlatformResolveRenderTargets();

            Array.Clear(_currentRenderTargetBindings, 0, _currentRenderTargetBindings.Length);


            int renderTargetWidth;
            int renderTargetHeight;
            if(renderTargets == null)
            {
                _currentRenderTargetCount = 0;
                PlatformApplyDefaultRenderTarget();

                clearTarget = PresentationParameters.RenderTargetUsage == RenderTargetUsage.DiscardContents;

                renderTargetWidth = PresentationParameters.BackBufferWidth;
                renderTargetHeight = PresentationParameters.BackBufferHeight;

            }
            else
            {
                Array.Copy(renderTargets, _currentRenderTargetBindings, renderTargets.Length);
                _currentRenderTargetCount = renderTargets.Length;

                var renderTarget = PlatformApplyRenderTargets();


                clearTarget = renderTarget.RenderTargetUsage == RenderTargetUsage.DiscardContents;
                renderTargetWidth = renderTarget.Width;
                renderTargetHeight = renderTarget.Height;
            }

            Viewport = new Viewport(0, 0, renderTargetWidth, renderTargetHeight);

            ScissorRectangle = new Rectangle(0, 0, renderTargetWidth, renderTargetHeight);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    lock (_resourceLock)
                    {
                        foreach(var resource in _resources.ToArray())
                        {
                            var target = resource.Target as IDisposable;
                            if (target != null)
                                target.Dispose();
                        }
                        _resources.Clear();
                    }

                    PlatformDispose();
                }
                _isDisposed = true;
            }
        }
    }
}