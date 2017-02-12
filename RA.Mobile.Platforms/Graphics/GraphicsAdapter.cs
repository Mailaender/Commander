using System;
using System.Collections.ObjectModel;
using Android.Views;
namespace RA.Mobile.Platforms.Graphics
{
    /// <summary>
    /// ͼ��������
    /// </summary>
    public sealed class GraphicsAdapter:IDisposable
    {

        public enum DriverT
        {
            Hardware,//Ӳ������
            Reference,//ģ��Ӳ��(����������)
            FastSoftware,//�������(�����豸��֧��Ӳ������)
        }

        private static ReadOnlyCollection<GraphicsAdapter> _adapters;
        GraphicsAdapter() { }

        public void Dispose()
        {

        }

        public DisplayMode CurrentDisplayMode
        {
            get
            {
#if ANDROID
                View view = ((AndroidGameWindow)Game.Instance.Window).GameView;
                return new DisplayMode(view.Width, view.Height, SurfaceFormat.Color);
#endif
            }
        }

        public static GraphicsAdapter DefaultAdapter
        {
            get { return Adapters[0]; }
        }
        public static ReadOnlyCollection<GraphicsAdapter> Adapters
        {
            get
            {
                if(_adapters == null)
                {
#if ANDROID
                    _adapters = new ReadOnlyCollection<GraphicsAdapter>(new[] { new GraphicsAdapter() });

#endif
                }
                return _adapters;
            }
        }





    }
}