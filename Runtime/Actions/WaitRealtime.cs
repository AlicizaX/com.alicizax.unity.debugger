using UnityEngine;

namespace AlicizaX.Console.Actions
{
    /// <summary>
    /// 按真实时间等待指定秒数。
    /// </summary>
    public class WaitRealtime : ICommandAction
    {
        private float _startTime;
        private readonly float _duration;

        public bool IsFinished => Time.realtimeSinceStartup >= _startTime + _duration;
        public bool StartsIdle => true;

        /// <param name="seconds">等待时长，单位秒。</param>
        public WaitRealtime(float seconds)
        {
            _duration = seconds;
        }

        public void Start(ActionContext ctx)
        {
            _startTime = Time.realtimeSinceStartup;
        }

        public void Finalize(ActionContext ctx) { }

    }
}