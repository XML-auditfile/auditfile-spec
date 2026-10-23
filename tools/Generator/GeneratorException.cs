namespace XAFtoolkit.Auditfile.Generator;

/// <summary>An expected error: invalid input or a rule violation. Reported without a stack trace.</summary>
internal sealed class GeneratorException(string message) : Exception(message);
