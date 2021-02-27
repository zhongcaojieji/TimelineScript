using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
namespace TimeLineScript
{
    /// <summary>
    /// 解释器，将脚本配置翻译成实例化的对象并进行赋值
    /// </summary>
    public class Interpreter{
        //剧本文本数据
        public Dictionary<string, object> json_dict = new Dictionary<string, object>();
        //保存实例化的表演对象
        public Dictionary<string, object> instance_dict = new Dictionary<string, object>();
        public Play GetPlay(string play_name)
        {
            //这里直接深度拷贝一个表演对象
            if(instance_dict.ContainsKey(play_name))
            {
                BinaryFormatter Formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                MemoryStream stream = new MemoryStream();
                Formatter.Serialize(stream, instance_dict[play_name]);
                stream.Position = 0;
                object clonedObj = Formatter.Deserialize(stream);
                stream.Close();
                return clonedObj as Play;
            }
            return null;
        }
        public void Init()
        {
            string file = Environment.CurrentDirectory + "/../../../script_config/play_script.json";
            json_dict = LoadData(file);
            //可以缓存实例化所有的表演
            //也可以按需实例化
            InstanceAllPlays();
        }
        private Dictionary<string, object> LoadData(string file)
        {
            var dataStr = string.Empty;
            using (var sr = new StreamReader(file, Encoding.UTF8))
            {
                dataStr = sr.ReadToEnd();
            }
            return Json.Deserialize(dataStr) as Dictionary<string, object>;
        }
        public void Destruct()
        {
            json_dict.Clear();
            instance_dict.Clear();
        }
        //通过反射+深拷贝继承数据
        public void Inherit(object parent, object child)
        {
            FieldInfo[] fields = parent.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                FieldInfo fi = child.GetType().GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (fi != null)
                {
                    var value = fi.GetValue(parent);
                    if (value == null)
                    {
                        return;
                    }
                    if (value is string || value.GetType().IsValueType)
                        fi.SetValue(child, fi.GetValue(parent));
                    else
                    {

                        BinaryFormatter Formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                        MemoryStream stream = new MemoryStream();
                        Formatter.Serialize(stream, value);
                        stream.Position = 0;
                        object clonedObj = Formatter.Deserialize(stream);
                        stream.Close();
                        fi.SetValue(child, clonedObj);
                    }
                }
            }
        }
        private T DeepCopy<T>(T obj)
        {
            //如果是字符串或值类型则直接返回
            if (obj is string || obj.GetType().IsValueType) return obj;

            object retval = Activator.CreateInstance(obj.GetType());
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                try { field.SetValue(retval, DeepCopy(field.GetValue(obj))); }
                catch { }
            }
            return (T)retval;
        }
        public static ScriptInterpreterType GetInterpreterType(string key)
        {
            System.Type classtype = System.Type.GetType("TimeLineScript." + key);
            if(classtype != null)
            {
                InterpreterTypeAttribute attr = classtype.GetCustomAttribute<InterpreterTypeAttribute>();
                if(attr != null)
                    return attr.type;
            }
            switch (key)
            {
                case "BasePlay":
                    return ScriptInterpreterType.Inherit;
                default:
                    return ScriptInterpreterType.Assignment;
            }

        }
        Action CreateAction(string type)
        {
            Action obj = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance("TimeLineScript." + type, false) as Action;
            return obj;
        }
        public void InstanceAllPlays()
        {
            foreach (var _playname in json_dict)
            {
                if (!instance_dict.ContainsKey(_playname.Key))
                {
                    Play play_ins = new Play();
                    var abDetail = _playname.Value as Dictionary<string, object>;
                    foreach (var detail in abDetail)
                    {
                        InterpreterDict(detail.Key, detail.Value, play_ins);
                    }
                    play_ins.Name = _playname.Key;
                    instance_dict.Add(_playname.Key, play_ins);
                }
            }
        }
        public void InterpreterDict<T>(string key, object value, T t)
        {
            ScriptInterpreterType checktype = GetInterpreterType(key);
            if (checktype == ScriptInterpreterType.Inherit)
            {
                string parent_name = value as string;
                InterpreterInherit(parent_name, t);
            }
            else if (checktype == ScriptInterpreterType.Assignment)
            {
                Interpreter.InterpreterAssignment(key, value, t);
            }
            else if (checktype == ScriptInterpreterType.TimeLine)
            {
                Play play_ins = t as Play;
                if (play_ins == null)
                    return;
                play_ins.timeline = new TimeLine();
                var timelineclips = value as List<object>;
                foreach (var clip in timelineclips)
                {
                    InterpreterDict("TimeLineClip", clip, play_ins.timeline);
                }
            }
            else if (checktype == ScriptInterpreterType.TimeLineClip)
            {
                TimeLine timeline = t as TimeLine;
                if (timeline == null)
                    return;
                TimeLineClip clip = new TimeLineClip();
                var detail = value as Dictionary<string, object>;
                foreach (var item in detail)
                {
                    InterpreterDict(item.Key, item.Value, clip);
                }
                timeline.clips.Add(clip);
            }
            else if (checktype == ScriptInterpreterType.Controller)
            {
                InterpreterController(key, value, t);
            }
            else if (checktype == ScriptInterpreterType.Action)
            {
                Controller controller = t as Controller;
                if (controller == null)
                {
                    return;
                }
                
                var detail = value as Dictionary<string, object>;
                if(detail != null)
                {
                    Action action = CreateAction(key);
                    foreach (var item in detail)
                    {
                        InterpreterDict(item.Key, item.Value, action);
                    }
                    controller.AddSubTask(action);
                }
                else
                {
                    var det = value as List<object>;
                    if (det != null)
                    {
                        foreach (var item in det)
                        {
                            InterpreterDict(key, item, t);
                        }
                    }
                }

            }
        }
        private void InterpreterController<T>(string key, object value, T t)
        {
            Controller controller;
            if (t.GetType() == typeof(TimeLineClip))
            {
                controller = (t as TimeLineClip).entry;
            }
            else
            {
                controller = t as Controller;
            }
            if (controller == null)
            {
                return;
            }
            if (key == "Sequence")
            {
                Sequence sequence = new Sequence();
                var detail = value as Dictionary<string, object>;
                foreach (var item in detail)
                {
                    InterpreterDict(item.Key, item.Value, sequence);
                }
                controller.AddSubTask(sequence);
            }
            else if (key == "Parallel")
            {
                Parallel parallel = new Parallel();
                var detail = value as Dictionary<string, object>;
                foreach (var item in detail)
                {
                    InterpreterDict(item.Key, item.Value, parallel);
                }
                controller.AddSubTask(parallel);
            }
            else if (key == "Delay")
            {
                Delay delay = new Delay();
                var detail = value as Dictionary<string, object>;
                foreach (var item in detail)
                {
                    InterpreterDict(item.Key, item.Value, delay);
                }
                controller.AddSubTask(delay);
            }
        }
        public static void InterpreterAssignment<T>(string key, object value, T t)
        {
            if (t == null)
                return;
            string str_value = value as string;
            FieldInfo fi = t.GetType().GetField(key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fi == null)
            {
                Console.WriteLine("GetField Failed, key:", key);
            }
            if (fi.FieldType.IsEnum)
            {
                fi.SetValue(t, Enum.Parse(fi.FieldType, str_value, true));
            }
            else if (fi.FieldType == typeof(string))
            {
                fi.SetValue(t, str_value);
            }
            else if (fi.FieldType == typeof(bool))
            {
                fi.SetValue(t, bool.Parse(str_value));
            }
            else if (fi.FieldType == typeof(int))
            {
                fi.SetValue(t, int.Parse(str_value));
            }
            else if (fi.FieldType == typeof(float))
            {
                fi.SetValue(t, float.Parse(str_value));
            }
        }
        private void InterpreterInherit<T>(string parent_name, T t)
        {
            if (!json_dict.ContainsKey(parent_name))
                return;
            if (!instance_dict.ContainsKey(parent_name))
            {
                Play play_ins = new Play();
                play_ins.Name = parent_name;
                var abDetail = json_dict[parent_name] as Dictionary<string, object>;
                foreach (var detail in abDetail)
                {
                    InterpreterDict(detail.Key, detail.Value, play_ins);
                }
                instance_dict.Add(parent_name, play_ins);
            }
            Inherit(instance_dict[parent_name], t);
        }
    }
}