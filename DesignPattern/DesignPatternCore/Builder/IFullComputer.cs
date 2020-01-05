namespace DesignPatternCore.Builder {
    public interface IFullComputer {
        string Mainboard { get; set; }
        string CPU { get; set; }
        string Disk { get; set; }
        string Graphics { get; set; }
        string Display { get; set; }
        bool HasOperatingSystem { get; set; }
    }
}