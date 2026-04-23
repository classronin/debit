using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace debit_wpf
{
    public static class ArithmeticHandler
    {
        public static string? Evaluate(string rawExpression)
        {
            if (string.IsNullOrWhiteSpace(rawExpression))
                return null;

            // 1. 提取纯算术表达式（智能处理 x/X 作为乘号）
            string expr = ExtractMathExpression(rawExpression);
            if (string.IsNullOrEmpty(expr))
                return null;

            // 2. 基本有效性检查
            if (!IsValidExpression(expr))
                return null;

            // 3. 去除整数前导零（避免八进制解析）
            expr = RemoveLeadingZeros(expr);

            // 4. 安全计算
            try
            {
                var result = new DataTable().Compute(expr, null);
                return FormatResult(result);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从原始字符串中提取出纯算术表达式。
        /// 过滤中文/字母，智能处理 x、X 作为乘号。
        /// </summary>
        private static string ExtractMathExpression(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";

            // 替换中文符号
            raw = raw.Replace('（', '(').Replace('）', ')');
            raw = raw.Replace('×', '*').Replace('÷', '/');

            // 第一步：只保留可能成为表达式部分的字符
            var allowed = new Regex(@"[0-9+\-*/().xX]");
            var matches = allowed.Matches(raw);
            StringBuilder sb = new StringBuilder();
            foreach (Match m in matches)
                sb.Append(m.Value);
            string filtered = sb.ToString();

            // 第二步：处理 x/X，智能识别是否为乘号
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < filtered.Length; i++)
            {
                char c = filtered[i];
                if (c == 'x' || c == 'X')
                {
                    // 判断前一个字符：数字 或 )
                    bool prevIsNumOrClose = (i > 0) &&
                        (char.IsDigit(filtered[i - 1]) || filtered[i - 1] == ')');
                    // 判断后一个字符：数字 或 ( 或 -
                    bool nextIsNumOrOpen = (i < filtered.Length - 1) &&
                        (char.IsDigit(filtered[i + 1]) || filtered[i + 1] == '(' || filtered[i + 1] == '-');

                    if (prevIsNumOrClose && nextIsNumOrOpen)
                    {
                        result.Append('*');   // 作为乘号
                    }
                    else
                    {
                        // 不是乘号，直接丢弃（不添加到结果中）
                    }
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private static bool IsValidExpression(string expr)
        {
            if (string.IsNullOrEmpty(expr))
                return false;

            // 至少包含一个运算符
            if (!Regex.IsMatch(expr, @"[\+\-\*/]"))
                return false;

            // 不能以运算符结尾
            if (Regex.IsMatch(expr, @"[\+\-\*/]$"))
                return false;

            // 括号匹配
            int count = 0;
            foreach (char c in expr)
            {
                if (c == '(') count++;
                if (c == ')') count--;
                if (count < 0) return false;
            }
            if (count != 0) return false;

            // 只允许数字、运算符、括号、小数点
            if (!Regex.IsMatch(expr, @"^[\d\+\-\*/\(\)\.]+$"))
                return false;

            // 不允许连续两个以上运算符（但负号开头的如 5*-3 是允许的，所以允许 *- 等）
            if (Regex.IsMatch(expr, @"[\+\*/]{2,}"))
                return false;
            if (Regex.IsMatch(expr, @"\-{2,}"))
                return false;

            // 开头不能是 * / 
            if (expr[0] == '*' || expr[0] == '/')
                return false;

            return true;
        }

        private static string RemoveLeadingZeros(string expr)
        {
            return Regex.Replace(expr, @"\b0+(\d)", "$1");
        }

        private static string FormatResult(object result)
        {
            if (result is int i)
                return i.ToString();
            if (result is double d)
            {
                if (Math.Abs(d - Math.Round(d)) < 1e-10)
                    return ((int)d).ToString();
                // 最多两位小数，去掉尾部零
                string s = d.ToString("0.##");
                return s;
            }
            return result.ToString() ?? "";
        }
    }
}