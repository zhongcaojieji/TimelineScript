using System;
using System.Collections;
using System.Collections.Generic;

namespace TimeLineScript
{
    /// <summary>
    /// 数据黑板，共享数据
    /// </summary>
    public class BlackBoard
    {
        Dictionary<string, object> bb_data = new Dictionary<string, object>();
        public bool ContainKey(string key)
        {
            return bb_data.ContainsKey(key);
        }
        public void SetValue(string key, object data)
        {
            bb_data[key] = data;
        }
        public T GetValue<T>(string key)
        {
            if (bb_data.ContainsKey(key))
                return (T)bb_data[key];
            return default(T);
        }
    }
    /// <summary>
    /// 片段筛选器，一个剧本可以定义很多段TimeLine,可以只筛选部分播放
    /// </summary>
    public enum TimeLineFilter
    {
        Any = 0, //不过滤
        Standard = 1,//合格，达标
        Perfect = 2//最佳效果
    }
    /// <summary>
    /// 一段表演
    /// </summary>
    [Serializable]
    public class Play
    {
        public enum TimeLinePlayState
        {
            Inactive,
            Awake,
            Running,
        }
        public string Name;//描述剧本，策划用
        public string Tag;//标签分类
        public GameStatePriority BeforePriority = GameStatePriority.Forbidden;//可激活权限
        public GameStatePriority Priority = GameStatePriority.Forbidden;//激活后的状态
        TimeLinePlayState playState = TimeLinePlayState.Inactive;

        public string NextPlay = "";//直接播放下段剧本
        public float CoolingLeftFrame = 0;//触发下段剧本冷却倒计时
        public TimeLineFilter timeline_filter = TimeLineFilter.Any;//有效片段筛选器
        //可调整播放速率，影响整体技能播放时间，动作和特效的播放速度
        public float Rate = 1.0f;
        public int PlayInstanceID = 0;
        static int PlayIDAcc = 0;//简单的累加器
  
        public TimeLine timeline;
        [NonSerialized]
        BlackBoard blackBoard;
        [NonSerialized]
        Actor actor;

        //定义基础属性
        public float JoystickMoveRate = 0;


        float accTimeAfterEnd = 0;
        public Actor GetActor() { return actor; }
        public BlackBoard GetBB() { return blackBoard; }
        //术语：演员请就位
        public void OnAwake(Actor _actor)
        {
            this.actor = _actor;
            blackBoard = new BlackBoard();
            playState = TimeLinePlayState.Awake;
            timeline.OnAwake(this);
        }
        //术语：Action
        public void OnStart(TimeLineFilter filter)
        {
            if (playState == TimeLinePlayState.Running)
                return;
            PlayInstanceID = ++Play.PlayIDAcc;
            this.timeline_filter = filter;
            playState = TimeLinePlayState.Running;
            timeline.OnStart(filter);
            accTimeAfterEnd = 0;
        }
        //术语：Cut 完美
        public void OnEnd()
        {
            playState = TimeLinePlayState.Inactive;
            timeline.OnEnd();
        }
        public bool OnUpdate(float delta_time)
        {
            delta_time *= Rate;
            if (playState != TimeLinePlayState.Running)
            {
                accTimeAfterEnd += delta_time;
                return false;
            }
            if (timeline.OnUpdate(delta_time) != Task.TaskStatus.Running)
            {
                playState = TimeLinePlayState.Awake;
                accTimeAfterEnd += delta_time;
                timeline.OnEnd();
                return false;
            }
            return true;
        }
        //术语：Cut-Cut!!!!
        public void Interrupt()
        {
            playState = TimeLinePlayState.Awake;
            timeline?.Interrupt();
        }
        public bool IsPlay()
        {
            return playState == TimeLinePlayState.Running;
        }

        float CoolingLeftTime
        {
            get { return CoolingLeftFrame / 30.0f; }
        }
        public string GetNextPlay()
        {
            if (accTimeAfterEnd <= CoolingLeftTime)
            {
                return NextPlay;
            }
            return "";
        }
    }
}