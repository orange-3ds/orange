namespace OrangeLib
{
    static public class Info
    {
        static readonly string _version = "0.0.0 Beta";
        static public void Help()
        {
            System.Console.WriteLine("Orange");
        }
        static public void Version()
        {
            System.Console.WriteLine($"Orange Version {_version}");
        }
    }
}