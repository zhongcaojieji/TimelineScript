using System;
using System.Collections.Generic;
using System.Text;

namespace TimeLineScript
{
    class PlayScheduler
    {
        Interpreter interpreter;
        List<Play> activePlays = new List<Play>();
        public PlayScheduler(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }
        public void ActivePlay(string playname)
        {
            var play = this.interpreter.GetPlay(playname);
            play.OnAwake(new Actor());
            activePlays.Add(play);
            play.OnStart(TimeLineFilter.Standard);
        }
        public void Update(float deltatime)
        {
            for(int i = 0; i<activePlays.Count; i++)
            {
                activePlays[i].OnUpdate(deltatime);
            }
            for (int i = activePlays.Count - 1; i >= 0; i--)
            {
                if(!activePlays[i].IsPlay())
                {
                    activePlays.RemoveAt(i);
                }
            }
        }
    }
}
