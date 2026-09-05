using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.AnimationTriggerActions
{
    public class FadeTriggerAction : TriggerAction<VisualElement>
    {
        public double Opacity { get; set; }
        public uint Length { get; set; } = 250;
        public Easing Easing { get; set; } = Easing.Linear;


        //实现方式1
        protected override async void Invoke(VisualElement sender)
        {
            await sender.FadeToAsync(Opacity, Length, Easing);
        }

        ////实现方式2
        //protected override void Invoke(VisualElement sender)
        //{
        //    sender.Animate("FadeTriggerAction",
        //                callback: v => sender.Opacity = v,  // 设置透明度
        //                start: sender.Opacity,              // 从当前透明度开始
        //                end: Opacity,                       // 到目标透明度结束
        //                length: Length,                     // 动画时长
        //                easing: Easing);                    // 缓动函数
        //}

    }
}
