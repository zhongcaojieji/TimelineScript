using System;
using System.Collections.Generic;
/// <summary>
/// 
/// </summary>
namespace TimeLineScript
{
    /// <summary>
    /// 行为节点
    /// </summary>
    [Serializable]
    public class Action : Task
    {
        public override TaskStatus OnUpdate(float time)
        {
            return TaskStatus.Success;
        }

        protected T Parse<T>(string prefix_key)
        {
            if(prefix_key.Contains("#BB_"))
            {
                string real_key = prefix_key.Substring(4, prefix_key.Length - 4);
                var bb = play.GetBB();
                if (bb.ContainKey(real_key))
                    return bb.GetValue<T>(real_key);
            }
            return default(T);
        }
    }
    /// Builtin Actions 内置行为
    //---------------------------------------------------------------------------------------------

    /// <summary>
    /// 修改剧本的属性
    /// </summary>
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Action)]
    public class ModifyPlay : Action
    {
        public string Key;
        public string Value;
        public override void OnStart()
        {
            Interpreter.InterpreterAssignment(Key, Value, play);
        }
    }
    /// <summary>
    /// 写黑板数据例子
    /// </summary>
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Action)]
    public class TestWriteToBlackBoard : Action
    {
        public override void OnStart()
        {
            play.GetBB().SetValue("PI", 3.14159565358);
        }
    }
    /// <summary>
    /// 从黑板数据读取的例子
    /// </summary>
    [Serializable]
    [InterpreterType(ScriptInterpreterType.Action)]
    public class TestReadBlackBoard : Action
    {
        public string Param = "";
        public override void OnStart()
        {
            var pi = Parse<double>(Param);
            Console.WriteLine("PI =" + pi);
        }
    }
}
    