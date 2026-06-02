using UnityEngine;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 按缩放后的游戏时间等待指定秒数。
    /// </summary>
    public class Wait : ICommandAction
    {
        private float _startTime;
        private readonly float _duration;

        public bool IsFinished => Time.time >= _startTime + _duration;
        public bool StartsIdle => true;

        /// <param name="seconds">等待时长，单位秒。</param>
        public Wait(float seconds)
        {
            _duration = seconds;
        }

        public void Start(ActionContext ctx)
        {
            _startTime = Time.time;
        }

        public void Finalize(ActionContext ctx) { }

    }
}