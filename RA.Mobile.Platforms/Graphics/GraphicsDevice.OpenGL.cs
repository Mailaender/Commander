using System;
using System.Collections.Generic;

namespace RA.Mobile.Platforms.Graphics
{
    public partial class GraphicsDevice
    {
        /// <summary>
        /// ƽ̨��ʼ��
        /// </summary>
        private void PlatformInitialize()
        {
            _viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

        }


    }
}