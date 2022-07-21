namespace MapyCZforTS_CS
{
    public class Mapset
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public byte MaxZoom { get; set; }

        public Mapset(string name, string value, byte maxZoom)
        {
            Name = name;
            Value = value;
            MaxZoom = maxZoom;
        }

        //public Mapset(string name, string value) : this(name, value, byte.MaxValue) { }

        public override string ToString()
        {
            return Name;
        }
    }
}
