using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.AnimationTriggerActions
{
    public class StartScaleTriggerAction : TriggerAction<VisualElement>
    {
        public VisualElement Target { get; set; }

        public string AnimationName { get; set; } = "ScaleAnimation";
        public double From { get; set; } = 1;
        public double To { get; set; } = 1.5;
        public uint Duration { get; set; } = 500;
        public Easing Easing { get; set; } = Easing.Linear;
        public bool Repeat { get; set; }

        protected override async void Invoke(VisualElement sender)
        {
            // 优先使用显式指定的 Target
            var targetElement = Target ?? sender;

            var animation = new Animation(v => targetElement.Scale = v, From, To, easing: Easing);

            animation.Commit(owner: targetElement, name: AnimationName, length: Duration, repeat: Repeat ? () => true : null);
        }
    }


    public class StopScaleTriggerAction : TriggerAction<VisualElement>
    {
        public VisualElement Target { get; set; }

        public string AnimationName { get; set; } = "ScaleAnimation";
        public double Value { get; set; } = 1;

        protected override void Invoke(VisualElement sender)
        {
            var targetElement = Target ?? sender;
            targetElement.AbortAnimation(AnimationName);
            targetElement.Scale = Value;
        }
    }
}
