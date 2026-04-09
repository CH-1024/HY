using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HY.ApiService.Extensions
{
    public static class StringExtension
    {

        extension (string str)
        {
            public bool IsHYid()
            {
                if (string.IsNullOrEmpty(str)) return false;

                // HYid 格式示例：16 位字母数字组合。根据实际规则调整。
                return Regex.IsMatch(str, @"^[a-zA-Z0-9]{16}$");
            }
            public bool IsPhone()
            {
                if (string.IsNullOrEmpty(str)) return false;

                // 简单的手机号/电话格式示例：允许可选的 '+'，后面 7-15 位数字。根据实际地区规则调整。
                return Regex.IsMatch(str, @"^\+?\d{7,15}$");
            }
        }

    }
}
