﻿using Galaxy.Libra.DapperExtensions.Mapper;
using Galaxy.Libra.DapperExtensions.Predicate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Galaxy.Libra.DapperExtensions.PredicateConver
{
    public class ExpressionPredicateConver
    {
        /// <summary>
        /// 将表达式转换为Predicate
        /// </summary>
        public static IPredicate GetExpressionPredicate<T>(IClassMapper classMap, Expression<Func<T, bool>> expression) where T : class
        {
            Expression expr = expression.Body;
            return ConvertToPredicate<T>(expr);
        }

        public static IPredicate ConvertToPredicate<T>(Expression expr) where T : class
        {
            if (expr.NodeType == ExpressionType.OrElse || expr.NodeType == ExpressionType.AndAlso)
            {
                BinaryExpression binExpr = expr as BinaryExpression;
                IList<IPredicate> predList = new List<IPredicate> { ConvertToPredicate<T>(binExpr.Left), ConvertToPredicate<T>(binExpr.Right) };
                GroupOperator op = expr.NodeType == ExpressionType.OrElse ? GroupOperator.Or : GroupOperator.And;
                return Predicates.Group(op, predList.ToArray());
            }
            else if (expr.NodeType == ExpressionType.Equal || expr.NodeType == ExpressionType.NotEqual
                || expr.NodeType == ExpressionType.GreaterThan || expr.NodeType == ExpressionType.GreaterThanOrEqual
                || expr.NodeType == ExpressionType.LessThan || expr.NodeType == ExpressionType.LessThanOrEqual)
            {
                return ConvertBaseExpression<T>(expr as BinaryExpression);
            }
            else if (expr.NodeType == ExpressionType.Call)
            {
                return ConverCallExpression<T>(expr);
            }

            return null;
        }

        private static IPredicate ConvertBaseExpression<T>(Expression expr) where T : class
        {
            BinaryExpression binExpr = expr as BinaryExpression;
            MemberExpression menLeftExpr = binExpr.Left as MemberExpression;
            ConstantExpression conRightExpr = binExpr.Right as ConstantExpression;

            if (menLeftExpr != null && conRightExpr != null)
            {
                Operator op = Operator.Eq;
                bool not = false;

                switch (binExpr.NodeType)
                {
                    case ExpressionType.Equal:
                        op = Operator.Eq;
                        break;
                    case ExpressionType.NotEqual:
                        op = Operator.Eq;
                        not = true;
                        break;
                    case ExpressionType.LessThan:
                        op = Operator.Lt;
                        break;
                    case ExpressionType.GreaterThan:
                        op = Operator.Gt;
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        op = Operator.Ge;
                        break;
                    case ExpressionType.LessThanOrEqual:
                        op = Operator.Le;
                        break;
                    default:
                        throw new Exception($"未实现该操作符逻辑{binExpr.NodeType}");
                }

                return new FieldPredicate<T>
                {
                    PropertyName = menLeftExpr.Member.Name,
                    Operator = op,
                    Not = not,
                    Value = conRightExpr.Value
                };
            }

            return null;
        }

        private static IPredicate ConverCallExpression<T>(Expression expr) where T : class
        {
            MethodCallExpression methExpr = expr as MethodCallExpression;
            MemberExpression menExpr = methExpr.Object as MemberExpression;

            //判断是否是列表，如果是列表则进行in条件构建
            if (menExpr.Type.IsGenericType == true || menExpr.Type.IsArray == true)
            {
                string propertyName = (methExpr.Arguments[0] as MemberExpression).Member.Name;
                object value = Expression.Lambda<Func<object>>(menExpr).Compile()();

                return new FieldPredicate<T>
                {
                    PropertyName = propertyName,
                    Operator = Operator.Eq,
                    Value = value
                };
            }
            else
            {
                string propertyName = menExpr.Member.Name;
                string methName = methExpr.Method.Name;
                string paramStr = (methExpr.Arguments[0] as ConstantExpression).Value.ToString();

                switch (methName)
                {
                    case "Contains":
                        paramStr = $"%{paramStr}%";
                        break;
                    case "StartsWith":
                        paramStr = $"{paramStr}%";
                        break;
                    case "EndsWith":
                        paramStr = $"%{paramStr}";
                        break;
                    default:
                        throw new Exception($"未能解析该方法{methName}");
                }

                return new FieldPredicate<T>
                {
                    PropertyName = propertyName,
                    Operator = Operator.Like,
                    Value = paramStr
                };
            }

            return null;
        }
    }
}
