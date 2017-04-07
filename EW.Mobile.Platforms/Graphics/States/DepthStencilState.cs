using System;

namespace EW.Mobile.Platforms.Graphics
{
    /// <summary>
    /// 定义模板缓冲对象操作
    /// </summary>
    public enum StencilOperation
    {
        Keep,
        Zero,
        Replace,
        Increment,
        Decrement,
        IncrementSaturation,
        DecrementSaturation,
        Invert,
    }

    public enum CompareFunction
    {
        Always,
        Never,
        Less,
        LessEqual,
        Equal,
        GreaterEqual,
        Greater,
        NotEqual,
    }

    /// <summary>
    /// 深度&模板 状态
    /// </summary>
    public partial class DepthStencilState:GraphicsResource
    {

        private readonly bool _defaultStateObject;

        public static readonly DepthStencilState Default;
        public static readonly DepthStencilState DepthRead;
        public static readonly DepthStencilState None;


        static DepthStencilState()
        {

        }

        public DepthStencilState()
        {

        }

        private DepthStencilState(string name,bool depthBufferEnable,bool depthBufferWriteEnable):this()
        {
            Name = name;
            _depthBufferEnable = depthBufferEnable;
            _depthBufferWriteEnable = depthBufferWriteEnable;
            _defaultStateObject = true;
        }

        internal void BindToGraphicsDevice(GraphicsDevice device)
        {
            if (GraphicsDevice != null && GraphicsDevice != device)
                throw new InvalidOperationException("This Depth stencil state is already bound to a different graphics device!");
            GraphicsDevice = device;
        }



        private bool _depthBufferEnable;
        public bool DepthBufferEnable
        {
            get { return _depthBufferEnable; }
            set
            {
                _depthBufferEnable = value;
            }
        }

        private bool _depthBufferWriteEnable;

        /// <summary>
        /// 深度缓冲区写入
        /// </summary>
        public bool DepthBufferWriteEnable
        {
            get { return _depthBufferWriteEnable; }
            set
            {
                _depthBufferWriteEnable = value;
            }
        }

        private CompareFunction _depthBufferFunction;

        public CompareFunction DepthBufferFunction
        {
            get { return _depthBufferFunction; }
            set
            {
                _depthBufferFunction = value;
            }
        }

        private bool _stencilEnable;

        public bool StencilEnable
        {
            get { return _stencilEnable; }
            set
            {
                _stencilEnable = value;
            }
        }

        private int _stencilWriteMask;

        public int StencilWriteMask
        {
            get { return _stencilWriteMask; }
            set
            {
                _stencilWriteMask = value;
            }
        }

        private StencilOperation _stencilFail;

        /// <summary>
        /// 如果模板测试失败将采取的操作
        /// </summary>
        public StencilOperation StencilFail
        {
            get { return _stencilFail; }
            set
            {
                _stencilFail = value;
            }
        }

        /// <summary>
        /// 如果模板测试通过，但是深度测试失败时采取的操作
        /// </summary>
        private StencilOperation _stencilDepthBufferFail;

        public StencilOperation StencilDetphBufferFail
        {
            get { return _stencilDepthBufferFail; }
            set
            {
                _stencilDepthBufferFail = value;
            }
        }

        private StencilOperation _stencilPass;

        public StencilOperation StencilPass
        {
            get { return _stencilPass; }
            set
            {
                _stencilPass = value;
            }
        }


        private CompareFunction _stencilFunction;
        /// <summary>
        /// 模板函数
        /// </summary>
        public CompareFunction StencilFunction
        {
            get { return _stencilFunction; }
            set
            {
                _stencilFunction = value;
            }
        }

        private int _referenceStencil;

        /// <summary>
        /// 指定模板测试的参考值，模板缓冲的内容会与这个值对比
        /// </summary>
        public int ReferenceStencil
        {
            get { return _referenceStencil; }
            set
            {
                _referenceStencil = value;
            }
        }

        private int _stencilMask;

        /// <summary>
        /// 指定一个掩码值，在模板测试比较参考值和储存的模板值前，会用掩码值对它分别进行按位与(AND)操作，初始情况下所有位都为1
        /// </summary>
        public int StencilMask
        {
            get { return _stencilMask; }
            set
            {
                _stencilMask = value;
            }
        }

        private bool _twoSidedStencilMode;

        public bool TwoSidedStencilMode
        {
            get { return _twoSidedStencilMode; }
            set
            {
                _twoSidedStencilMode = value;
            }
        }

    }
}