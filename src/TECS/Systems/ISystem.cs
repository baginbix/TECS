using System.Dynamic;
using TECS.Commands;

namespace TECS;
[AttributeUsage(AttributeTargets.Method)]
public class SystemAttribute : Attribute;
    
    // Optional: For grouping systems together if you want to add a whole class at once
[AttributeUsage(AttributeTargets.Class)]
public class SystemGroupAttribute : Attribute;
public interface ISystem 
{
    Type[] Reads {get;}
    Type[] Writes {get;}
    void Run(IEngine engine, CommandBuffer cmd);
}
