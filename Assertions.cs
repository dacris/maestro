namespace Dacris.Maestro
{
    public static class Assertions
    {
        public static void ShouldBe(this object? a, object? b)
        {
            const string msg = "Error in self-checking operation. Expected two objects to match.";
            if (a == b) return;
            if (a == null && b != null) throw new Exception(msg);
            if (a != null && b == null) throw new Exception(msg);
            if (!a!.Equals(b)) throw new Exception(msg);
        }

        public static void ShouldBeBetween(this ValueType value, string start, string end)
        {
            if (value.GetType().Name.Equals("DateTime"))
            {
                if ((DateTime)value > DateTime.Parse(end)
                    || (DateTime)value < DateTime.Parse(start))
                {
                    throw new Exception("DateTime out of range.");
                }
            }
            else if (value.GetType().Name.Equals("Int64") || value.GetType().Name.Equals("Int32"))
            {
                if ((long)value > long.Parse(end)
                    || (long)value < long.Parse(start))
                {
                    throw new Exception("Integer out of range.");
                }
            }
            else if (value.GetType().Name.Equals("Double") || value.GetType().Name.Equals("Single"))
            {
                if ((double)value > double.Parse(end)
                    || (double)value < double.Parse(start))
                {
                    throw new Exception("Number out of range.");
                }
            }
            else if (value.GetType().Name.Equals("Decimal"))
            {
                if ((decimal)value > decimal.Parse(end)
                    || (decimal)value < decimal.Parse(start))
                {
                    throw new Exception("Number out of range.");
                }
            }
        }
    }
}
