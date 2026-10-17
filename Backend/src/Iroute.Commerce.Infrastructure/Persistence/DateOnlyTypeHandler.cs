using System.Data;
using Dapper;

namespace Iroute.Commerce.Infrastructure.Persistence;

/// <summary>
/// Mapea columnas SQL DATE a DateOnly (Dapper no lo hace de forma nativa).
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => DateOnly.Parse(value.ToString()!)
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
