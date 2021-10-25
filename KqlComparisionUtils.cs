using System;
using System.Collections.Generic;
using System.Text;

namespace CompareTables
{
    public static class KqlComparisionUtils
    {
        public static readonly string ComparisionUtilsFunctions = 
        @"
//
//  Helper functions to calculate to handle floating point inaccuaracy when comparing to 0 or test equality
//
let is_real_zero = (x:real)
{
    let EPSILON = 1e-5;
    abs(x) < EPSILON 
};
//
let are_reals_equal = (x:real, y:real)
{
    is_real_zero(x - y)
};
//
let are_series_equal = (vec1:dynamic, vec2:dynamic)
{
    let subtract = series_subtract(vec1, vec2);
    let min = series_stats_dynamic(subtract)['min'];
    let max = series_stats_dynamic(subtract)['max'];
    is_real_zero(todouble(min)) and is_real_zero(todouble(max))
};
//
//";
    }
}
