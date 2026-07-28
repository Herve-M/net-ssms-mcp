using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions;
using ssmsmcp.Domain.Abstractions.Databases;
using ExecutionContext = Microsoft.SqlServer.Management.Smo.ExecutionContext;

namespace ssmsmcp.Application.Procedures;

public sealed record DescribeProcedureDto
{
    public required ObjectRefDto Object { get; init; }
    public required string Kind { get; init; }
    public required bool IsEncrypted { get; init; }
    public required bool IsSchemaBound { get; init; }
    public string? ExecuteAs { get; init; }
    public required IReadOnlyList<ParameterDto> Parameters { get; init; }
    public string? ReturnType { get; init; }
    public IReadOnlyList<FirstResultSetColumnDto>? FirstResultSetColumns { get; init; }
    public string? Body { get; init; }
    public ClrDto? Clr { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}

public sealed record FirstResultSetColumnDto
{
    public required int Ordinal { get; init; }
    public string? Name { get; init; }
    public string? DataType { get; init; }
    public bool? IsNullable { get; init; }
}

public sealed record ClrDto
{
    public required string AssemblyName { get; init; }
    public required string AssemblyClass { get; init; }
    public required string AssemblyMethod { get; init; }
    public required int AssemblyId { get; init; }
    public string? ExecutionContextPrincipal { get; init; }
}

public sealed record DescribeProcedureRequest(
    string ServerName,
    string DatabaseName,
    string Schema,
    string Name,
    bool IncludeBody,
    bool IncludeFirstResultSet)
    : IRequest<DescribeProcedureDto?>;

public sealed class DescribeProcedureHandler(
    IStoredProcedurePort storedProcedurePort,
    IUserDefinedFunctionPort userDefinedFunctionPort,
    IUserDefinedAggregatePort userDefinedAggregatePort,
    IDatabasePort databasePort)
    : IRequestHandler<DescribeProcedureRequest, DescribeProcedureDto?>
{
    private readonly IStoredProcedurePort _storedProcedurePort = storedProcedurePort;
    private readonly IUserDefinedFunctionPort _userDefinedFunctionPort = userDefinedFunctionPort;
    private readonly IUserDefinedAggregatePort _userDefinedAggregatePort = userDefinedAggregatePort;
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DescribeProcedureDto?> Handle(DescribeProcedureRequest request, CancellationToken cancellationToken)
    {
        StoredProcedure? procedure = await _storedProcedurePort.GetStoredProcedure(
            request.ServerName, request.DatabaseName, request.Schema, request.Name, cancellationToken);
        if (procedure is not null)
        {
            return await BuildFromStoredProcedure(request, procedure, cancellationToken);
        }

        UserDefinedFunction? function = await _userDefinedFunctionPort.GetUserDefinedFunction(
            request.ServerName, request.DatabaseName, request.Schema, request.Name, cancellationToken);
        if (function is not null)
        {
            return await BuildFromUserDefinedFunction(request, function, cancellationToken);
        }

        UserDefinedAggregate? aggregate = await _userDefinedAggregatePort.GetUserDefinedAggregate(
            request.ServerName, request.DatabaseName, request.Schema, request.Name, cancellationToken);
        if (aggregate is not null)
        {
            return await BuildFromUserDefinedAggregate(request, aggregate, cancellationToken);
        }

        return null;
    }

    private async ValueTask<DescribeProcedureDto> BuildFromStoredProcedure(DescribeProcedureRequest request, StoredProcedure procedure, CancellationToken cancellationToken)
    {
        bool isClr = procedure.ImplementationType == ImplementationType.SqlClr;
        string kind = isClr ? "CLR_PROCEDURE" : "PROCEDURE";

        List<string> warnings = [];
        IReadOnlyList<FirstResultSetColumnDto>? firstResultSetColumns = null;

        if (request.IncludeFirstResultSet)
        {
            if (!isClr)
            {
                IReadOnlyCollection<FirstResultSetColumnInfo> columns = await _storedProcedurePort.DescribeFirstResultSet(
                    request.ServerName, request.DatabaseName, procedure.ID, cancellationToken);

                if (columns.Any(c => c.ErrorNumber != 0))
                {
                    warnings.Add("first_result_set_columns could not be fully determined (e.g. dynamic SQL); some columns may be missing or unnamed.");
                }

                firstResultSetColumns = columns
                    .Select(c => new FirstResultSetColumnDto
                    {
                        Ordinal = c.Ordinal,
                        Name = c.Name,
                        DataType = c.SystemTypeName,
                        IsNullable = c.IsNullable,
                    })
                    .ToArray();
            }
            else
            {
                warnings.Add(FirstResultSetNotAvailableWarning(kind));
            }
        }

        string? body;
        ClrDto? clr = null;
        if (isClr)
        {
            body = null;
            warnings.Add(ClrBodyNullWarning);
            clr = await BuildClrDto(
                request,
                procedure.AssemblyName,
                procedure.ClassName,
                procedure.MethodName,
                procedure.ExecutionContext == ExecutionContext.ExecuteAsUser ? procedure.ExecutionContextPrincipal : null,
                cancellationToken);
        }
        else if (procedure.IsEncrypted)
        {
            body = null;
            warnings.Add("body is null because the procedure is encrypted (WITH ENCRYPTION).");
        }
        else if (!request.IncludeBody)
        {
            body = null;
        }
        else
        {
            body = procedure.TextBody;
        }

        return new DescribeProcedureDto
        {
            Object = BuildObjectRef(request.DatabaseName, request.Schema, procedure.Name, procedure.ID, isClr ? "CLR_STORED_PROCEDURE" : "SQL_STORED_PROCEDURE"),
            Kind = kind,
            IsEncrypted = procedure.IsEncrypted,
            IsSchemaBound = procedure.IsSchemaBound,
            ExecuteAs = MapExecuteAs(procedure.ExecutionContext, procedure.ExecutionContextPrincipal),
            Parameters = MapStoredProcedureParameters(procedure.Parameters),
            ReturnType = null,
            FirstResultSetColumns = firstResultSetColumns,
            Body = body,
            Clr = clr,
            Warnings = warnings,
        };
    }

    private async Task<DescribeProcedureDto> BuildFromUserDefinedFunction(DescribeProcedureRequest request, UserDefinedFunction function, CancellationToken cancellationToken)
    {
        bool isClr = function.ImplementationType == ImplementationType.SqlClr;
        string kind = isClr
            ? (function.FunctionType == UserDefinedFunctionType.Scalar ? "CLR_SCALAR_FUNCTION" : "CLR_TABLE_FUNCTION")
            : function.FunctionType switch
            {
                UserDefinedFunctionType.Scalar => "SCALAR_FUNCTION",
                UserDefinedFunctionType.Table when function.InlineType => "INLINE_TABLE_FUNCTION",
                UserDefinedFunctionType.Table => "TABLE_FUNCTION",
                _ => "SCALAR_FUNCTION",
            };

        List<string> warnings = [];
        if (request.IncludeFirstResultSet)
        {
            warnings.Add(FirstResultSetNotAvailableWarning(kind));
        }

        string? body;
        ClrDto? clr = null;
        if (isClr)
        {
            body = null;
            warnings.Add(ClrBodyNullWarning);
            clr = await BuildClrDto(
                request,
                function.AssemblyName,
                function.ClassName,
                function.MethodName,
                function.ExecutionContext == ExecutionContext.ExecuteAsUser ? function.ExecutionContextPrincipal : null,
                cancellationToken);
        }
        else if (function.IsEncrypted)
        {
            body = null;
            warnings.Add("body is null because the function is encrypted (WITH ENCRYPTION).");
        }
        else if (!request.IncludeBody)
        {
            body = null;
        }
        else
        {
            body = function.TextBody;
        }

        string? returnType = function.FunctionType == UserDefinedFunctionType.Scalar
            ? TableViewMappers.FormatDataType(function.DataType)
            : null;

        string typeDesc = isClr
            ? (function.FunctionType == UserDefinedFunctionType.Scalar ? "CLR_SCALAR_FUNCTION" : "CLR_TABLE_VALUED_FUNCTION")
            : kind switch
            {
                "SCALAR_FUNCTION" => "SQL_SCALAR_FUNCTION",
                "INLINE_TABLE_FUNCTION" => "SQL_INLINE_TABLE_VALUED_FUNCTION",
                _ => "SQL_TABLE_VALUED_FUNCTION",
            };

        return new DescribeProcedureDto
        {
            Object = BuildObjectRef(request.DatabaseName, request.Schema, function.Name, function.ID, typeDesc),
            Kind = kind,
            IsEncrypted = function.IsEncrypted,
            IsSchemaBound = function.IsSchemaBound,
            ExecuteAs = MapExecuteAs(function.ExecutionContext, function.ExecutionContextPrincipal),
            Parameters = MapUserDefinedFunctionParameters(function.Parameters),
            ReturnType = returnType,
            FirstResultSetColumns = null,
            Body = body,
            Clr = clr,
            Warnings = warnings,
        };
    }

    private async Task<DescribeProcedureDto> BuildFromUserDefinedAggregate(DescribeProcedureRequest request, UserDefinedAggregate aggregate, CancellationToken cancellationToken)
    {
        List<string> warnings = [];
        warnings.Add(ClrBodyNullWarning);
        if (request.IncludeFirstResultSet)
        {
            warnings.Add(FirstResultSetNotAvailableWarning("AGGREGATE_FUNCTION"));
        }

        ClrDto clr = await BuildClrDto(
            request,
            aggregate.AssemblyName,
            aggregate.ClassName,
            "(aggregate: Init/Accumulate/Merge/Terminate)",
            null,
            cancellationToken);

        return new DescribeProcedureDto
        {
            Object = BuildObjectRef(request.DatabaseName, request.Schema, aggregate.Name, aggregate.ID, "AGGREGATE_FUNCTION"),
            Kind = "AGGREGATE_FUNCTION",
            IsEncrypted = false,
            IsSchemaBound = false,
            ExecuteAs = null,
            Parameters = MapUserDefinedAggregateParameters(aggregate.Parameters),
            ReturnType = TableViewMappers.FormatDataType(aggregate.DataType),
            FirstResultSetColumns = null,
            Body = null,
            Clr = clr,
            Warnings = warnings,
        };
    }

    private async Task<ClrDto> BuildClrDto(
        DescribeProcedureRequest request,
        string assemblyName,
        string assemblyClass,
        string assemblyMethod,
        string? executionContextPrincipal,
        CancellationToken cancellationToken)
    {
        Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);
        SqlAssembly? assembly = database.Assemblies[assemblyName];

        if (assembly is null)
        {
            throw new InvalidOperationException($"Assembly '{assemblyName}' not found in database '{request.DatabaseName}'.");
        }

        return new ClrDto
        {
            AssemblyName = assemblyName,
            AssemblyClass = assemblyClass,
            AssemblyMethod = assemblyMethod,
            AssemblyId = assembly.ID,
            ExecutionContextPrincipal = executionContextPrincipal,
        };
    }

