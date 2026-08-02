using System.Data;
using Dapper;

namespace HomeDiary_api.Data;

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value  = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d  => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _           => DateOnly.FromDateTime(Convert.ToDateTime(value)),
    };
}
