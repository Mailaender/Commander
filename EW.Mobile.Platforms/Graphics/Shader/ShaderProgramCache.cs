using System;
using System.Collections.Generic;
using OpenTK.Graphics.ES20;

namespace RA.Mobile.Platforms.Graphics
{

    /// <summary>
    /// shader С����
    /// </summary>
    internal class ShaderProgram
    {
        public readonly int Program;
        private readonly Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();


        public ShaderProgram(int program)
        {
            Program = program;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetUnitformLocation(string name)
        {
            if (_uniformLocations.ContainsKey(name))
                return _uniformLocations[name];

            var location = GL.GetUniformLocation(Program, name);
            GraphicsExtensions.CheckGLError();
            _uniformLocations[name] = location;
            return location;
        }
    }


    /// <summary>
    /// shader ���򻺴�
    /// </summary>
    internal class ShaderProgramCache:IDisposable
    {
        private readonly Dictionary<int, ShaderProgram> _programCache = new Dictionary<int, ShaderProgram>();
        bool disposed;
        ~ShaderProgramCache()
        {
            Dispose(false);   
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    Clear();
                disposed = true;
            }
        }

        /// <summary>
        /// ������򻺴棬�ͷ�����shader С����
        /// </summary>
        public void Clear()
        {

        }

    }
}