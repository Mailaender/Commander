using System;
using System.Collections.Generic;
using System.Linq;
using EW.Traits;
namespace EW.Activities
{

    /// <summary>
    /// ���������������ͼ�����ݽṹ��.ÿ������һ��������Ϳ�ѡ���Ӽ��(ͨ������һ���),CurrentActivity ��һ��ָ���ͼ��ָ�룬���Ż�Ľ��ж��ƶ���
    /// <summary>
    /// </summary>
    public enum ActivityState 
    { 
        Queued,//队列�?
        Active, //活动�?
        Done,   //已完�?
        Canceled //已取�?
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class Activity
    {

        public ActivityState State { get; private set; }    //活动状�?

        public bool IsInterruptible { get; protected set; } //标识是否可以中断

        public Activity()
        {
            IsInterruptible = true; //Ĭ������»�ǿ��Ա��ж�
        }

        /// <summary>
        /// ��ʶ��Ƿ��ѱ�ȡ��
        /// </summary>
        public bool IsCanceled
        {
            get
            {
                return State == ActivityState.Canceled;
            }
        }

        /// <summary>
        /// Returns the top-most activity *from the point of view of the calling activity*.
        /// Note that the root activity can and likely will have next activities of its own,which would in turn be the root for their children.
        /// �ӵ����߻�ĽǶȷ�����˵Ļ��
        /// ��Դ�Ļ���ܶ��ҿ��ܻ����Լ�����һ���ӻ���ⷴ�������������Ӽ��ĸ�Դ
        /// </summary>
        public Activity RootActivity
        {
            get
            {
                var p = this;
                while (p.ParentActivity != null)
                    p = p.ParentActivity;
                return p;
            }
        }

        /// <summary>
        /// �����
        /// </summary>
        Activity parentActivity;
        public Activity ParentActivity
        {
            get { return parentActivity; }
            set
            {
                parentActivity = value;

                var next = NextInQueue;
                if (next != null)
                    next.ParentActivity = parentActivity;
            }
        }


        /// <summary>
        /// �ӻ
        /// </summary>
        Activity childActivity;
        protected Activity ChildActivity
        {
            get
            {
                return childActivity != null && childActivity.State < ActivityState.Done ? childActivity : null;
            }
            set
            {
                if (value == this || value == ParentActivity || value == NextInQueue)
                    childActivity = null;
                else
                {
                    childActivity = value;

                    if (childActivity != null)
                        childActivity.ParentActivity = this;
                }
            }
        }

        Activity nextActivity;
        /// <summary>
        /// The getter will return either the next activity or,if there is none,the parent one.
        /// getter ��������һ��������û�У��򷵻ظ����
        /// </summary>
        public virtual Activity NextActivity
        {
            get
            {
                return nextActivity != null ? nextActivity : ParentActivity;
            }
            set
            {
                
                if (value == this || value == ParentActivity || (value != null && value.ParentActivity == this))
                    nextActivity = null;//����û�������ŶӵĻ��
                else
                {
                    nextActivity = value;
                    if (nextActivity != null)
                        nextActivity.ParentActivity = ParentActivity;
                }
            }
        }

        /// <summary>
        /// The getter will return the next activity on the same level_only_,in contrast to NextActivity.
        /// Use this to check whether there are any follow-up activities queued.
        /// ��NextActivity��ȣ�getter��������ͬlevel_only_�ϵ���һ���������������Ƿ����κκ�����Ŷӡ�
        /// </summary>
        public Activity NextInQueue
        {
            get { return nextActivity; }
            set
            {
                NextActivity = value;
            }
        }
        
        public Activity TickOuter(Actor self)
        {
            if (State == ActivityState.Done && WarGame.Settings.Debug.StrictActivityChecking)
                throw new InvalidOperationException("Actor {0} attempted to tick activity {1} after it had already completed.".F(self, this.GetType()));

            if(State == ActivityState.Queued)
            {
                OnFirstRun(self);
                State = ActivityState.Active;
            }

            var ret = Tick(self);
            if(ret == null || (ret!=this && ret.ParentActivity != this))
            {
                //Make sure that the Parent's ChildActivity pointer is moved forwards as the child queue advances.
                //The Child's ParentActivity will be set automatically during assignment.
                //ȷ���������ӻָ�������Ӷ��е�ǰ������ǰ�ƶ�
                //����ĸ�����ڷ���������Զ�����
                if (ParentActivity != null && ParentActivity != ret)
                    ParentActivity.ChildActivity = ret;

                if (State != ActivityState.Canceled)
                    State = ActivityState.Done;

                OnLastRun(self);
            }

            return ret;
        }


        public abstract Activity Tick(Actor self);

        /// <summary>
        /// �ȡ��
        /// </summary>
        /// <param name="self"></param>
        /// <param name="keepQueue">��ʶ�Ƿ񱣳ֶ���˳��</param>
        /// <returns></returns>
        public virtual bool Cancel(Actor self, bool keepQueue = false)
        {
            if (!IsInterruptible)
                return false;

            if (ChildActivity != null && !ChildActivity.Cancel(self))
                return false;

            if(!keepQueue)
                NextActivity = null;

            ChildActivity = null;
            State = ActivityState.Canceled;
            return true;
        }

        /// <summary>
        /// һ����Ŷ�
        /// 
        /// </summary>
        /// <param name="activity">�����ǰû�����ŶӵĻ��activity ������һ���.����������ŶӵĻ��activity���ڵ�ǰ�֮��</param>
        public virtual void Queue(Activity activity)
        {

            if (NextInQueue != null)
                NextInQueue.Queue(activity);
            else
                NextInQueue = activity;
        }

        /// <summary>
        /// ������Ŷ�
        /// </summary>
        /// <param name="activity"></param>
        public virtual void QueueChild(Activity activity)
        {
            if (ChildActivity != null)
                ChildActivity.Queue(activity);
            else
                ChildActivity = activity;
        }

        public virtual IEnumerable<Target> GetTargets(Actor self)
        {
            yield break;
        }

        protected virtual void OnFirstRun(Actor self) { }

        protected virtual void OnLastRun(Actor self) { }

        protected void PrintActivityTree(Activity origin = null,int level = 0)
        {
            if (origin == null)
                RootActivity.PrintActivityTree(this);
            else
            {

            }
        }

    }

    /// <summary>
    /// In cointrast to the base activity class,which is responsible for running its children itself,
    /// composite activities rely on the actor's activity-running logic for their children.
    /// </summary>
    public  abstract class CompositeActivity : Activity
    {
        public override Activity NextActivity
        {
            get
            {
                if (ChildActivity != null)
                    return ChildActivity;
                else if (NextInQueue != null)
                    return NextInQueue;
                else
                    return ParentActivity;
            }
        }
    }

    public static class ActivityExts
    {
        public static IEnumerable<Target> GetTargetQueue(this Actor self)
        {
            return self.CurrentActivity.Iterate(u => u.NextActivity).TakeWhile(u => u != null).SelectMany(u => u.GetTargets(self));
        }
    }


}