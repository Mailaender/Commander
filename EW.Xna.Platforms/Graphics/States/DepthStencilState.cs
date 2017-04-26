using System;

namespace EW.Xna.Platforms.Graphics
{
    /// <summary>
    /// ����ģ�建��������
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
    /// ���&ģ�� ״̬
    /// </summary>
    public partial class DepthStencilState:GraphicsResource
    {

        private readonly bool _defaultStateObject;

        public static readonly DepthStencilState Default;
        public static readonly DepthStencilState DepthRead;
        public static readonly DepthStencilState None;


        static DepthStencilState()
        {
            Default = new DepthStencilState("DepthStencilState.Default", true, true);
            DepthRead = new DepthStencilState("DepthStencilState.DepthRead", true, false);
            None = new DepthStencilState("DepthStencilState.None", false, false);
        }

        public DepthStencilState() { }
        private DepthStencilState(DepthStencilState cloneSource)
        {
            Name = cloneSource.Name;
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
        /// ��Ȼ�����д��
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
        /// ���ģ�����ʧ�ܽ���ȡ�Ĳ���
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
        /// ���ģ�����ͨ����������Ȳ���ʧ��ʱ��ȡ�Ĳ���
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
        /// ģ�庯��
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
        /// ָ��ģ����ԵĲο�ֵ��ģ�建������ݻ������ֵ�Ա�
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
        /// ָ��һ������ֵ����ģ����ԱȽϲο�ֵ�ʹ����ģ��ֵǰ����������ֵ�����ֱ���а�λ��(AND)��������ʼ���������λ��Ϊ1
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
        internal DepthStencilState Clone()
        {
            return new DepthStencilState(this);
        }

    }
}