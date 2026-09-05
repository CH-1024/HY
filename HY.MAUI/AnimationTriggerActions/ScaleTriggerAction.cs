using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.AnimationTriggerActions
{
    public class ScaleTriggerAction : TriggerAction<VisualElement>
    {
        public double Scale { get; set; }
        public uint Length { get; set; } = 250;
        public Easing Easing { get; set; } = Easing.Linear;


        //实现方式1
        protected override async void Invoke(VisualElement sender)
        {
            await sender.ScaleToAsync(Scale, Length, Easing);
        }

        ////实现方式2
        //protected override void Invoke(VisualElement sender)
        //{
        //    sender.Animate("ScaleTriggerAction",
        //                callback: v => sender.Scale = v,  // 设置缩放比例
        //                start: sender.Scale,              // 从当前缩放比例开始
        //                end: Scale,                       // 到目标缩放比例结束
        //                length: Length,                   // 动画时长
        //                easing: Easing);                  // 缓动函数
        //}

    }
}
