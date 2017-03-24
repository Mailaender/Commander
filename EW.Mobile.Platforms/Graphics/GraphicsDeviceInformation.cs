

namespace EW.Mobile.Platforms.Graphics
{

    public enum GraphicsProfile
    {
        Reach,
        HiDef,
    }

    /// <summary>
    /// ͼ���豸��Ϣ
    /// </summary>
    public class GraphicsDeviceInformation
    {

        public GraphicsAdapter Adapter { get; set; }

        public GraphicsProfile GraphicsProfile { get; set; }

        public PresentationParameters PresentationParameters { get; set; }

    }
}