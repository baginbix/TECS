using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Query;
using TECS.Result;

namespace UnitTestsECS;

public ref struct CustomQuery
{
    public ref int i;
}

public static class CustomQueryExtension
{
    public static QueryOption<CustomQuery> Get(this Query<CustomQuery> query)
    {
        var i = new CustomQuery();
        var ret = QueryOption<CustomQuery>.Some(i);
        return ret;
    }
}
