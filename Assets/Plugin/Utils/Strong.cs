﻿using System.Reflection;
using System.Linq.Expressions;
using System;
using System.Linq;

namespace Battlehub.Utils
{
    public class Strong
    {
        public static PropertyInfo PropertyInfo<T, U>(Expression<Func<T, U>> expression)
        {
            return (PropertyInfo)MemberInfo(expression);
        }

        public static MemberInfo MemberInfo<T, U>(Expression<Func<T, U>> expression)
        {
            var member = expression.Body as MemberExpression;
            if (member != null)
                return member.Member;

            throw new ArgumentException("Expression is not a member access", "expression");
        }

        public static MethodInfo MethodInfo<T>(Expression<Func<T, Delegate>> expression)
        {
            var unaryExpression = (UnaryExpression)expression.Body;
            var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
            var methodInfoExpression = (ConstantExpression)methodCallExpression.Arguments.Last();
            var methodInfo = (MethodInfo)methodInfoExpression.Value;
            return methodInfo;
        }
    }


}