    private const string ClrBodyNullWarning =
        "Object is CLR; body is null by design. See 'clr' field for the assembly reference.";

    private static string FirstResultSetNotAvailableWarning(string kind) =>
        $"first_result_set_columns is not available for {kind} (sys.dm_exec_describe_first_result_set_for_object only supports T-SQL procedures).";

    private static ObjectRefDto BuildObjectRef(string database, string schema, string name, int objectId, string typeDesc) => new()
    {
        Database = database,
        Schema = schema,
        Name = name,
        ObjectId = objectId,
        TypeDesc = typeDesc,
        Fqn = Identifiers.BuildFqn(database, schema, name),
    };

    private static string? MapExecuteAs(ExecutionContext executionContext, string executionContextPrincipal) => executionContext switch
    {
        ExecutionContext.Owner => "OWNER",
        ExecutionContext.Self => "SELF",
        ExecutionContext.ExecuteAsUser => executionContextPrincipal,
        _ => null,
    };

    private static List<ParameterDto> MapStoredProcedureParameters(StoredProcedureParameterCollection parameters) =>
        parameters.Cast<StoredProcedureParameter>()
            .Select(p => new ParameterDto
            {
                Name = p.Name,
                Ordinal = p.ID,
                DataType = TableViewMappers.FormatDataType(p.DataType),
                IsOutput = p.IsOutputParameter,
                IsReadonly = p.IsReadOnly,
                HasDefaultValue = !string.IsNullOrEmpty(p.DefaultValue),
                DefaultValue = string.IsNullOrEmpty(p.DefaultValue) ? null : p.DefaultValue,
            })
            .ToList();

    private static List<ParameterDto> MapUserDefinedFunctionParameters(UserDefinedFunctionParameterCollection parameters) =>
        parameters.Cast<UserDefinedFunctionParameter>()
            .Select(p => new ParameterDto
            {
                Name = p.Name,
                Ordinal = p.ID,
                DataType = TableViewMappers.FormatDataType(p.DataType),
                IsOutput = false,
                IsReadonly = p.IsReadOnly,
                HasDefaultValue = !string.IsNullOrEmpty(p.DefaultValue),
                DefaultValue = string.IsNullOrEmpty(p.DefaultValue) ? null : p.DefaultValue,
            })
            .ToList();

    private static List<ParameterDto> MapUserDefinedAggregateParameters(UserDefinedAggregateParameterCollection parameters) =>
        parameters.Cast<UserDefinedAggregateParameter>()
            .Select(p => new ParameterDto
            {
                Name = p.Name,
                Ordinal = p.ID,
                DataType = TableViewMappers.FormatDataType(p.DataType),
                IsOutput = false,
                IsReadonly = false,
                HasDefaultValue = false,
                DefaultValue = null,
            })
            .ToList();
}
