using System.Threading.Tasks;

namespace MQRpc.Core.Interfaces
{
    public readonly struct Unit
    {
        public static Task<Unit> TaskUnit => Task.FromResult(new Unit());
    }
}