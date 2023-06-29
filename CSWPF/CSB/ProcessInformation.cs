namespace CSWPF.CSB;

public class ProcessInformation
{
    public uint ProcessId;
    public string BoxName;
    public string ImageName;
    public string SidString;
    public uint SessionId;

    public override string ToString()
    {
        try
        {
            return string.IsNullOrWhiteSpace(this.ImageName) ? base.ToString() : this.ImageName;
        }
        catch
        {
            return base.ToString();
        }
    }
}