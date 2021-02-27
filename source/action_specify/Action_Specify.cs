using System;
using System.Collections.Generic;
using System.Numerics;
/// <summary>
/// 特化的行为节点
/// </summary>
namespace TimeLineScript
{
    /// <summary>
    /// 播放动画
    /// </summary>
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Action)]
    public class PlayAction : Action
    {
        public string ActionName = "";
        public override void OnStart()
        {
            actor?.PlayAction(ActionName, 1.0f);
        }
    }
    /// <summary>
    /// 向前移动
    /// </summary>
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Action)]
    public class MoveForward : Action
    {
        public float Frame = 0.0f;
        public float Speed1 = 0.0f;
        public float Speed2 = 0.0f;
        public float Rotation = 0.0f;
        [NonSerialized]
        float accTime = 0.0f;
        public float Time
        {
            get { return Frame / 30.0f; }
        }
        public override void OnStart()
        {
            accTime = 0;
        }
        public override TaskStatus OnUpdate(float time)
        {
            accTime += time;
            float speed = Speed1 + (Speed2 - Speed1) * accTime / Time;
            Vector3 moveVec = actor.Dir * time * speed;
            actor.Move(moveVec);
            return accTime >= Time ? TaskStatus.Success : TaskStatus.Running;
        }
    }

    /// <summary>
    /// 显示演员
    /// </summary>
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Action)]
    public class EnableRender : Action
    {
        public string SubNode = "";
        public override void OnStart()
        {
            if (!string.IsNullOrEmpty(SubNode))
            {
                //显示部位
            }
            else
            {
                actor.SetVisible(true);
            }

        }
    }
    /// <summary>
    /// 隐藏演员
    /// </summary>
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Action)]
    public class DisableRender : Action
    {
        public string SubNode = "";
        public override void OnStart()
        {
            if (!string.IsNullOrEmpty(SubNode))
            {
                //隐藏部位
            }
            else
            {
                actor.SetVisible(true);
            }
        }
    }
}