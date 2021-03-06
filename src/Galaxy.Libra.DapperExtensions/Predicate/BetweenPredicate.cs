﻿using Galaxy.Libra.DapperExtensions.Sql;
using System.Collections.Generic;

namespace Galaxy.Libra.DapperExtensions.Predicate
{
    public struct BetweenValues
    {
        public object Value1 { get; set; }
        public object Value2 { get; set; }
    }

    public interface IBetweenPredicate : IPredicate
    {
        string PropertyName { get; set; }
        BetweenValues Value { get; set; }
        bool Not { get; set; }
    }

    public class BetweenPredicate<T> : BasePredicate, IBetweenPredicate
        where T : class
    {
        public override string GetSql(ISqlGenerator sqlGenerator, IDictionary<string, object> parameters)
        {
            string columnName = GetColumnName(typeof(T), sqlGenerator, PropertyName);
            string notStr = Not ? "NOT " : string.Empty;
            string propertyName1 = parameters.SetParameterName(this.PropertyName, this.Value.Value1, sqlGenerator.Configuration.Dialect.ParameterPrefix);
            string propertyName2 = parameters.SetParameterName(this.PropertyName, this.Value.Value2, sqlGenerator.Configuration.Dialect.ParameterPrefix);
            return $"({columnName} {notStr}BETWEEN {propertyName1} AND {propertyName2})";
        }

        public BetweenValues Value { get; set; }

        public bool Not { get; set; }
    }
}
