using System.Runtime.CompilerServices;

// 允许单元测试程序集访问本程序集的 internal 类型（如 HTTPDNSFactory / CyHTTPDNS），
// 以便对内部工厂与协议实现进行白盒测试。不改变对外公开 API。
[assembly: InternalsVisibleTo("Tests")]
