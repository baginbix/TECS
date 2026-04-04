using TECS.Commands;

namespace TECS
{
    public interface ISystem 
    {
        void Run(IEngine engine, CommandBuffer cmd);
    }
}