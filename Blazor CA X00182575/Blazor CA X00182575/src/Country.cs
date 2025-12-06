namespace CA3_X00182575.Data
{
    public class Country
    {
        public Name name { get; set; }
        public List<string> capital { get; set; }
        public string region { get; set; }
        public long population { get; set; }
        public double area { get; set; }
        public Flags flags { get; set; }
        public List<double> latlng { get; set; }
    }

    public class Name
    {
        public string common { get; set; }
    }

    public class Flags
    {
        public string png { get; set; }
    }
}
