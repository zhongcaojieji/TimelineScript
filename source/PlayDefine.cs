using System;
using System.Collections.Generic;
namespace TimeLineScript
{
    public enum GameStatePriority
    {
        Forbidden,//不可切换任何状态
        Walk = 1,//可以移动
        ActiveAbilityDie = 1 << 2, //死亡技能
        ActiveAbilityShoot = 1 << 3,//可以使用（射击）技能
        ActiveAbilitySlash = 1 << 4,//可以使用（攻击）技能
        ActiveAbilitySprint = 1 << 5,//可以使用（闪避）技能
        ActiveAbilityFinishing = 1 << 6,//可以使用大招技能
        ActiveAbilitySeldom = 1 << 7,//特殊技能
        DamageAbility = 1 << 8,//可以受击
        Jump = 1 << 9,//可以进入跳台状态
        ActiveAbilityBirth = 1 << 10,
        ActiveAbility = ActiveAbilityDie | ActiveAbilityShoot | ActiveAbilitySlash | ActiveAbilitySprint | ActiveAbilityFinishing | ActiveAbilitySeldom | ActiveAbilityBirth | Jump,//可以使用（任何）技能

        Free = ActiveAbilityDie | Walk | ActiveAbility | DamageAbility | Jump | ActiveAbilityBirth, //自由切换
        MoveAndActiveAbility = Walk | ActiveAbility,//可以移动可以放技能
        JumpState = ActiveAbilitySprint | Jump,//跳台状态，可以进入跳台状态，可以使用（闪避）技能
        Die = Forbidden,//死亡技能最高优先级
        Birth = Forbidden,//死亡技能最高优先级
        AbilityType_End = Forbidden, //结束技能最高优先级
    }
    public enum ScriptInterpreterType
    {
        Assignment,//赋值操作
        Inherit,//数据继承
        TimeLine,
        TimeLineClip,
        Controller,//控制器
        Action,//预定义的行为
    }
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class InterpreterTypeAttribute : System.Attribute
    {
        public ScriptInterpreterType type;

        public InterpreterTypeAttribute(ScriptInterpreterType inputType)
        {
            type = inputType;
        }
    }
    [Serializable]
    public class Task
    {
        public Task()
        {

        }
        public enum TaskStatus
        {
            Inactive = 0,
            Failure = 1,
            Success = 2,
            Running = 3
        }
        [NonSerialized]
        protected Play play;
        protected Actor actor
        {
            get
            {
                return play.GetActor();
            }
        }
        public virtual void OnAwake(Play play)
        {
            this.play = play;
        }
        public virtual void OnDestory() { }
        public virtual void OnStart() { }
        public virtual void OnEnd() { }
        public virtual TaskStatus OnUpdate(float time)
        {
            return TaskStatus.Inactive;
        }
        public virtual void Interrupt()
        {
        }
    }
    [Serializable]
    [InterpreterType(ScriptInterpreterType.TimeLine)]
    public class TimeLine : Task
    {
        TaskStatus status = TaskStatus.Inactive;
        float playTime = 0;
        public List<TimeLineClip> clips = new List<TimeLineClip>();
        List<TimeLineClip> activeClips = new List<TimeLineClip>();
        public override void OnAwake(Play play)
        {
            base.OnAwake(play);
            for (int i = 0; i < clips.Count; i++)
            {
                clips[i].OnAwake(play);
            }
        }
        public void OnStart(TimeLineFilter filter)
        {
            activeClips.Clear();
            for (int i = 0; i < clips.Count; i++)
            {
                if(clips[i].CheckJudge(filter))
                {
                    activeClips.Add(clips[i]);
                    clips[i].OnStart();
                }
            }
            status = TaskStatus.Running;
            playTime = 0;
        }
        public override void OnEnd()
        {
            status = TaskStatus.Inactive;
            for (int i = 0; i < activeClips.Count; i++)
            {
                activeClips[i].OnEnd();
            }
        }
        public override TaskStatus OnUpdate(float delta_time)
        {
            if (status == TaskStatus.Inactive)
                return TaskStatus.Inactive;
            playTime += delta_time;
            TaskStatus childStatus = TaskStatus.Success;
            for (int i = 0; i < activeClips.Count; i++)
            {
                TaskStatus status = activeClips[i].OnUpdate(playTime, delta_time);
                if (status == TaskStatus.Running || status == TaskStatus.Inactive)
                {
                    childStatus = TaskStatus.Running;
                }
            }
            if(childStatus == TaskStatus.Success)
            {
                status = TaskStatus.Inactive;
            }
            return childStatus;
        }
        public override void Interrupt()
        {
            status = TaskStatus.Inactive;
            for (int i = 0; i < activeClips.Count; i++)
            {
                activeClips[i].Interrupt();
            }
            activeClips.Clear();
        }
    }
    [Serializable]
    [InterpreterType(ScriptInterpreterType.TimeLineClip)]
    public class TimeLineClip : Task
    {
        public string Filter = "Any";
        public string Description = "";
        public int StartFrame = 0;
        public int EndFrame = 0;
        public bool loop = false;
        public int loopTime = 0;
        int accLoopTime = 0;
        public Entry entry = new Entry();
        readonly static float TimeToFrameRate = 30.0f;
        TaskStatus status = TaskStatus.Inactive;
        string[] levels;
        public bool CheckJudge(TimeLineFilter filter)
        {
            for(int i = 0; i < levels.Length; i++)
            {
                if (Check(levels[i], filter))
                    return true;
            }
            return false;
        }
        bool Check(string level, TimeLineFilter result)
        {
            if (level == "Any")
                return true;
            if (result == TimeLineFilter.Standard)
                return level == "Standard";
            if (result == TimeLineFilter.Perfect)
                return level == "Perfect";
                    return false;
        }
        bool CheckActive(float play_time)
        {
            float fframe = play_time * TimeToFrameRate;
            if(loop)
            {
                fframe -= (EndFrame - StartFrame) * accLoopTime;
            }
            return fframe >= StartFrame && fframe <= EndFrame;
        }
        public override void OnAwake(Play play)
        {
            levels = Filter.Split(',');
            base.OnAwake(play);
            entry.OnAwake(play);
        }
        public override void OnStart()
        {
            accLoopTime = 0;
        }
        public override void OnEnd()
        {
            if (status != TaskStatus.Inactive)
            {
                entry.OnEnd();
            }
            status = TaskStatus.Inactive;
        }
        public TaskStatus OnUpdate(float play_time, float delta_time)
        {
            if (CheckActive(play_time))
            {
                if (status == TaskStatus.Inactive)
                {
                    entry.OnStart();
                    status = TaskStatus.Running;
                }
                entry.OnUpdate(delta_time);
                return TaskStatus.Running;
            }
            else
            {
                if (status != TaskStatus.Inactive)
                {
                    entry.OnEnd();
                }
                status = TaskStatus.Inactive;
                if (loop && accLoopTime + 1 < loopTime)
                {
                    accLoopTime++;
                    return TaskStatus.Running;
                }
                return TaskStatus.Success;
            }
        }
        public override void Interrupt()
        {
            if (status != TaskStatus.Inactive)
            {
                entry.Interrupt();
            }
            status = TaskStatus.Inactive;
        }
    }
}