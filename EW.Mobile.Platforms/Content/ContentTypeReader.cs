using System;


namespace EW.Mobile.Platforms.Content
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ContentTypeReader
    {

        private Type _targetT;

        public Type TargetT
        {
            get { return _targetT; }
        }

        protected ContentTypeReader(Type targetT)
        {
            _targetT = targetT;
        }

        /// <summary>
        /// �Ƿ�ɷ����л����Ѵ��ڵĶ���
        /// </summary>
        public virtual bool CanDeserializeIntoExistingObject { get { return false; } }




    }
}