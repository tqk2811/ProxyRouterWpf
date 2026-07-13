namespace ProxyRouterWpf.Enums
{
    public enum ProxySourceGroupFilterType
    {
        Wildcard = 0,
        Equals = 1,
        StartsWith = 2,
        EndsWith = 3,
        Contains = 4,
        CidrIp = 5,
        Regex = 6,
        TotalBytes = 7,
    }
}
