using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TimeLineScript
{
    /// <summary>
    /// 演员
    /// </summary>
    public class Actor
    {
        public Vector3 Dir
        {
            get
            {
                return new Vector3(0, 1, 0);
            }
        }
        //测试接口
        public void PlayAction(string action, float rate)
        {
            Console.WriteLine("Actor PlayAction:" + action);
        }
        public void Move(Vector3 pos)
        {
            Console.WriteLine("Actor Move:"+ pos);
        }
        public void SetVisible(bool visible)
        {
            Console.WriteLine("Actor SetVisible:"+ visible);
        }
    }
}
