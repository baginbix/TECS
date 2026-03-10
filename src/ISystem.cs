using TECS.Commands;

namespace TECS
{
    public interface ISystem 
    {
        void Run(ECS ecs, ref CommandBuffer cmd);
    }
}