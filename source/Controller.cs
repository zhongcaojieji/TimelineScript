using System;
using System.Collections.Generic;
namespace TimeLineScript
{
    /// <summary>
    /// 流程调度节点定义
    /// </summary>
    [Serializable]
    public class Controller : Task
    {
        public virtual void AddSubTask(Task task) { }
    }
    [Serializable]
    public class Entry : Controller
    {
        public Controller _controller;
        [NonSerialized]
        bool finish = false;
        public override void AddSubTask(Task task)
        {
            _controller = task as Controller;
        }
        public override void OnAwake(Play play)
        {
            base.OnAwake(play);
            _controller?.OnAwake(play);
            finish = false;
        }
        public override void OnStart()
        {
            finish = false;
            _controller?.OnStart();
        }
        public override void OnEnd()
        {
            _controller?.OnEnd();
        }
        public override void Interrupt()
        {
            _controller?.Interrupt();
        }
        public override TaskStatus OnUpdate(float play_time)
        {
            if(!finish)
            {
                if (_controller != null)
                {
                    TaskStatus res = _controller.OnUpdate(play_time);
                    finish = res == TaskStatus.Running ? false : true;
                    return res;
                }
            }
            return TaskStatus.Success;
        }
    }
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Controller)]
    public class Sequence : Controller
    {
        public enum ContinueStrategy
        {
            ContinueAlways,
            ContinueWhenSucc,
            ContinueWhenFailed,
        }
        public ContinueStrategy continueStrategy = ContinueStrategy.ContinueAlways;
        public List<Task> executions = new List<Task>();
        int lastRunning = 0;
        public override void AddSubTask(Task task)
        {
            executions.Add(task);
        }
        public override void OnAwake(Play play)
        {
            base.OnAwake(play);
            for (int i = 0; i < executions.Count; i++)
            {
                executions[i].OnAwake(play);
            }
        }
        public override void OnStart()
        {
            if (executions.Count > 0)
            {
                executions[0].OnStart();
            }
            lastRunning = 0;
        }
        public override void OnEnd()
        {
            for (int i = 0; i < executions.Count; i++)
            {
                executions[i].OnEnd();
            }
        }
        public override void Interrupt()
        {
            for (int i = 0; i < executions.Count; i++)
            {
                executions[i].Interrupt();
            }
        }
        public override TaskStatus OnUpdate(float time)
        {
            int start = lastRunning;
            for (int i = start; i < executions.Count; i++)
            {
                Task task = executions[i];
                if (lastRunning < i)
                {
                    task.OnStart();
                }
                TaskStatus status = task.OnUpdate(time);
                if(status == TaskStatus.Running)
                {
                    lastRunning = i;
                    return TaskStatus.Running;
                }
                if (status == TaskStatus.Failure)
                {
                    if (continueStrategy != ContinueStrategy.ContinueWhenFailed)
                        return TaskStatus.Failure;
                }
                if (status == TaskStatus.Success)
                {
                    if (continueStrategy == ContinueStrategy.ContinueWhenFailed)
                        return TaskStatus.Success;
                }

            }
            return TaskStatus.Success;
        }
    }
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Controller)]
    public class Parallel : Controller
    {
        public enum BreakStrategy
        {
            RunAlways,
            BreakWhenSucc,
            BreakWhenFailed,
        }
        public BreakStrategy breakStrategy = BreakStrategy.RunAlways;
        public List<Task> executions = new List<Task>();
        List<Task> stillRunning = new List<Task>();
        public override void AddSubTask(Task task)
        {
            executions.Add(task);

        }
        public override void OnAwake(Play play)
        {
            base.OnAwake(play);
            for (int i = 0; i < executions.Count; i++)
            {
                executions[i].OnAwake(play);
            }
        }
        public override void OnStart()
        {
            stillRunning.Clear();
            for (int i = 0; i < executions.Count; i++)
            {
                stillRunning.Add(executions[i]);
            }
            for (int i = 0; i < executions.Count; i++)
            {
                executions[i].OnStart();
            }
        }
        public override void OnEnd()
        {
            for (int i = 0; i < executions.Count; i++)
            {
                executions[i].OnEnd();
            }
        }
        public override void Interrupt()
        {
            for (int i = 0; i < executions.Count; i++)
            {
                executions[i].Interrupt();
            }
        }
        public override TaskStatus OnUpdate(float time)
        {
            for (int i = 0; i < stillRunning.Count; i++)
            {
                Task task = stillRunning[i];
                TaskStatus status = task.OnUpdate(time);
                if (status != TaskStatus.Running)
                {
                    stillRunning.Remove(task);
                    --i;
                }
            }
            if (stillRunning.Count == 0)
                return TaskStatus.Success;

            return TaskStatus.Running;
        }
    }
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Controller)]
    public class Delay : Controller
    {
        public float DelayFrame = 0;
        public Task subNode;
        float accTime = 0;
        public override void AddSubTask(Task task)
        {
            subNode = task;
        }
        public override void OnAwake(Play play)
        {
            base.OnAwake(play);
            subNode.OnAwake(play);
        }
        public override void OnStart()
        {
            accTime = 0;
        }
        public override void OnEnd()
        {
            subNode.OnEnd();
        }
        public override void Interrupt()
        {
            subNode.Interrupt();
        }
        float DelayTime
        {
            get { return DelayFrame / 30.0f; }
        }
        public override TaskStatus OnUpdate(float time)
        {
            accTime += time;
            if (accTime >= DelayTime)
            {
                if (accTime - time < DelayTime)
                {
                    subNode.OnStart();
                }
                return subNode.OnUpdate(time);
            }
            return TaskStatus.Running;
        }
    }
}