using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Generator;

public abstract record SystemParam(string ParameterName, int Index);

public record QueryParam(string ParemeterName, int Index, string StructType) 
: SystemParam(ParemeterName, Index);

public record CommandBufferParam(string ParameterName, int Index)
: SystemParam(ParameterName, Index);

public record EventReaderParam(string ParameterName, int Index, string EventType)
: SystemParam(ParameterName, Index);

public record EventWriterParam(string ParameterName, int Index, string EventType)
: SystemParam(ParameterName, Index);

public record ResParam(string ParameterName, int Index, string ResourceType)
: SystemParam(ParameterName, Index);

public record ResMutParam(string ParameterName, int Index, string ResourceType)
: SystemParam(ParameterName, Index);