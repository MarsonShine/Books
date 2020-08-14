namespace DynamicLoadExtension
{
    public class Extension
    {
        public bool DoWork(string numberString)
        {
            // 这段注释调了，以便可以看出函数调用的开销对比

            //Int64 number = Int64.Parse(numberString);
            //Math.Sqrt(number);
            return true;
        }
    }
}
