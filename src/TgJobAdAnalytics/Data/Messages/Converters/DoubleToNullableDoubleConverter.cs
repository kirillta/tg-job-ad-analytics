using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TgJobAdAnalytics.Data.Messages.Converters;

public class DoubleToNullableDoubleConverter : ValueConverter<double?, double?>
{
    public DoubleToNullableDoubleConverter() 
        : base(
            v => ConvertToProvider(v),
            v => ConvertFromProvider(v))
    {
    }


    private static double? ConvertToProvider(double? value)
    {
        if (value is null)
            return null;
        
        if (double.IsNaN(value.Value))
            return NanSentinelValue;
        
        return value;
    }


    private static double? ConvertFromProvider(double? value)
    {
        if (value == NanSentinelValue)
            return double.NaN;
            
        return value;
    }


    private const double NanSentinelValue = -1;
}
