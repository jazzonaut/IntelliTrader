namespace IntelliTrader.Core
{
    public static class Utils
    {
        public static decimal CalculateMargin(decimal oldValue, decimal newValue)
        {
            return (newValue - oldValue) / oldValue * 100;
        }
    }
}
