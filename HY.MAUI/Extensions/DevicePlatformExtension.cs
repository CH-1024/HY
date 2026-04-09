using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Extensions
{
    public static class DevicePlatformExtension
    {
        public static int ToInt(this DevicePlatform platform) => platform switch
        {
            var p when p.Equals(DevicePlatform.Android) => 0,
            var p when p.Equals(DevicePlatform.iOS) => 1,
            var p when p.Equals(DevicePlatform.macOS) => 2,
            var p when p.Equals(DevicePlatform.MacCatalyst) => 3,
            var p when p.Equals(DevicePlatform.tvOS) => 4,
            var p when p.Equals(DevicePlatform.Tizen) => 5,
            var p when p.Equals(DevicePlatform.UWP) => 6,
            var p when p.Equals(DevicePlatform.WinUI) => 6,
            var p when p.Equals(DevicePlatform.watchOS) => 7,
            var p when p.Equals(DevicePlatform.Unknown) => -1,
            _ => -1
        };
    }
}
